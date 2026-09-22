using System.Diagnostics;
using System.Text.RegularExpressions;

namespace MandarinVoice;

internal static class SystemVoiceCatalog
{
    private static readonly Regex VoiceLine = new(
        @"^(?<name>.+?)\s+(?<locale>[a-z]{2}_[A-Z]{2})\s+#",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static IReadOnlyList<string> DiscoverMandarinVoices()
    {
        var start = new ProcessStartInfo("/usr/bin/say")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        start.ArgumentList.Add("-v");
        start.ArgumentList.Add("?");
        using Process process = Process.Start(start)
            ?? throw new IOException("无法读取 macOS 系统声音列表。");
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0)
            throw new IOException($"读取 macOS 系统声音失败：{error.Trim()}");
        return Parse(output, "zh_CN");
    }

    internal static IReadOnlyList<string> Parse(string output, string locale)
    {
        var voices = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (string line in output.Split(new[] { "\r\n", "\n" },
                     StringSplitOptions.RemoveEmptyEntries))
        {
            Match match = VoiceLine.Match(line);
            if (!match.Success || match.Groups["locale"].Value != locale) continue;
            string name = match.Groups["name"].Value.Trim();
            if (name.Length > 0 && seen.Add(name)) voices.Add(name);
        }
        return voices;
    }

    internal static string? Resolve(IReadOnlyList<string> voices, string configured)
    {
        foreach (string voice in voices)
            if (string.Equals(voice, configured, StringComparison.Ordinal)) return voice;
        string shortName = ShortName(configured);
        foreach (string voice in voices)
            if (string.Equals(ShortName(voice), shortName, StringComparison.Ordinal)) return voice;
        return null;
    }

    internal static string ShortName(string voice)
    {
        int suffix = voice.IndexOf(" (", StringComparison.Ordinal);
        return suffix > 0 ? voice[..suffix] : voice;
    }
}
