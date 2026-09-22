using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;

namespace MandarinVoice;

internal sealed class VoiceSettingsMenu : IClickableMenu
{
    private readonly ModConfig config;
    private readonly Action save;
    private int page;
    private readonly Rectangle learningTab;
    private readonly Rectangle contentTab;
    private readonly Rectangle slower;
    private readonly Rectangle faster;
    private readonly Rectangle speechSlower;
    private readonly Rectangle speechFaster;
    private readonly Rectangle quieter;
    private readonly Rectangle louder;
    private readonly Rectangle quests;
    private readonly Rectangle letters;
    private readonly Rectangle television;

    public VoiceSettingsMenu(ModConfig config, Action save)
        : base(Game1.uiViewport.Width / 2 - 360, Game1.uiViewport.Height / 2 - 240,
            720, 480, showUpperRightCloseButton: true)
    {
        this.config = config;
        this.save = save;
        learningTab = new Rectangle(xPositionOnScreen + 100, yPositionOnScreen + 52, 220, 52);
        contentTab = new Rectangle(xPositionOnScreen + 336, yPositionOnScreen + 52, 220, 52);
        slower = new Rectangle(xPositionOnScreen + 290, yPositionOnScreen + 140, 54, 48);
        faster = new Rectangle(xPositionOnScreen + 500, yPositionOnScreen + 140, 54, 48);
        speechSlower = new Rectangle(xPositionOnScreen + 290, yPositionOnScreen + 214, 54, 48);
        speechFaster = new Rectangle(xPositionOnScreen + 500, yPositionOnScreen + 214, 54, 48);
        quieter = new Rectangle(xPositionOnScreen + 290, yPositionOnScreen + 288, 54, 48);
        louder = new Rectangle(xPositionOnScreen + 500, yPositionOnScreen + 288, 54, 48);
        quests = new Rectangle(xPositionOnScreen + 84, yPositionOnScreen + 142, 552, 52);
        letters = new Rectangle(xPositionOnScreen + 84, yPositionOnScreen + 212, 552, 52);
        television = new Rectangle(xPositionOnScreen + 84, yPositionOnScreen + 282, 552, 52);
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        if (learningTab.Contains(x, y)) { page = 0; Click(); return; }
        if (contentTab.Contains(x, y)) { page = 1; Click(); return; }
        if (page == 0)
        {
            if (slower.Contains(x, y)) SetPlaybackRate(config.PlaybackRate - 0.05f);
            else if (faster.Contains(x, y)) SetPlaybackRate(config.PlaybackRate + 0.05f);
            else if (speechSlower.Contains(x, y)) SetSpeechRate(config.SpeechRate - 10);
            else if (speechFaster.Contains(x, y)) SetSpeechRate(config.SpeechRate + 10);
            else if (quieter.Contains(x, y)) SetVolume(config.Volume - 0.1f);
            else if (louder.Contains(x, y)) SetVolume(config.Volume + 0.1f);
            else base.receiveLeftClick(x, y, playSound);
            return;
        }
        if (quests.Contains(x, y)) Toggle(() => config.ReadQuestText = !config.ReadQuestText);
        else if (letters.Contains(x, y)) Toggle(() => config.ReadLetters = !config.ReadLetters);
        else if (television.Contains(x, y))
            Toggle(() => config.ReadNonNpcDialogue = !config.ReadNonNpcDialogue);
        else base.receiveLeftClick(x, y, playSound);
    }

    public override void receiveKeyPress(Keys key)
    {
        if (key == Keys.Escape) exitThisMenu();
        else base.receiveKeyPress(key);
    }

    protected override void cleanupBeforeExit()
    {
        save();
        base.cleanupBeforeExit();
    }

    private void SetPlaybackRate(float value)
    {
        config.PlaybackRate = MathF.Round(Math.Clamp(value, 0.7f, 1.15f), 2);
        SaveClick();
    }

    private void SetSpeechRate(int value)
    {
        config.SpeechRate = Math.Clamp(value, 80, 350);
        SaveClick();
    }

    private void SetVolume(float value)
    {
        config.Volume = MathF.Round(Math.Clamp(value, 0f, 1f), 1);
        SaveClick();
    }

    private void Toggle(Action change)
    {
        change();
        SaveClick();
    }

    private void SaveClick()
    {
        save();
        Click();
    }

    private static void Click() => Game1.playSound("smallSelect");

    public override void draw(SpriteBatch b)
    {
        b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds,
            Color.Black * 0.5f);
        Game1.drawDialogueBox(xPositionOnScreen, yPositionOnScreen, width, height, false, true);
        DrawText(b, "macOS 系统普通话配音", xPositionOnScreen + 62,
            yPositionOnScreen + 14, Game1.textColor, Game1.dialogueFont);
        DrawTab(b, learningTab, "声音设置", page == 0);
        DrawTab(b, contentTab, "朗读内容", page == 1);
        if (page == 0) DrawLearningPage(b);
        else DrawContentPage(b);
        upperRightCloseButton?.draw(b);
        drawMouse(b);
    }

    private void DrawLearningPage(SpriteBatch b)
    {
        DrawText(b, "播放速度", xPositionOnScreen + 84, yPositionOnScreen + 148);
        DrawButton(b, slower, "－");
        DrawText(b, $"{config.PlaybackRate:0.00}×", xPositionOnScreen + 390,
            yPositionOnScreen + 148, Color.DarkSlateBlue);
        DrawButton(b, faster, "＋");
        DrawText(b, "系统语速", xPositionOnScreen + 84, yPositionOnScreen + 222);
        DrawButton(b, speechSlower, "－");
        DrawText(b, config.SpeechRate.ToString(), xPositionOnScreen + 402,
            yPositionOnScreen + 222, Color.DarkSlateBlue);
        DrawButton(b, speechFaster, "＋");
        DrawText(b, "总音量", xPositionOnScreen + 84, yPositionOnScreen + 296);
        DrawButton(b, quieter, "－");
        DrawText(b, $"{config.Volume:P0}", xPositionOnScreen + 398,
            yPositionOnScreen + 296, Color.DarkSlateBlue);
        DrawButton(b, louder, "＋");
        DrawText(b, $"默认声音：{config.FallbackVoice}", xPositionOnScreen + 84,
            yPositionOnScreen + 370, Color.SaddleBrown, Game1.smallFont);
    }

    private void DrawContentPage(SpriteBatch b)
    {
        DrawToggle(b, quests, "朗读任务文本", config.ReadQuestText);
        DrawToggle(b, letters, "朗读信件", config.ReadLetters);
        DrawToggle(b, television, "朗读电视节目和无角色对白", config.ReadNonNpcDialogue);
        DrawText(b, "朗读始终由 F8 手动触发；设置会立即保存。",
            xPositionOnScreen + 84, yPositionOnScreen + 376, Color.SaddleBrown,
            Game1.smallFont);
    }

    private static void DrawText(SpriteBatch b, string text, int x, int y,
        Color? color = null, SpriteFont? font = null) =>
        b.DrawString(font ?? Game1.smallFont, text, new Vector2(x, y),
            color ?? Game1.textColor);

    private static void DrawTab(SpriteBatch b, Rectangle area, string label, bool selected)
    {
        DrawPanel(b, area, selected ? new Color(255, 225, 145) : new Color(205, 180, 135));
        DrawText(b, label, area.X + 54, area.Y + 10,
            selected ? Color.SaddleBrown : Game1.textColor);
    }

    private static void DrawButton(SpriteBatch b, Rectangle area, string label)
    {
        DrawPanel(b, area, new Color(242, 211, 155));
        Vector2 size = Game1.smallFont.MeasureString(label);
        DrawText(b, label, area.Center.X - (int)size.X / 2,
            area.Center.Y - (int)size.Y / 2);
    }

    private static void DrawToggle(SpriteBatch b, Rectangle area, string label, bool enabled)
    {
        DrawPanel(b, area, new Color(242, 221, 175));
        DrawText(b, enabled ? "✓" : "○", area.X + 18, area.Y + 12,
            enabled ? Color.DarkGreen : Color.Gray);
        DrawText(b, label, area.X + 66, area.Y + 12);
    }

    private static void DrawPanel(SpriteBatch b, Rectangle area, Color color)
    {
        b.Draw(Game1.staminaRect, area, color);
        b.Draw(Game1.staminaRect, new Rectangle(area.X, area.Y, area.Width, 3), Color.SaddleBrown);
        b.Draw(Game1.staminaRect, new Rectangle(area.X, area.Bottom - 3, area.Width, 3), Color.SaddleBrown);
        b.Draw(Game1.staminaRect, new Rectangle(area.X, area.Y, 3, area.Height), Color.SaddleBrown);
        b.Draw(Game1.staminaRect, new Rectangle(area.Right - 3, area.Y, 3, area.Height), Color.SaddleBrown);
    }
}
