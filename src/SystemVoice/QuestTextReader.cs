using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Quests;

namespace MandarinVoice;

internal sealed class QuestTextReader
{
    private readonly IModHelper helper;
    public QuestTextReader(IModHelper helper) => this.helper = helper;

    public List<Narration> Read(IClickableMenu menu)
    {
        if (menu is QuestLog log)
        {
            // _shownQuest can remain populated after returning to the list.
            if (helper.Reflection.GetField<int>(log, "questPage").GetValue() < 0) return new();
            IQuest? quest = helper.Reflection.GetField<IQuest>(log, "_shownQuest").GetValue();
            return quest is null ? new() : QuestNarration.Create(
                quest.GetName(), quest.GetDescription(), quest.GetObjectiveDescriptions());
        }
        if (menu is Billboard board
            && helper.Reflection.GetField<bool>(board, "dailyQuestBoard").GetValue())
        {
            // Match the game's 'nothing posted' condition; the calendar shares this menu class.
            var quest = Game1.questOfTheDay;
            if (quest is null || string.IsNullOrEmpty(quest.currentObjective)) return new();
            return QuestNarration.Create(null, quest.questDescription, null);
        }
        return new();
    }
}
