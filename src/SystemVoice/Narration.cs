namespace MandarinVoice;

internal sealed record Narration(string Speaker, string Text);

internal static class NarrationText
{
    public static List<Narration> Create(string speaker, string? title, string? description,
        IEnumerable<string>? remaining)
    {
        var result = new List<Narration>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (string? value in new[] { title, description }.Concat(remaining ?? Array.Empty<string>()))
        {
            // Mail uses ^ as a visual line break in some localized content.
            string text = VoiceKey.Normalize((value ?? "").Replace('^', ' '));
            if (text.Length > 0 && seen.Add(text)) result.Add(new Narration(speaker, text));
        }
        return result;
    }
}

internal static class QuestNarration
{
    public const string Speaker = "QuestNarrator";

    public static List<Narration> Create(string? title, string? description, IEnumerable<string>? objectives)
    {
        return NarrationText.Create(Speaker, title, description, objectives);
    }
}

// Observing content never authorizes playback. Only an explicit request queues audio.
internal sealed class NarrationSession
{
    private object? context;
    private string? signature;
    private readonly Queue<Narration> pending = new();

    public bool Update(object menu, IReadOnlyList<Narration> lines)
    {
        string next = string.Join(":", lines.Select(line => VoiceKey.For(line.Speaker, line.Text)));
        if (ReferenceEquals(context, menu) && signature == next) return false;
        context = menu;
        signature = next;
        pending.Clear();
        return true;
    }

    public void Request(object menu, IReadOnlyList<Narration> lines)
    {
        Update(menu, lines);
        pending.Clear();
        foreach (var line in lines) pending.Enqueue(line);
    }

    public Narration? Next() => pending.Count > 0 ? pending.Dequeue() : null;

    public Narration? Peek() => pending.Count > 0 ? pending.Peek() : null;

    public void Clear()
    {
        context = null;
        signature = null;
        pending.Clear();
    }
}
