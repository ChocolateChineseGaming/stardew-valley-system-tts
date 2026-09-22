using StardewModdingAPI;
using StardewValley.Menus;

namespace MandarinVoice;

internal sealed class AdditionalTextReader
{
    public const string LetterSpeaker = "LetterNarrator";
    private readonly IModHelper helper;

    public AdditionalTextReader(IModHelper helper) => this.helper = helper;

    public List<Narration> ReadLetter(LetterViewerMenu letter)
    {
        object? rawMessage = helper.Reflection.GetField<object>(letter, "mailMessage").GetValue();
        List<string> pages = rawMessage switch
        {
            string message => new() { message },
            IEnumerable<string> values => values.Where(page => page is not null).ToList()!,
            null => new(),
            _ => throw new InvalidDataException(
                $"无法读取信件内容类型：{rawMessage.GetType().FullName}")
        };
        string speaker = LetterSender.Resolve(
            ReadOptional(letter, "mailTitle"),
            ReadOptionalInt(letter, "secretNoteImage", -1),
            ReadOptionalBool(letter, "fromPlayer"),
            pages);
        return pages.Count == 0
            ? new()
            : NarrationText.Create(speaker, null, null, pages);
    }

    private string? ReadOptional(LetterViewerMenu letter, string name)
    {
        try
        {
            return helper.Reflection.GetField<string>(letter, name, required: false)?.GetValue();
        }
        catch (Exception)
        {
            return null;
        }
    }

    private int ReadOptionalInt(LetterViewerMenu letter, string name, int fallback)
    {
        try
        {
            return helper.Reflection.GetField<int>(letter, name, required: false)?.GetValue() ?? fallback;
        }
        catch (Exception)
        {
            return fallback;
        }
    }

    private bool ReadOptionalBool(LetterViewerMenu letter, string name)
    {
        try
        {
            return helper.Reflection.GetField<bool>(letter, name, required: false)?.GetValue() ?? false;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
