namespace MandarinVoice;

internal static class LetterSender
{
    public const string Fallback = "LetterNarrator";

    private static readonly string[] NpcIds =
    {
        "ProfessorSnail", "Demetrius", "Sebastian", "Caroline", "Abigail",
        "Governor", "Grandpa", "Gunther", "Henchman", "Bouncer", "Elliott",
        "Harvey", "Krobus", "Linus", "Marlon", "Marnie", "Morris", "Pierre",
        "Robin", "Sandy", "Shane", "Vincent", "Willy", "Wizard", "Birdie",
        "Clint", "Dwarf", "Emily", "Evelyn", "George", "Haley", "Jodi",
        "Kent", "Leah", "Lewis", "Maru", "Penny", "Alex", "Gil", "Gus",
        "Jas", "Leo", "Pam", "Sam", "MrQi"
    };

    private static readonly Dictionary<string, string> Aliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Qi"] = "MrQi",
        ["齐先生"] = "MrQi",
        ["阿比盖尔"] = "Abigail",
        ["亚历克斯"] = "Alex",
        ["伯蒂"] = "Birdie",
        ["保镖"] = "Bouncer",
        ["卡洛琳"] = "Caroline",
        ["克林特"] = "Clint",
        ["德米特里厄斯"] = "Demetrius",
        ["矮人"] = "Dwarf",
        ["艾利欧特"] = "Elliott",
        ["艾米丽"] = "Emily",
        ["伊芙琳"] = "Evelyn",
        ["乔治"] = "George",
        ["吉尔"] = "Gil",
        ["州长"] = "Governor",
        ["爷爷"] = "Grandpa",
        ["冈瑟"] = "Gunther",
        ["格斯"] = "Gus",
        ["海莉"] = "Haley",
        ["哈维"] = "Harvey",
        ["打手"] = "Henchman",
        ["贾斯"] = "Jas",
        ["乔迪"] = "Jodi",
        ["肯特"] = "Kent",
        ["科罗巴斯"] = "Krobus",
        ["莉亚"] = "Leah",
        ["雷欧"] = "Leo",
        ["刘易斯"] = "Lewis",
        ["莱纳斯"] = "Linus",
        ["马龙"] = "Marlon",
        ["玛妮"] = "Marnie",
        ["玛鲁"] = "Maru",
        ["莫里斯"] = "Morris",
        ["潘姆"] = "Pam",
        ["潘妮"] = "Penny",
        ["皮埃尔"] = "Pierre",
        ["蜗牛教授"] = "ProfessorSnail",
        ["罗宾"] = "Robin",
        ["山姆"] = "Sam",
        ["桑迪"] = "Sandy",
        ["塞巴斯蒂安"] = "Sebastian",
        ["谢恩"] = "Shane",
        ["文森特"] = "Vincent",
        ["威利"] = "Willy",
        ["法师"] = "Wizard",
        ["巫师"] = "Wizard"
    };

    static LetterSender()
    {
        foreach (string npc in NpcIds)
            Aliases.TryAdd(npc, npc);
    }

    public static string Resolve(string? mailTitle, int secretNoteImage, bool fromPlayer,
        IEnumerable<string>? pages)
    {
        if (fromPlayer || secretNoteImage >= 0) return Fallback;
        if (TryMatch(mailTitle, out string npc)) return npc;
        if (TryFromSignature(pages, out npc)) return npc;
        return Fallback;
    }

    internal static bool TryMatch(string? value, out string npc)
    {
        npc = Fallback;
        if (string.IsNullOrWhiteSpace(value)) return false;
        string text = value.Trim();
        if (Aliases.TryGetValue(text, out string? exact))
        {
            npc = exact;
            return true;
        }

        string? best = null;
        foreach (var pair in Aliases)
        {
            if (text.StartsWith(pair.Key, StringComparison.OrdinalIgnoreCase)
                && (best is null || pair.Key.Length > best.Length))
                best = pair.Key;
        }
        if (best is null) return false;
        npc = Aliases[best];
        return true;
    }

    internal static bool TryFromSignature(IEnumerable<string>? pages, out string npc)
    {
        npc = Fallback;
        if (pages is null) return false;
        string? last = null;
        foreach (string page in pages)
        {
            if (!string.IsNullOrWhiteSpace(page)) last = page;
        }
        if (last is null) return false;
        string text = last.Replace('^', '\n');
        foreach (string line in text.Split('\n').Reverse())
        {
            string trimmed = line.Trim();
            if (trimmed.Length == 0) continue;
            if (trimmed.StartsWith('-') || trimmed.StartsWith('–') || trimmed.StartsWith('—'))
                trimmed = trimmed.TrimStart('-', '–', '—', ' ').Trim();
            if (TryMatch(trimmed, out npc)) return true;
            int dash = Math.Max(trimmed.LastIndexOf('-'),
                Math.Max(trimmed.LastIndexOf('–'), trimmed.LastIndexOf('—')));
            if (dash >= 0 && TryMatch(trimmed[(dash + 1)..].Trim(), out npc))
                return true;
        }
        return false;
    }
}
