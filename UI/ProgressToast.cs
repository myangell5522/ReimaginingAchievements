using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReimaginingAchievements.Common;
using Terraria;
using Terraria.Achievements;
using Terraria.GameContent;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI.Chat;

namespace ReimaginingAchievements.UI;

public class ProgressToastSystem : ModSystem
{
	private bool _hooked;

	public override void PostSetupContent()
	{
		TryHook();
	}

	public override void OnWorldLoad()
	{
		TryHook();
	}

	public override void PostUpdateEverything()
	{
		if (ProgressToast.ArmTimer > 0)
			ProgressToast.ArmTimer--;
	}

	public override void PostDrawInterface(SpriteBatch spriteBatch)
	{
		ProgressToast.Draw(spriteBatch);
	}

	public override void Unload()
	{
		ProgressToast.Reset();
	}

	private void TryHook()
	{
		if (_hooked || Main.Achievements == null)
			return;

		foreach (Achievement achievement in Main.Achievements.CreateAchievementsList())
		{
			foreach (AchievementCondition condition in RequirementLines.GetConditions(achievement))
			{
				Achievement capturedAchievement = achievement;
				condition.OnComplete += _ =>
				{
					if (ProgressToast.ArmTimer > 0 || capturedAchievement.IsCompleted)
						return;

					ProgressToast.Push(capturedAchievement, RequirementLines.Describe(capturedAchievement, condition));
				};
			}
		}

		_hooked = true;
	}
}

public static class ProgressToast
{
	public const int ArmTimerStart = 180;

	private const int MaxLines = 4;
	private const int Duration = 300;

	public static int ArmTimer = ArmTimerStart;

	private static string _title = "";
	private static readonly List<string> Lines = new List<string>();
	private static int _extra;
	private static int _timer;

	public static void Push(Achievement achievement, string line)
	{
		if (achievement == null || string.IsNullOrEmpty(line) || ArmTimer > 0 || Main.gameMenu)
			return;

		string title = LiveText.Name(achievement);
		if (_title != title)
		{
			_title = title;
			Lines.Clear();
			_extra = 0;
		}

		if (Lines.Contains(line))
		{
			_timer = Duration;
			return;
		}

		if (Lines.Count < MaxLines)
			Lines.Add(line);
		else
			_extra++;

		_timer = Duration;
	}

	public static void Reset()
	{
		_title = "";
		Lines.Clear();
		_extra = 0;
		_timer = 0;
		ArmTimer = ArmTimerStart;
	}

	public static void Draw(SpriteBatch spriteBatch)
	{
		if (_timer <= 0 || Lines.Count == 0 || Main.gameMenu)
			return;

		_timer--;
		float alpha = _timer < 30 ? _timer / 30f : 1f;
		if (alpha <= 0f)
			return;

		var font = FontAssets.MouseText.Value;
		float scale = 0.85f;
		float width = 280f;
		foreach (string line in Lines)
		{
			float measured = ChatManager.GetStringSize(font, line, new Vector2(scale), width - 28f).X + 36f;
			if (measured > width)
				width = MathHelper.Min(measured, 420f);
		}

		float titleWidth = ChatManager.GetStringSize(font, _title, new Vector2(0.95f), width - 16f).X + 24f;
		if (titleWidth > width)
			width = MathHelper.Min(titleWidth, 420f);

		int shown = Lines.Count + (_extra > 0 ? 1 : 0);
		float height = 36f + shown * 20f;
		float x = Main.screenWidth - width - 24f;
		float y = 72f;
		var panel = new Rectangle((int)x, (int)y, (int)width, (int)height);
		Texture2D pixel = TextureAssets.MagicPixel.Value;
		Color back = new Color(33, 43, 79) * (0.92f * alpha);
		Color border = new Color(10, 12, 28) * alpha;
		spriteBatch.Draw(pixel, panel, back);
		spriteBatch.Draw(pixel, new Rectangle(panel.X, panel.Y, panel.Width, 2), border);
		spriteBatch.Draw(pixel, new Rectangle(panel.X, panel.Bottom - 2, panel.Width, 2), border);
		spriteBatch.Draw(pixel, new Rectangle(panel.X, panel.Y, 2, panel.Height), border);
		spriteBatch.Draw(pixel, new Rectangle(panel.Right - 2, panel.Y, 2, panel.Height), border);

		ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, _title, new Vector2(x + 12f, y + 8f), Color.Gold * alpha, 0f, Vector2.Zero, new Vector2(0.95f), width - 20f);
		float lineY = y + 32f;
		foreach (string line in Lines)
		{
			spriteBatch.Draw(pixel, new Rectangle((int)x + 12, (int)lineY + 3, 8, 8), new Color(90, 190, 90) * alpha);
			ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, line, new Vector2(x + 26f, lineY - 2f), new Color(190, 230, 190) * alpha, 0f, Vector2.Zero, new Vector2(scale), width - 36f);
			lineY += 20f;
		}

		if (_extra > 0)
		{
			string more = Language.GetTextValue("Mods.ReimaginingAchievements.UI.More", _extra);
			ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, more, new Vector2(x + 26f, lineY - 2f), Color.Gray * alpha, 0f, Vector2.Zero, new Vector2(scale), width - 36f);
		}
	}
}
