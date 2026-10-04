using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using ReimaginingAchievements.Common;
using Terraria;
using Terraria.Achievements;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.UI;
using Terraria.UI;
using Terraria.UI.Chat;

namespace ReimaginingAchievements.UI;

public class AchievementRow : UIPanel
{
	public static readonly HashSet<string> Expanded = new HashSet<string>();

	private const float CollapsedHeight = 108f;
	private const float LineHeight = 18f;

	private readonly Achievement _achievement;
	private readonly UIImageFramed _icon;
	private readonly Rectangle _unlockedFrame;
	private readonly Rectangle _lockedFrame;
	private readonly Asset<Texture2D> _panelTop;
	private readonly Asset<Texture2D> _panelBottom;
	private readonly Asset<Texture2D> _categories;
	private const int LockTooltipTime = 180;

	private static Asset<Texture2D> LockTexture => ModContent.Request<Texture2D>("ReimaginingAchievements/UI/Lock");

	private static Asset<Texture2D> LinkTexture => ModContent.Request<Texture2D>("ReimaginingAchievements/UI/ChainLink");

	private List<RequirementLine> _lines = new List<RequirementLine>();
	private bool _expanded;
	private int _lockTooltip;
	private int _lockShake;

	public Achievement Achievement => _achievement;

	public AchievementRow(Achievement achievement)
	{
		_achievement = achievement;
		BackgroundColor = new Color(26, 40, 89) * 0.8f;
		BorderColor = new Color(13, 20, 44) * 0.8f;
		Width.Set(0f, 1f);
		PaddingTop = 8f;
		PaddingLeft = 9f;
		PaddingRight = 8f;
		SetPadding(0f);

		int index = Main.Achievements.GetIconIndex(achievement.Name);
		ModAchievement modAchievement = achievement.ModAchievement;
		if (modAchievement != null)
		{
			_unlockedFrame = new Rectangle(0, index * 66, 64, 64);
			_lockedFrame = new Rectangle(66, index * 66, 64, 64);
		}
		else
		{
			_unlockedFrame = new Rectangle(index % 8 * 66, index / 8 * 66, 64, 64);
			_lockedFrame = _unlockedFrame;
			_lockedFrame.X += 528;
		}

		_icon = new UIImageFramed(modAchievement?.Texture ?? Main.Assets.Request<Texture2D>("Images/UI/Achievements"), _lockedFrame);
		_icon.Left.Set(10f, 0f);
		_icon.Top.Set(16f, 0f);
		Append(_icon);

		var border = new UIImage(Main.Assets.Request<Texture2D>("Images/UI/Achievement_Borders"));
		border.Left.Set(6f, 0f);
		border.Top.Set(12f, 0f);
		Append(border);

		_panelTop = Main.Assets.Request<Texture2D>("Images/UI/Achievement_InnerPanelTop");
		_panelBottom = Main.Assets.Request<Texture2D>("Images/UI/Achievement_InnerPanelBottom_Large");
		_categories = Main.Assets.Request<Texture2D>("Images/UI/Achievement_Categories");
		_expanded = Expanded.Contains(achievement.Name);
		if (_expanded)
			_lines = RequirementLines.Build(achievement);

		Resize();
	}

	public override void LeftClick(UIMouseEvent evt)
	{
		base.LeftClick(evt);
		if (ModLocked && IconRectangle().Contains(evt.MousePosition.ToPoint()))
		{
			_lockTooltip = LockTooltipTime;
			_lockShake = 20;
			SoundEngine.PlaySound(SoundID.Unlock);
			return;
		}

		_expanded = !_expanded;
		if (_expanded)
		{
			Expanded.Add(_achievement.Name);
			_lines = RequirementLines.Build(_achievement);
		}
		else
		{
			Expanded.Remove(_achievement.Name);
			_lines.Clear();
		}

		Resize();
		Parent?.Recalculate();
	}

	public override void Update(GameTime gameTime)
	{
		base.Update(gameTime);
		_icon.SetFrame(_achievement.IsCompleted ? _unlockedFrame : _lockedFrame);
		_icon.Color = ModLocked ? new Color(90, 90, 100) : Color.White;
		if (_lockTooltip > 0)
			_lockTooltip--;

		if (_lockShake > 0)
			_lockShake--;

		if (!_expanded)
			return;

		List<RequirementLine> fresh = RequirementLines.Build(_achievement);
		bool countChanged = fresh.Count != _lines.Count;
		_lines = fresh;
		if (countChanged)
		{
			Resize();
			Parent?.Recalculate();
		}
	}

	public override void MouseOver(UIMouseEvent evt)
	{
		base.MouseOver(evt);
		BackgroundColor = new Color(46, 60, 119);
		BorderColor = new Color(20, 30, 56);
	}

	public override void MouseOut(UIMouseEvent evt)
	{
		base.MouseOut(evt);
		BackgroundColor = new Color(26, 40, 89) * 0.8f;
		BorderColor = new Color(13, 20, 44) * 0.8f;
	}

	public override void Draw(SpriteBatch spriteBatch)
	{
		base.Draw(spriteBatch);
		if (!ModLocked)
			return;

		DrawChains(spriteBatch);
		bool hoverIcon = IconRectangle().Contains(Main.MouseScreen.ToPoint());
		if ((_lockTooltip > 0 || hoverIcon) && _achievement.ModAchievement is IModGated gated)
			UICommon.TooltipMouseText(Language.GetTextValue("Mods.ReimaginingAchievements.UI.RequiresMod", gated.RequiredDisplayName));
	}

	private bool ModLocked => LiveText.IsModLocked(_achievement);

	private Rectangle IconRectangle()
	{
		CalculatedStyle dims = _icon.GetDimensions();
		return new Rectangle((int)dims.X, (int)dims.Y, 64, 64);
	}

	private void DrawChains(SpriteBatch spriteBatch)
	{
		Texture2D link = LinkTexture.Value;
		Texture2D padlock = LockTexture.Value;
		Rectangle icon = IconRectangle();
		float time = (float)Main.timeForVisualEffects / 60f;
		float shake = _lockShake > 0 ? (float)Math.Sin(_lockShake * 1.3f) * _lockShake * 0.12f : 0f;
		float sag = 6f + (float)Math.Sin(time * 2f) * 1.5f + shake * 0.5f;
		bool hoverIcon = icon.Contains(Main.MouseScreen.ToPoint());
		Color chainColor = hoverIcon ? new Color(200, 200, 210) : new Color(150, 150, 160);

		DrawChain(spriteBatch, link, new Vector2(icon.Left + 2, icon.Top + 6), new Vector2(icon.Right - 2, icon.Bottom - 10), sag, chainColor);
		DrawChain(spriteBatch, link, new Vector2(icon.Right - 2, icon.Top + 6), new Vector2(icon.Left + 2, icon.Bottom - 10), sag, chainColor);

		float sway = (float)Math.Sin(time * 2f) * 0.08f + shake * 0.06f;
		Vector2 lockPos = new Vector2(icon.Center.X + shake, icon.Center.Y + 4f);
		Color lockColor = hoverIcon ? Color.White : new Color(225, 225, 225);
		spriteBatch.Draw(padlock, lockPos, null, lockColor, sway, new Vector2(padlock.Width / 2f, 2f), 1f, SpriteEffects.None, 0f);
	}

	private static void DrawChain(SpriteBatch spriteBatch, Texture2D link, Vector2 start, Vector2 end, float sag, Color color)
	{
		float length = Vector2.Distance(start, end);
		int count = Math.Max(2, (int)(length / 6f));
		Vector2 control = (start + end) / 2f + new Vector2(0f, sag);
		Rectangle flat = new Rectangle(0, 0, link.Width, link.Height / 2);
		Rectangle edge = new Rectangle(0, link.Height / 2, link.Width, link.Height / 2);
		for (int i = 0; i <= count; i++)
		{
			float t = i / (float)count;
			Vector2 point = Bezier(start, control, end, t);
			Vector2 next = Bezier(start, control, end, Math.Min(1f, t + 0.01f));
			Vector2 previous = Bezier(start, control, end, Math.Max(0f, t - 0.01f));
			float rotation = (next - previous).ToRotation();
			Rectangle frame = i % 2 == 0 ? flat : edge;
			spriteBatch.Draw(link, point, frame, color, rotation, new Vector2(frame.Width / 2f, frame.Height / 2f), 1f, SpriteEffects.None, 0f);
		}
	}

	private static Vector2 Bezier(Vector2 a, Vector2 b, Vector2 c, float t)
	{
		float u = 1f - t;
		return u * u * a + 2f * u * t * b + t * t * c;
	}

	private void Resize()
	{
		float extra = _expanded ? 10f + HeaderHeight() + _lines.Count * LineHeight : 0f;
		Height.Set(CollapsedHeight + extra, 0f);
	}

	private static float HeaderHeight()
	{
		return 16f;
	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		base.DrawSelf(spriteBatch);
		bool locked = !_achievement.IsCompleted;
		bool hidden = _achievement.Hidden && locked;
		string name = LiveText.Name(_achievement);
		string description = LiveText.Description(_achievement);
		string source = hidden ? "" : AchievementSources.Of(_achievement);

		CalculatedStyle inner = GetInnerDimensions();
		float textX = inner.X + 84f;
		float textWidth = inner.Width - 96f;
		Color titleColor = locked ? Color.Silver : Color.Gold;
		titleColor = Color.Lerp(titleColor, Color.White, IsMouseHovering ? 0.5f : 0f);
		Color bodyColor = locked ? Color.DarkGray : Color.Silver;
		bodyColor = Color.Lerp(bodyColor, Color.White, IsMouseHovering ? 0.35f : 0f);
		Color chrome = IsMouseHovering ? Color.White : Color.Gray;
		var nameScale = new Vector2(0.85f);
		var bodyScale = new Vector2(0.92f);

		Vector2 namePos = new Vector2(textX, inner.Y + 6f);
		DrawPanelTop(spriteBatch, namePos, textWidth, chrome);
		int category = (int)_achievement.Category;
		if (category >= 0 && category < 4)
		{
			spriteBatch.Draw(_categories.Value, namePos + new Vector2(4f, 2f), _categories.Frame(4, 2, category), chrome, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
		}

		Vector2 nameText = namePos + new Vector2(category >= 0 && category < 4 ? 24f : 6f, 2f);
		ChatManager.DrawColorCodedStringWithShadow(spriteBatch, FontAssets.ItemStack.Value, name, nameText, titleColor, 0f, Vector2.Zero, nameScale, textWidth - 28f);

		if (!string.IsNullOrEmpty(source))
		{
			Vector2 sourceSize = ChatManager.GetStringSize(FontAssets.ItemStack.Value, source, nameScale, textWidth);
			Vector2 sourcePos = new Vector2(namePos.X + textWidth - sourceSize.X - 4f, nameText.Y);
			ChatManager.DrawColorCodedStringWithShadow(spriteBatch, FontAssets.ItemStack.Value, source, sourcePos, new Color(255, 220, 70), 0f, Vector2.Zero, nameScale, textWidth);
		}

		string wrapped = FontAssets.ItemStack.Value.CreateWrappedText(description, (textWidth - 16f) * (1f / bodyScale.X), Language.ActiveCulture.CultureInfo);
		Vector2 bodySize = ChatManager.GetStringSize(FontAssets.ItemStack.Value, wrapped, bodyScale, textWidth);
		float bodyRoom = 46f;
		if (bodySize.Y > bodyRoom)
			bodyScale.Y *= bodyRoom / bodySize.Y;

		Vector2 bodyPos = new Vector2(textX, inner.Y + 34f);
		DrawPanelBottom(spriteBatch, bodyPos, textWidth, chrome);
		ChatManager.DrawColorCodedStringWithShadow(spriteBatch, FontAssets.ItemStack.Value, wrapped, bodyPos + new Vector2(8f, 4f), bodyColor, 0f, Vector2.Zero, bodyScale, textWidth - 12f);
		DrawTracker(spriteBatch, namePos, textWidth, titleColor);

		if (!_expanded)
			return;

		float y = inner.Y + CollapsedHeight - 8f;
		string header = Language.GetTextValue("Mods.ReimaginingAchievements.UI.Requirements");
		ChatManager.DrawColorCodedStringWithShadow(spriteBatch, FontAssets.ItemStack.Value, header, new Vector2(textX, y), titleColor, 0f, Vector2.Zero, new Vector2(0.8f), textWidth);
		y += HeaderHeight();
		Texture2D pixel = TextureAssets.MagicPixel.Value;
		foreach (RequirementLine line in _lines)
		{
			Color mark = line.Complete ? new Color(96, 196, 96) : new Color(70, 74, 92);
			spriteBatch.Draw(pixel, new Rectangle((int)textX, (int)y + 3, 10, 10), mark);
			if (line.Complete)
			{
				spriteBatch.Draw(pixel, new Rectangle((int)textX + 2, (int)y + 7, 3, 3), Color.White);
				spriteBatch.Draw(pixel, new Rectangle((int)textX + 4, (int)y + 5, 4, 3), Color.White);
			}

			Color lineColor = line.Complete ? new Color(150, 220, 150) : Color.Gray;
			ChatManager.DrawColorCodedStringWithShadow(spriteBatch, FontAssets.ItemStack.Value, line.Text, new Vector2(textX + 16f, y - 1f), lineColor, 0f, Vector2.Zero, new Vector2(0.8f), textWidth - 20f);
			y += LineHeight;
		}
	}

	private void DrawTracker(SpriteBatch spriteBatch, Vector2 namePos, float textWidth, Color titleColor)
	{
		if (!_achievement.HasTracker || _achievement.IsCompleted)
			return;

		IAchievementTracker tracker = _achievement.GetTracker();
		float value = 0f;
		float max = 0f;
		PropertyInfo valueProperty = tracker.GetType().GetProperty("Value");
		PropertyInfo maxProperty = tracker.GetType().GetProperty("MaxValue");
		if (valueProperty == null || maxProperty == null)
			return;

		object rawValue = valueProperty.GetValue(tracker);
		object rawMax = maxProperty.GetValue(tracker);
		if (rawValue is int intValue && rawMax is int intMax)
		{
			value = intValue;
			max = intMax;
		}
		else if (rawValue is float floatValue && rawMax is float floatMax)
		{
			value = floatValue;
			max = floatMax;
		}

		if (max <= 0f)
			return;

		string label = (int)value + "/" + (int)max;
		float progress = MathHelper.Clamp(value / max, 0f, 1f);
		Texture2D pixel = TextureAssets.MagicPixel.Value;
		var bar = new Rectangle((int)(namePos.X + textWidth - 92f), (int)namePos.Y + 18, 84, 8);
		spriteBatch.Draw(pixel, bar, new Color(16, 18, 32));
		spriteBatch.Draw(pixel, new Rectangle(bar.X, bar.Y, (int)(bar.Width * progress), bar.Height), new Color(255, 214, 70));
		var labelScale = new Vector2(0.7f);
		Vector2 labelSize = ChatManager.GetStringSize(FontAssets.ItemStack.Value, label, labelScale);
		ChatManager.DrawColorCodedStringWithShadow(spriteBatch, FontAssets.ItemStack.Value, label, new Vector2(bar.X - labelSize.X - 4f, bar.Y - 4f), titleColor, 0f, Vector2.Zero, labelScale, 80f);
	}

	private void DrawPanelTop(SpriteBatch spriteBatch, Vector2 position, float width, Color color)
	{
		Texture2D texture = _panelTop.Value;
		spriteBatch.Draw(texture, position, new Rectangle(0, 0, 2, texture.Height), color);
		spriteBatch.Draw(texture, new Vector2(position.X + 2f, position.Y), new Rectangle(2, 0, 2, texture.Height), color, 0f, Vector2.Zero, new Vector2((width - 4f) / 2f, 1f), SpriteEffects.None, 0f);
		spriteBatch.Draw(texture, new Vector2(position.X + width - 2f, position.Y), new Rectangle(4, 0, 2, texture.Height), color);
	}

	private void DrawPanelBottom(SpriteBatch spriteBatch, Vector2 position, float width, Color color)
	{
		Texture2D texture = _panelBottom.Value;
		spriteBatch.Draw(texture, position, new Rectangle(0, 0, 6, texture.Height), color);
		spriteBatch.Draw(texture, new Vector2(position.X + 6f, position.Y), new Rectangle(6, 0, 7, texture.Height), color, 0f, Vector2.Zero, new Vector2((width - 12f) / 7f, 1f), SpriteEffects.None, 0f);
		spriteBatch.Draw(texture, new Vector2(position.X + width - 6f, position.Y), new Rectangle(13, 0, 6, texture.Height), color);
	}
}
