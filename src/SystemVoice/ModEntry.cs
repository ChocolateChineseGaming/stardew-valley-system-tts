using System.Text.Json;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;

namespace MandarinVoice;

public sealed class ModEntry : Mod
{
    private const string ModVersion = "0.1.0";
    private ModConfig config = new();
    private readonly MacAudioPlayer audio = new();
    private readonly HashSet<string> missing = new();
    private readonly NarrationSession session = new();
    private QuestTextReader quests = null!;
    private AdditionalTextReader additional = null!;
    private bool questReaderFailed;
    private bool letterReaderFailed;
    private bool failed;

    public override void Entry(IModHelper helper)
    {
        if (!OperatingSystem.IsMacOS())
        {
            Monitor.Log("此 Mod 仅支持 macOS 系统 TTS。", LogLevel.Error);
            return;
        }

        quests = new QuestTextReader(helper);
        additional = new AdditionalTextReader(helper);
        config = helper.ReadConfig<ModConfig>();
        RepairConfig();

        helper.Events.GameLoop.UpdateTicked += OnUpdate;
        helper.Events.GameLoop.SaveLoaded += (_, _) =>
            Notify($"系统普通话配音 Mod {ModVersion} 已加载；F8 朗读，F10 设置。", LogLevel.Info);
        helper.Events.Display.MenuChanged += (_, _) => Reset();
        helper.Events.GameLoop.ReturnedToTitle += (_, _) => Reset();
        helper.Events.Input.ButtonPressed += OnButton;

        helper.ConsoleCommands.Add("system_voice_reload", "重新读取系统配音配置。", (_, _) =>
        {
            config = helper.ReadConfig<ModConfig>();
            RepairConfig();
            failed = false;
            questReaderFailed = false;
            letterReaderFailed = false;
            Reset();
        });
        helper.ConsoleCommands.Add("system_voice_status", "显示系统配音状态。", (_, _) =>
            Notify($"系统 TTS 已{(config.Enabled ? "启用" : "停用")}；声音：{config.FallbackVoice}。",
                LogLevel.Info));
        helper.ConsoleCommands.Add("system_voice_config", "打开系统配音设置。", (_, _) =>
            OpenSettings());

        AppDomain.CurrentDomain.ProcessExit += (_, _) => audio.Dispose();
    }

    private void RepairConfig()
    {
        bool changed = false;
        if (string.IsNullOrWhiteSpace(config.FallbackVoice))
        {
            config.FallbackVoice = "Tingting (中文（中国大陆）)";
            changed = true;
        }
        int speechRate = Math.Clamp(config.SpeechRate, 80, 350);
        float volume = Math.Clamp(float.IsFinite(config.Volume) ? config.Volume : 1f, 0f, 1f);
        float playbackRate = Math.Clamp(float.IsFinite(config.PlaybackRate)
            ? config.PlaybackRate : 0.85f, 0.7f, 1.15f);
        if (speechRate != config.SpeechRate || volume != config.Volume
            || playbackRate != config.PlaybackRate)
        {
            config.SpeechRate = speechRate;
            config.Volume = volume;
            config.PlaybackRate = playbackRate;
            changed = true;
        }
        if (config.NpcVoices is null)
        {
            config.NpcVoices = new();
            changed = true;
        }
        if (config.NpcVolume is null)
        {
            config.NpcVolume = new();
            changed = true;
        }
        if (changed) Helper.WriteConfig(config);
    }

    private void SaveSettings()
    {
        RepairConfig();
        Helper.WriteConfig(config);
    }

    private void Reset()
    {
        audio.Stop();
        session.Clear();
    }

    private void OpenSettings()
    {
        if (!Context.IsWorldReady) return;
        if (Game1.activeClickableMenu is VoiceSettingsMenu)
        {
            Game1.exitActiveMenu();
            return;
        }
        Reset();
        Game1.activeClickableMenu = new VoiceSettingsMenu(config, SaveSettings);
    }

    private void Notify(string message, LogLevel level = LogLevel.Info)
    {
        Monitor.Log(message, level);
        if (Context.IsWorldReady) Game1.addHUDMessage(new HUDMessage(message));
    }

    private void OnButton(object? sender, ButtonPressedEventArgs e)
    {
        if (e.Button == config.ConfigKey)
        {
            Helper.Input.Suppress(e.Button);
            OpenSettings();
            return;
        }
        if (e.Button == config.ReplayKey)
        {
            config = Helper.ReadConfig<ModConfig>();
            RepairConfig();
        }
        if (e.Button != config.ReplayKey || !config.Enabled || failed
            || LocalizedContentManager.CurrentLanguageCode != LocalizedContentManager.LanguageCode.zh
            || Game1.activeClickableMenu is not { } menu)
            return;

        try
        {
            List<Narration> lines = ReadMenu(menu);
            if (lines.Count == 0)
            {
                Notify("当前界面没有可朗读的对白、任务、电视节目或信件。", LogLevel.Info);
                return;
            }
            Helper.Input.Suppress(e.Button);
            audio.Stop();
            session.Request(menu, lines);
            PlayNext();
        }
        catch (Exception ex) { PauseNarration(ex); }
    }

    private void OnUpdate(object? sender, UpdateTickedEventArgs e)
    {
        if (failed) return;
        try
        {
            if (!config.Enabled
                || LocalizedContentManager.CurrentLanguageCode != LocalizedContentManager.LanguageCode.zh
                || Game1.activeClickableMenu is not { } menu)
            {
                Reset();
                return;
            }

            List<Narration> lines = ReadMenu(menu);
            if (lines.Count == 0)
            {
                Reset();
                return;
            }
            if (session.Update(menu, lines))
            {
                audio.Stop();
                foreach (Narration line in lines)
                {
                    string key = VoiceKey.For(line.Speaker, line.Text);
                    if (!File.Exists(Path.Combine(Helper.DirectoryPath, "audio", key + ".wav")))
                        CaptureMissing(line, key);
                }
                return;
            }

            audio.ConsumePlaybackStarted();
            int? exitCode = audio.Reap();
            if (exitCode is null) return;
            if (exitCode != 0)
            {
                Monitor.Log($"系统语音生成或播放失败（退出码 {exitCode}）。", LogLevel.Warn);
                Notify("系统语音生成或播放失败；请检查 macOS 声音设置。", LogLevel.Warn);
            }
            PlayNext();
        }
        catch (Exception ex) { PauseNarration(ex); }
    }

    private void PauseNarration(Exception ex)
    {
        failed = true;
        Reset();
        Monitor.Log($"配音暂停，游戏可继续。使用 system_voice_reload 重试。\n{ex}", LogLevel.Error);
    }

    private List<Narration> ReadMenu(IClickableMenu menu)
    {
        if (menu is DialogueBox box)
        {
            string text = VoiceKey.Normalize(box.getCurrentString());
            if (text.Length == 0) return new();
            if (box.characterDialogue?.speaker is NPC npc)
                return new() { new(npc.Name, text) };
            return config.ReadNonNpcDialogue
                ? new() { new(TvShow.SpeakerFor(CurrentTvChannel(), text), text) }
                : new();
        }
        if (config.ReadLetters && !letterReaderFailed && menu is LetterViewerMenu letter)
        {
            try { return additional.ReadLetter(letter); }
            catch (Exception ex)
            {
                letterReaderFailed = true;
                Monitor.Log($"信件文本读取暂停，其他配音不受影响。\n{ex}", LogLevel.Warn);
                return new();
            }
        }
        if (!config.ReadQuestText || questReaderFailed) return new();
        try { return quests.Read(menu); }
        catch (Exception ex)
        {
            questReaderFailed = true;
            Monitor.Log($"任务文本读取暂停，NPC 配音不受影响。\n{ex}", LogLevel.Warn);
            return new();
        }
    }

    private string? CurrentTvChannel()
    {
        if (Game1.currentLocation?.furniture is null) return null;
        foreach (var furniture in Game1.currentLocation.furniture)
        {
            if (furniture is not TV) continue;
            try
            {
                object? value = Helper.Reflection.GetField<object>(furniture, "currentChannel",
                    required: false)?.GetValue();
                string? channel = Convert.ToString(value,
                    System.Globalization.CultureInfo.InvariantCulture);
                if (!string.IsNullOrWhiteSpace(channel) && channel != "0") return channel;
            }
            catch (Exception) { }
        }
        return null;
    }

    private void PlayNext()
    {
        while (session.Next() is { } line)
        {
            string spokenText = VoiceKey.ForSpeech(line.Text);
            if (spokenText.Length == 0) continue;
            string key = VoiceKey.For(line.Speaker, line.Text);
            string libraryPath = Path.Combine(Helper.DirectoryPath, "audio", key + ".wav");
            float gain = config.NpcVolume.GetValueOrDefault(line.Speaker, 1f);
            float volume = Math.Clamp(config.Volume * gain, 0f, 1f);
            float playbackRate = Math.Clamp(config.PlaybackRate, 0.7f, 1.15f);
            if (File.Exists(libraryPath) && SpeechCache.HasAudio(libraryPath))
            {
                audio.Play(libraryPath, volume, playbackRate);
                return;
            }

            CaptureMissing(line, key);
            string voice = config.NpcVoices.GetValueOrDefault(line.Speaker)
                ?? config.FallbackVoice;
            int rate = Math.Clamp(config.SpeechRate, 80, 350);
            string cached = Path.Combine(Helper.DirectoryPath, "cache",
                SpeechCache.FileName(line.Speaker, line.Text, voice, rate));
            if (File.Exists(cached) && SpeechCache.HasAudio(cached))
                audio.Play(cached, volume, playbackRate);
            else
            {
                Notify("系统语音准备中…", LogLevel.Trace);
                audio.GenerateAndPlay(spokenText, voice, rate, cached, volume, playbackRate);
            }
            return;
        }
    }

    private void CaptureMissing(Narration line, string key)
    {
        if (!config.CaptureMissingDialogue || !missing.Add(key)) return;
        try
        {
            File.AppendAllText(Path.Combine(Helper.DirectoryPath, "missing-dialogue.jsonl"),
                JsonSerializer.Serialize(new { npc = line.Speaker, text = line.Text, key }) + "\n");
        }
        catch (IOException ex) { Monitor.Log($"无法记录缺失文本：{ex.Message}", LogLevel.Warn); }
        catch (UnauthorizedAccessException ex) { Monitor.Log($"无法记录缺失文本：{ex.Message}", LogLevel.Warn); }
    }
}
