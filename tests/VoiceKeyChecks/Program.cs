using MandarinVoice;
if (VoiceKey.For("Penny", "今天天气真不错。") != "bd00aabe9b556f1d2c384aad9a88c2b76a1e9c181982e1b6af3fe050f1a97a4f") throw new Exception("Python/C# key mismatch");
if (VoiceKey.For("Penny", "  你好\n 世界。 ") != "fe10a6e624e7923da913bb27c3100f8fe9df89e0334f0a283abf430263b7b6bf") throw new Exception("Python/C# key mismatch");
if (VoiceKey.For("Abigail", "é") != "a2e72e06f93e8987c664b2117b0710641b03b8d8740b6eeb91c3d45cc10cdada") throw new Exception("Python/C# key mismatch");
if (VoiceKey.For("Lewis", "你　好") != "908c95bef35463a50418a8820f5bc3cf936d16fde63c89cc1b88514f2b35df45") throw new Exception("Python/C# key mismatch");
Console.WriteLine("4 Python/C# hash compatibility checks passed.");
if (VoiceKey.ForSpeech("- 收集 10 个木材 —") != "收集 10 个木材"
    || VoiceKey.ForSpeech("• 第一项 – 第二项") != "第一项 第二项"
    || VoiceKey.ForSpeech("---").Length != 0)
    throw new Exception("Speech punctuation cleanup failed");

var lines = QuestNarration.Create("  入门  ", "种植防风草。", new[] { "收获防风草。", "收获防风草。", "  " });
if (lines.Count != 3 || lines[0].Text != "入门" || lines[2].Text != "收获防风草。"
    || lines.Any(l => l.Speaker != "QuestNarrator")) throw new Exception("Quest content/order/deduplication failed");
if (QuestNarration.Create(null, " ", null).Count != 0) throw new Exception("Empty board must remain silent");
var letter = NarrationText.Create("LetterNarrator", null, "第一行^第二行", null);
if (letter.Count != 1 || letter[0].Text != "第一行 第二行"
    || letter.Any(l => l.Speaker != "LetterNarrator"))
    throw new Exception("Letter body composition failed");
var pagedLetter = NarrationText.Create("LetterNarrator", null, null,
    new[] { "第一页", "第二页", "第一页" });
if (pagedLetter.Count != 2 || pagedLetter[0].Text != "第一页" || pagedLetter[1].Text != "第二页")
    throw new Exception("Paged letter composition/order/deduplication failed");
if (LetterSender.Resolve("EmilyCooking", -1, false, null) != "Emily")
    throw new Exception("Letter sender from mail id failed");
if (LetterSender.Resolve("MarlonItem", -1, false, null) != "Marlon")
    throw new Exception("Letter sender must prefer Marlon over Maru");
if (LetterSender.Resolve("MaruPlanet", -1, false, null) != "Maru")
    throw new Exception("Letter sender from Maru mail id failed");
if (LetterSender.Resolve("QiChallengeComplete", -1, false, null) != "MrQi")
    throw new Exception("Letter sender Qi alias failed");
if (LetterSender.Resolve("unknownMail", -1, false, new[] { "谢谢你。\n   - 潘妮" }) != "Penny")
    throw new Exception("Letter sender from Chinese signature failed");
if (LetterSender.Resolve("unknownMail", -1, false, new[] { "Thanks.\n- Penny" }) != "Penny")
    throw new Exception("Letter sender from English signature failed");
if (LetterSender.Resolve("unknownMail", -1, false, new[] { "谢谢你。   - 潘妮" }) != "Penny")
    throw new Exception("Letter sender from same-line signature failed");
if (LetterSender.Resolve("EmilyCooking", 3, false, null) != "LetterNarrator")
    throw new Exception("Secret notes must keep letter narrator");
if (LetterSender.Resolve("EmilyCooking", -1, true, null) != "LetterNarrator")
    throw new Exception("Player-authored letters must keep letter narrator");
if (LetterSender.Resolve("passedOut1", -1, false, new[] { "你昏倒了。" }) != "LetterNarrator")
    throw new Exception("Unknown mail must keep letter narrator");
if (TvShow.SpeakerFor("2", "天气预报") != "FortuneTeller"
    || TvShow.SpeakerFor("fortune", "") != "FortuneTeller"
    || TvShow.SpeakerFor(null, "欢迎收看今日占卜。") != "FortuneTeller")
    throw new Exception("Fortune teller must use adult female speaker");
if (TvShow.SpeakerFor("4", "早上好") != "QueenOfSauce"
    || TvShow.SpeakerFor("The Queen of Sauce", "") != "QueenOfSauce"
    || TvShow.SpeakerFor(null, "欢迎收看酱汁女皇。") != "QueenOfSauce")
    throw new Exception("Recipe show must use adult female speaker");
if (TvShow.SpeakerFor("1", "占卜") != "GameNarrator"
    || TvShow.SpeakerFor("weather", "食谱") != "GameNarrator")
    throw new Exception("Weather channel must keep game narrator");
if (VoiceKey.For("QuestNarrator", "你好。") == VoiceKey.For("Penny", "你好。"))
    throw new Exception("Quest and NPC audio must remain separate");
var menu = new object();
var session = new NarrationSession();
if (!session.Update(menu, lines) || session.Next() is not null) throw new Exception("Opening content must remain silent");
session.Request(menu, lines);
if (session.Next() != lines[0]) throw new Exception("Manual request must start with title");
if (session.Update(menu, lines) || session.Next() != lines[1]) throw new Exception("Same page must preserve requested queue");
if (session.Next() != lines[2] || session.Next() is not null) throw new Exception("Queue must end after objectives");
if (session.Update(menu, lines) || session.Next() is not null) throw new Exception("Finished page must stay silent");
session.Request(menu, lines);
session.Next();
session.Request(menu, lines);
if (session.Next() != lines[0]) throw new Exception("Pressing again must restart at title");
var changed = QuestNarration.Create("新任务", "新说明", null);
if (!session.Update(menu, changed) || session.Next() is not null) throw new Exception("Switching tasks must clear old queue without starting new audio");
session.Request(menu, changed);
if (session.Next() != changed[0]) throw new Exception("New task must play only after request");
session.Clear();
if (session.Next() is not null || !session.Update(menu, lines) || session.Next() is not null)
    throw new Exception("Close and reopen must remain silent");
session.Request(menu, lines);
if (!session.Update(new object(), lines) || session.Next() is not null) throw new Exception("New menu must cancel requested narration");
var dialogue = new[] { new Narration("Penny", "今天天气真不错。") };
if (!session.Update(menu, dialogue) || session.Next() is not null) throw new Exception("NPC dialogue must not autoplay");
session.Request(menu, dialogue);
if (session.Next() != dialogue[0] || session.Next() is not null) throw new Exception("Manual NPC request must play once");
session.Request(menu, dialogue);
if (!session.Update(menu, Array.Empty<Narration>()) || session.Next() is not null) throw new Exception("Empty content must cancel playback queue");
Console.WriteLine("Quest composition and manual narration lifecycle checks passed.");
if (SpeechCache.FileName("Penny", "你好。", "voice A", 175) == SpeechCache.FileName("Penny", "你好。", "voice B", 175)
    || SpeechCache.FileName("Penny", "你好。", "voice A", 175) == SpeechCache.FileName("Penny", "你好。", "voice A", 180))
    throw new Exception("Voice and rate must invalidate generated cache");
string work = Path.Combine(Path.GetTempPath(), "mandarin-tests-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(work);
try
{
    string empty = Path.Combine(work, "empty.wav");
    File.WriteAllBytes(empty, Array.Empty<byte>());
    if (SpeechCache.HasAudio(empty)) throw new Exception("Empty WAV accepted");
    using (var writer = new BinaryWriter(File.Create(empty)))
    {
        writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write(36u);
        writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEdata")); writer.Write(0u);
    }
    if (SpeechCache.HasAudio(empty)) throw new Exception("Header-only WAV accepted");
    if (args.Contains("--speech-test"))
    {
        using var player = new MacAudioPlayer();
        string destination = Path.Combine(work, "cached.wav");
        player.GenerateAndPlay("你好，这是手动朗读测试。", "Tingting (中文（中国大陆）)", 175, destination, 0f, 0.85f);
        var timer = System.Diagnostics.Stopwatch.StartNew();
        int? result = null;
        while (timer.Elapsed < TimeSpan.FromSeconds(30) && result is null)
        {
            result = player.Reap();
            Thread.Sleep(20);
        }
        if (result != 0 || !File.Exists(destination) || !SpeechCache.HasAudio(destination))
            throw new Exception("macOS local generation/cache/playback failed");
        string cancelled = Path.Combine(work, "cancelled.wav");
        player.GenerateAndPlay(string.Concat(Enumerable.Repeat("这段内容已经取消。", 100)),
            "Tingting (中文（中国大陆）)", 175, cancelled, 0f, 0.85f);
        player.Stop();
        Thread.Sleep(200);
        if (player.Reap() is not null || File.Exists(cancelled)) throw new Exception("Cancelled generation was played or cached");
        if (Directory.GetFiles(work, ".*").Length != 0) throw new Exception("Temporary files left after cancellation");
        Console.WriteLine("macOS offline generation, valid cache, muted playback and cancellation passed.");
    }
}
finally { Directory.Delete(work, recursive: true); }
Console.WriteLine("Speech cache checks passed.");
