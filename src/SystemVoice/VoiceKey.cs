using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace MandarinVoice;

public static class VoiceKey
{
    // Keep punctuation and names. Only collapse ASCII layout whitespace.
    public static string Normalize(string text) => Regex.Replace(
        text.Normalize(NormalizationForm.FormC), "[ \\t\\r\\n\\f\\v]+", " ").Trim(' ');

    public static string For(string npc, string text) => Convert.ToHexString(
        SHA256.HashData(Encoding.UTF8.GetBytes(npc + "\n" + Normalize(text))))
        .ToLowerInvariant();

    // Remove visual list/separator marks which TTS engines may pronounce literally.
    // Keep the original text for cache identity and screen-change detection.
    public static string ForSpeech(string text) => Regex.Replace(
        Regex.Replace(Normalize(text), "[-‐‑‒–—―−•‣▪●*]+", " "),
        "[ \\t\\r\\n\\f\\v]+", " ").Trim(' ');
}
