using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReimaginingAchievements.Common;
using ReLogic.Content;
using Terraria;
using Terraria.Achievements;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader.UI;
using Terraria.UI;
using Terraria.UI.Chat;
using Terraria.UI.Gamepad;

namespace ReimaginingAchievements.UI;

public class AchievementsMenuState : UIState, IHaveBackButtonCommand
{
	private enum CompletionFilter
	{
		All,
		Done,
		Todo
	}

	private const float RailWidth = 170f;

	private readonly List<UIToggleImage> _categoryButtons = new List<UIToggleImage>();
	private UIList _list;
	private UISearchBar _search;
	private UIPanel _searchPanel;
	private int _escapeGuard;
	private string _query = "";
	private UITextPanel<string> _filterButton;
	private UITextPanel<LocalizedText> _backButton;
	private UITextPanel<LocalizedText> _resetButton;
	private ProgressHeader _progress;
	private UIList _sourceList;
	private UIToggleImage _moddedButton;
	private UIElement _outer;
	private UIImage _blockInput;
	private UIPanel _confirm;
	private CompletionFilter _completion = CompletionFilter.All;
	private string _source;

	public Achievement GotoTarget { get; set; }

	public UIState PreviousUIState { get; set; }

	public override void OnActivate()
	{
		Main.clrInput();
		AchievementRow.Expanded.Clear();
		Build();
		if (GotoTarget != null)
		{
			Achievement target = GotoTarget;
			GotoTarget = null;
			_list.Goto(element => element is AchievementRow row && row.Achievement == target);
		}
	}

	public override void OnDeactivate()
	{
		AchievementRow.Expanded.Clear();
	}

	public override void Update(GameTime gameTime)
	{
		bool typing = _search != null && _search.IsWritingText || _escapeGuard > 0;
		if (_escapeGuard > 0)
			_escapeGuard--;

		base.Update(gameTime);
		if (Main.inputTextEscape && !Main.gameMenu && !typing)
			HandleBackButtonUsage();
	}

	public override void LeftMouseDown(UIMouseEvent evt)
	{
		base.LeftMouseDown(evt);
		if (_search != null && _search.IsWritingText && !_searchPanel.ContainsPoint(evt.MousePosition))
			_search.ToggleTakingText();
	}

	public void HandleBackButtonUsage()
	{
		if (_blockInput != null && HasChild(_blockInput))
		{
			CloseConfirm(null, null);
			return;
		}

		Main.menuMode = 0;
		IngameFancyUI.Close();
	}

	public override void Draw(SpriteBatch spriteBatch)
	{
		base.Draw(spriteBatch);
		DrawCategoryTooltip(spriteBatch);
		UILinkPointNavigator.Shortcuts.BackButtonCommand = 7;
	}

	private void Build()
	{
		RemoveAllChildren();
		_categoryButtons.Clear();
		_blockInput = null;
		_confirm = null;

		float top = Main.gameMenu ? 220f : 120f;
		_outer = new UIElement();
		_outer.Width.Set(0f, 0.88f);
		_outer.MaxWidth.Set(1040f, 0f);
		_outer.MinWidth.Set(820f, 0f);
		_outer.Top.Set(top, 0f);
		_outer.Height.Set(-top, 1f);
		_outer.HAlign = 0.5f;
		Append(_outer);

		_backButton = new UITextPanel<LocalizedText>(Language.GetText("UI.Back"), 0.7f, true);
		_backButton.Width.Set(-10f, 0.72f);
		_backButton.Height.Set(50f, 0f);
		_backButton.VAlign = 1f;
		_backButton.HAlign = 0f;
		_backButton.Top.Set(-45f, 0f);
		_backButton.OnMouseOver += FadeIn;
		_backButton.OnMouseOut += FadeOut;
		_backButton.OnLeftClick += (_, _) =>
		{
			Main.menuMode = 0;
			IngameFancyUI.Close();
		};
		_outer.Append(_backButton);

		_resetButton = new UITextPanel<LocalizedText>(Language.GetText("tModLoader.AchievementsReset"), 0.7f, true);
		_resetButton.Width.Set(-10f, 0.28f);
		_resetButton.Height.Set(50f, 0f);
		_resetButton.VAlign = 1f;
		_resetButton.HAlign = 1f;
		_resetButton.Top.Set(-45f, 0f);
		_resetButton.OnMouseOver += FadeIn;
		_resetButton.OnMouseOut += FadeOut;
		_resetButton.OnLeftClick += OpenConfirm;
		_outer.Append(_resetButton);

		var panel = new UIPanel();
		panel.Width.Set(0f, 1f);
		panel.Height.Set(-110f, 1f);
		panel.BackgroundColor = new Color(33, 43, 79) * 0.8f;
		panel.PaddingTop = 0f;
		_outer.Append(panel);

		BuildRail(panel);
		BuildContent(panel);

		// Appended after the panel so the panel doesn't draw over its lower half.
		var title = new UITextPanel<LocalizedText>(Language.GetText("UI.Achievements"), 1f, true);
		title.HAlign = 0.5f;
		title.Top.Set(-33f, 0f);
		title.SetPadding(13f);
		title.BackgroundColor = new Color(73, 94, 171);
		_outer.Append(title);

		Refill();
	}

	private void BuildRail(UIElement panel)
	{
		var rail = new UIElement();
		rail.Width.Set(RailWidth, 0f);
		rail.Height.Set(-8f, 1f);
		rail.Top.Set(4f, 0f);
		panel.Append(rail);

		var sourcesLabel = new UIText(T("UI.Sources"), 0.8f);
		sourcesLabel.Top.Set(8f, 0f);
		sourcesLabel.HAlign = 0.5f;
		rail.Append(sourcesLabel);

		_sourceList = new UIList();
		_sourceList.Top.Set(32f, 0f);
		_sourceList.Width.Set(0f, 1f);
		_sourceList.Height.Set(-32f, 1f);
		_sourceList.ListPadding = 2f;
		_sourceList.OverflowHidden = true;
		rail.Append(_sourceList);

		var seen = new HashSet<string>();
		foreach (Achievement achievement in Main.Achievements.CreateAchievementsList())
		{
			string source = AchievementSources.Of(achievement);
			if (!seen.Add(source))
				continue;

			string captured = source;
			var button = new UITextPanel<string>(captured, 0.62f);
			button.Width.Set(0f, 1f);
			button.Height.Set(26f, 0f);
			button.OnMouseOver += FadeIn;
			button.OnMouseOut += (evt, _) =>
			{
				if (evt.Target is not UIPanel sourcePanel)
					return;

				bool selected = sourcePanel is UITextPanel<string> text && text.Text == _source;
				sourcePanel.BackgroundColor = selected ? new Color(73, 94, 171) : new Color(63, 82, 151) * 0.8f;
				sourcePanel.BorderColor = Color.Black;
			};
			button.OnLeftClick += (_, _) =>
			{
				SoundEngine.PlaySound(SoundID.MenuTick);
				_source = _source == captured ? null : captured;
				HighlightSources();
				Refill();
			};
			_sourceList.Add(button);
		}

		HighlightSources();
	}

	private void BuildContent(UIElement panel)
	{
		var content = new UIElement();
		content.Left.Set(RailWidth + 12f, 0f);
		content.Width.Set(-RailWidth - 20f, 1f);
		content.Height.Set(-8f, 1f);
		content.Top.Set(4f, 0f);
		panel.Append(content);

		Asset<Texture2D> categories = Main.Assets.Request<Texture2D>("Images/UI/Achievement_Categories");
		for (int i = 0; i < 4; i++)
		{
			var toggle = new UIToggleImage(categories, 32, 32, new Point(34 * i, 0), new Point(34 * i, 34));
			toggle.Left.Set(i * 36f, 0f);
			toggle.SetState(true);
			toggle.OnLeftClick += (_, _) =>
			{
				SoundEngine.PlaySound(SoundID.MenuTick);
				Refill();
			};
			_categoryButtons.Add(toggle);
			content.Append(toggle);
		}

		_moddedButton = new UIToggleImage(UICommon.UIAchievementsMenuIconsTexture, 32, 32, new Point(0, 0), new Point(0, 34));
		_moddedButton.Left.Set(4 * 36f, 0f);
		_moddedButton.SetState(false);
		_moddedButton.OnLeftClick += (_, _) =>
		{
			SoundEngine.PlaySound(SoundID.MenuTick);
			Refill();
		};
		content.Append(_moddedButton);

		_filterButton = new UITextPanel<string>(FilterLabel(), 0.68f);
		_filterButton.HAlign = 1f;
		_filterButton.Left.Set(-206f, 0f);
		_filterButton.Width.Set(124f, 0f);
		_filterButton.Height.Set(32f, 0f);
		_filterButton.OnMouseOver += FadeIn;
		_filterButton.OnMouseOut += FadeOut;
		_filterButton.OnLeftClick += (_, _) =>
		{
			SoundEngine.PlaySound(SoundID.MenuTick);
			_completion = _completion switch
			{
				CompletionFilter.All => CompletionFilter.Done,
				CompletionFilter.Done => CompletionFilter.Todo,
				_ => CompletionFilter.All
			};
			_filterButton.SetText(FilterLabel());
			Refill();
		};
		content.Append(_filterButton);

		var searchPanel = _searchPanel = new UIPanel
		{
			Width = { Pixels = 200f },
			Height = { Pixels = 32f },
			HAlign = 1f,
			Top = { Pixels = 0f }
		};
		searchPanel.SetPadding(0f);
		searchPanel.OnRightClick += (_, _) =>
		{
			_search.SetContents("");
			_query = "";
			Refill();
		};
		content.Append(searchPanel);

		_search = new UISearchBar(Language.GetText("tModLoader.ModsTypeToSearch"), 0.8f);
		_search.Width.Set(-4f, 1f);
		_search.Height.Set(-4f, 1f);
		_search.Left.Set(4f, 0f);
		_search.VAlign = 0.5f;
		_search.OnContentsChanged += text =>
		{
			_query = text ?? "";
			Refill();
		};
		_search.OnCanceledTakingInput += () => _escapeGuard = 2;
		_search.OnEndTakingInput += () => _escapeGuard = 2;
		searchPanel.Append(_search);
		var clear = new UIImageButton(Main.Assets.Request<Texture2D>("Images/UI/SearchCancel"))
		{
			HAlign = 1f,
			VAlign = 0.5f,
			Left = new StyleDimension(-2f, 0f)
		};
		clear.OnLeftClick += (_, _) =>
		{
			_search.SetContents("");
			_query = "";
			Refill();
		};
		searchPanel.Append(clear);
		searchPanel.OnLeftClick += (_, _) =>
		{
			if (!clear.IsMouseHovering)
				_search.ToggleTakingText();
		};

		_progress = new ProgressHeader();
		_progress.Top.Set(38f, 0f);
		_progress.Width.Set(0f, 1f);
		_progress.Height.Set(18f, 0f);
		content.Append(_progress);

		_list = new UIList();
		_list.Top.Set(62f, 0f);
		_list.Width.Set(-28f, 1f);
		_list.Height.Set(-62f, 1f);
		_list.ListPadding = 5f;
		content.Append(_list);

		var scroll = new UIScrollbar();
		scroll.Top.Set(62f, 0f);
		scroll.Height.Set(-62f, 1f);
		scroll.HAlign = 1f;
		content.Append(scroll);
		_list.SetScrollbar(scroll);
	}

	private void Refill()
	{
		if (_list == null)
			return;

		string query = _query ?? "";
		var rows = new List<Achievement>();
		foreach (Achievement achievement in Main.Achievements.CreateAchievementsList())
		{
			if (!PassCategory(achievement) || !PassSource(achievement) || !PassCompletion(achievement) || !PassSearch(achievement, query))
				continue;

			rows.Add(achievement);
		}

		rows = rows.OrderBy(achievement => achievement.Id).ToList();

		_list.Clear();
		foreach (Achievement achievement in rows)
			_list.Add(new AchievementRow(achievement));

		_progress.Done = rows.Count(achievement => achievement.IsCompleted);
		_progress.Total = rows.Count;
		Recalculate();
	}

	private bool PassCategory(Achievement achievement)
	{
		if (_moddedButton.IsOn && achievement.ModAchievement == null)
			return false;

		int category = (int)achievement.Category;
		if (category < 0 || category >= _categoryButtons.Count)
			return true;

		return _categoryButtons[category].IsOn;
	}

	private bool PassSource(Achievement achievement)
	{
		return string.IsNullOrEmpty(_source) || AchievementSources.Of(achievement) == _source;
	}

	private bool PassCompletion(Achievement achievement)
	{
		return _completion switch
		{
			CompletionFilter.Done => achievement.IsCompleted,
			CompletionFilter.Todo => !achievement.IsCompleted,
			_ => true
		};
	}

	private bool PassSearch(Achievement achievement, string query)
	{
		if (string.IsNullOrWhiteSpace(query))
			return true;

		if (achievement.Hidden && !achievement.IsCompleted)
			return false;

		return LiveText.Name(achievement).Contains(query, StringComparison.CurrentCultureIgnoreCase)
			|| LiveText.Description(achievement).Contains(query, StringComparison.CurrentCultureIgnoreCase)
			|| AchievementSources.Of(achievement).Contains(query, StringComparison.CurrentCultureIgnoreCase);
	}

	private void HighlightSources()
	{
		foreach (UIElement element in _sourceList)
		{
			if (element is not UITextPanel<string> button)
				continue;

			bool selected = button.Text == _source;
			button.BackgroundColor = selected ? new Color(73, 94, 171) : new Color(63, 82, 151) * 0.7f;
		}
	}

	private void DrawCategoryTooltip(SpriteBatch spriteBatch)
	{
		string text = null;
		for (int i = 0; i < _categoryButtons.Count; i++)
		{
			if (!_categoryButtons[i].IsMouseHovering)
				continue;

			text = i switch
			{
				0 => Language.GetTextValue("Achievements.SlayerCategory"),
				1 => Language.GetTextValue("Achievements.CollectorCategory"),
				2 => Language.GetTextValue("Achievements.ExplorerCategory"),
				3 => Language.GetTextValue("Achievements.ChallengerCategory"),
				_ => null
			};
			break;
		}

		if (_moddedButton != null && _moddedButton.IsMouseHovering)
			text = Language.GetTextValue("tModLoader.AchievementsOnlyShowModded");

		if (string.IsNullOrEmpty(text))
			return;

		float width = FontAssets.MouseText.Value.MeasureString(text).X;
		Vector2 pos = new Vector2(Main.mouseX, Main.mouseY) + new Vector2(16f);
		if (pos.Y > Main.screenHeight - 30)
			pos.Y = Main.screenHeight - 30;
		if (pos.X > Main.screenWidth - width)
			pos.X = Main.screenWidth - width - 8f;

		Utils.DrawBorderStringFourWay(spriteBatch, FontAssets.MouseText.Value, text, pos.X, pos.Y, new Color(Main.mouseTextColor, Main.mouseTextColor, Main.mouseTextColor, Main.mouseTextColor), Color.Black, Vector2.Zero);
	}

	private void OpenConfirm(UIMouseEvent evt, UIElement listeningElement)
	{
		_blockInput = new UIImage(TextureAssets.MagicPixel)
		{
			Width = StyleDimension.Fill,
			Height = StyleDimension.Fill,
			Color = Color.Black * 0.5f,
			ScaleToFit = true
		};
		_blockInput.OnLeftClick += CloseConfirm;
		Append(_blockInput);

		_confirm = new UIPanel();
		_confirm.Width.Set(400f, 0f);
		_confirm.Height.Set(300f, 0f);
		_confirm.VAlign = 0.5f;
		_confirm.HAlign = 0.5f;
		Append(_confirm);

		var heading = new UITextPanel<LocalizedText>(Language.GetText("tModLoader.AchievementsResetConfirm"), 0.6f, true);
		heading.HAlign = 0.5f;
		heading.SetPadding(13f);
		heading.Top.Set(-33f, 0f);
		heading.BackgroundColor = new Color(73, 94, 171);
		_confirm.Append(heading);

		string wrapped = FontAssets.ItemStack.Value.CreateWrappedText(Language.GetTextValue("tModLoader.AchievementsResetConfirmTooltip"), 310f, Language.ActiveCulture.CultureInfo);
		var body = new UITextPanel<string>(wrapped);
		body.HAlign = 0.5f;
		body.Top.Set(20f, 0f);
		body.SetPadding(13f);
		body.Width.Set(-10f, 1f);
		body.Height.Set(-50f, 0.9f);
		_confirm.Append(body);

		var yes = new UITextPanel<LocalizedText>(Language.GetText("tModLoader.AchievementsReset"), 0.7f, true);
		yes.Width.Set(0f, 0.5f);
		yes.Height.Set(40f, 0f);
		yes.VAlign = 1f;
		yes.HAlign = 1f;
		yes.OnMouseOver += FadeIn;
		yes.OnMouseOut += FadeOut;
		yes.OnLeftClick += (_, _) =>
		{
			CloseConfirm(null, null);
			Main.Achievements.ClearAll();
			Main.menuMode = 0;
			IngameFancyUI.Close();
			SoundEngine.PlaySound(in SoundID.MenuClose);
		};
		_confirm.Append(yes);

		var no = new UITextPanel<LocalizedText>(Language.GetText("UI.Cancel"), 0.7f, true);
		no.Width.Set(-10f, 0.5f);
		no.Height.Set(40f, 0f);
		no.VAlign = 1f;
		no.OnMouseOver += FadeIn;
		no.OnMouseOut += FadeOut;
		no.OnLeftClick += CloseConfirm;
		_confirm.Append(no);
		SoundEngine.PlaySound(in SoundID.MenuOpen);
	}

	private void CloseConfirm(UIMouseEvent evt, UIElement listeningElement)
	{
		if (_blockInput != null)
			RemoveChild(_blockInput);
		if (_confirm != null)
			RemoveChild(_confirm);

		_blockInput = null;
		_confirm = null;
		SoundEngine.PlaySound(in SoundID.MenuClose);
	}

	private static void FadeIn(UIMouseEvent evt, UIElement listeningElement)
	{
		SoundEngine.PlaySound(SoundID.MenuTick);
		if (evt.Target is UIPanel panel)
		{
			panel.BackgroundColor = new Color(73, 94, 171);
			panel.BorderColor = Colors.FancyUIFatButtonMouseOver;
		}
	}

	private static void FadeOut(UIMouseEvent evt, UIElement listeningElement)
	{
		if (evt.Target is UIPanel panel)
		{
			panel.BackgroundColor = new Color(63, 82, 151) * 0.8f;
			panel.BorderColor = Color.Black;
		}
	}

	private string FilterLabel()
	{
		return _completion switch
		{
			CompletionFilter.Done => T("UI.FilterDone"),
			CompletionFilter.Todo => T("UI.FilterTodo"),
			_ => T("UI.FilterAll")
		};
	}

	private static string T(string suffix)
	{
		return Language.GetTextValue("Mods.ReimaginingAchievements." + suffix);
	}

	private sealed class ProgressHeader : UIElement
	{
		public int Done;
		public int Total;

		protected override void DrawSelf(SpriteBatch spriteBatch)
		{
			CalculatedStyle dimensions = GetDimensions();
			string text = Language.GetTextValue("Mods.ReimaginingAchievements.UI.Progress", Done, Total);
			var scale = new Vector2(0.85f);
			ChatManager.DrawColorCodedStringWithShadow(spriteBatch, FontAssets.ItemStack.Value, text, dimensions.Position(), Color.Gold, 0f, Vector2.Zero, scale);
			float labelWidth = ChatManager.GetStringSize(FontAssets.ItemStack.Value, text, scale).X + 10f;
			var bar = new Rectangle((int)(dimensions.X + labelWidth), (int)dimensions.Y + 3, (int)Math.Max(40f, dimensions.Width - labelWidth), 12);
			Texture2D pixel = TextureAssets.MagicPixel.Value;
			spriteBatch.Draw(pixel, bar, new Color(13, 20, 44));
			float fill = Total <= 0 ? 0f : Done / (float)Total;
			spriteBatch.Draw(pixel, new Rectangle(bar.X, bar.Y, (int)(bar.Width * fill), bar.Height), new Color(255, 214, 70));
		}
	}
}
