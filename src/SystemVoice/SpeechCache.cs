using System.Text;

namespace MandarinVoice;

internal static class SpeechCache
{
    public static string FileName(string speaker, string text, string voice, int rate) =>
        VoiceKey.For(speaker, text) + "-" + VoiceKey.For(voice, rate.ToString(System.Globalization.CultureInfo.InvariantCulture)) + ".wav";

    public static bool HasAudio(string path)
    {
        using var stream = File.OpenRead(path);
        using var reader = new BinaryReader(stream, Encoding.ASCII);
        if (stream.Length < 12 || new string(reader.ReadChars(4)) != "RIFF") return false;
        reader.ReadUInt32();
        if (new string(reader.ReadChars(4)) != "WAVE") return false;
        while (stream.Position + 8 <= stream.Length)
        {
            string chunk = new(reader.ReadChars(4));
            uint size = reader.ReadUInt32();
            if (size > stream.Length - stream.Position) return false;
            if (chunk == "data") return size > 0;
            stream.Position += size + (size % 2);
        }
        return false;
    }
}
