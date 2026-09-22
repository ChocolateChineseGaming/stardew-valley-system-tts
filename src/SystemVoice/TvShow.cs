namespace MandarinVoice;

internal static class TvShow
{
    public const string FortuneSpeaker = "FortuneTeller";
    public const string CookingSpeaker = "QueenOfSauce";
    public const string DefaultSpeaker = "GameNarrator";

    public static string SpeakerFor(string? channel, string text)
    {
        if (IsFortuneChannel(channel)) return FortuneSpeaker;
        if (IsCookingChannel(channel)) return CookingSpeaker;
        if (!string.IsNullOrWhiteSpace(channel) && channel.Trim() != "0")
            return DefaultSpeaker;
        if (IsFortuneText(text)) return FortuneSpeaker;
        if (IsCookingText(text)) return CookingSpeaker;
        return DefaultSpeaker;
    }

    internal static bool IsFortuneChannel(string? channel)
    {
        if (string.IsNullOrWhiteSpace(channel)) return false;
        string value = channel.Trim();
        return value == "2"
            || value.Contains("fortune", StringComparison.OrdinalIgnoreCase);
    }

    internal static bool IsCookingChannel(string? channel)
    {
        if (string.IsNullOrWhiteSpace(channel)) return false;
        string value = channel.Trim();
        return value == "4"
            || value.Contains("cooking", StringComparison.OrdinalIgnoreCase)
            || value.Contains("sauce", StringComparison.OrdinalIgnoreCase)
            || (value.Contains("queen", StringComparison.OrdinalIgnoreCase)
                && !value.Contains("fortune", StringComparison.OrdinalIgnoreCase));
    }

    internal static bool IsFortuneText(string text) =>
        ContainsAny(text, "占卜", "算命", "占星", "Welwick", "Fortune Teller");

    internal static bool IsCookingText(string text) =>
        ContainsAny(text, "酱汁女皇", "食谱", "Queen of Sauce");

    private static bool ContainsAny(string text, params string[] tokens)
    {
        foreach (string token in tokens)
        {
            if (text.Contains(token, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
