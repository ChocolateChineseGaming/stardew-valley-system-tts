using StardewModdingAPI;

namespace MandarinVoice;

public sealed class ModConfig
{
    public bool Enabled { get; set; } = true;
    public bool ReadQuestText { get; set; } = true;
    public bool ReadLetters { get; set; } = true;
    public bool ReadNonNpcDialogue { get; set; } = true;
    public string FallbackVoice { get; set; } = "Tingting";
    public bool UseNpcVoices { get; set; } = false;
    public int SpeechRate { get; set; } = 175;
    public Dictionary<string, string> NpcVoices { get; set; } = new()
    {
        ["Penny"] = "Tingting",
        ["Abigail"] = "Flo (中文（中国大陆）)",
        ["Sebastian"] = "Reed (中文（中国大陆）)",
        ["Lewis"] = "Grandpa (中文（中国大陆）)"
    };
    public float Volume { get; set; } = 1f;
    public float PlaybackRate { get; set; } = 0.85f;
    public Dictionary<string, float> NpcVolume { get; set; } = new();
    public SButton ReplayKey { get; set; } = SButton.F8;
    public SButton ConfigKey { get; set; } = SButton.F10;
    public bool CaptureMissingDialogue { get; set; } = true;
}
