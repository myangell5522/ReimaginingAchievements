using System;
using System.Collections.Generic;
using ReimaginingAchievements.Common;
using ReimaginingAchievements.Content.Tracking;
using Terraria.Achievements;
using Terraria.GameContent.Achievements;
using Terraria.Localization;
using Terraria.ModLoader;

namespace ReimaginingAchievements.Content.Achievements;

public abstract class OwnedAchievement : ModAchievement
{
	public CustomFlagCondition Flag { get; private set; }

	public override void SetStaticDefaults()
	{
		Achievement.SetCategory(AchievementCategory.Challenger);
		Flag = AddCondition();
	}

	public override Position GetDefaultPosition() => new After("CHAMPION_OF_TERRARIA");

	public void Grant()
	{
		if (Flag != null && !Flag.IsCompleted)
			Flag.Complete();
	}
}

public abstract class CounterAchievement : OwnedAchievement, IRequirementList
{
	protected abstract string StatusKey { get; }

	protected abstract int Current { get; }

	protected abstract int Goal { get; }

	public IEnumerable<RequirementLine> GetRequirements()
	{
		string text = Language.GetTextValue("Mods.ReimaginingAchievements.Status." + StatusKey, Current, Goal);
		yield return new RequirementLine(text, Achievement.IsCompleted);
	}

	protected static int LocalCount(Func<JokePlayer, int> read)
	{
		if (MainMenu())
			return 0;

		return read(Terraria.Main.LocalPlayer.GetModPlayer<JokePlayer>());
	}

	private static bool MainMenu()
	{
		return Terraria.Main.gameMenu || !Terraria.Main.LocalPlayer.active;
	}
}
