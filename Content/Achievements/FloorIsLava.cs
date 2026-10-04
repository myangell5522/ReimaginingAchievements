using System.Collections.Generic;
using ReimaginingAchievements.Common;
using ReimaginingAchievements.Content.Tracking;
using Terraria.Achievements;
using Terraria.GameContent.Achievements;
using Terraria.Localization;
using Terraria.ModLoader;

namespace ReimaginingAchievements.Content.Achievements;

public class FloorIsLava : ModAchievement, IRequirementList
{
	public CustomFlagCondition Condition { get; private set; }

	public override void SetStaticDefaults()
	{
		Achievement.SetCategory(AchievementCategory.Challenger);
		Condition = AddCondition();
	}

	public override Position GetDefaultPosition() => new After("CHAMPION_OF_TERRARIA");

	public IEnumerable<RequirementLine> GetRequirements()
	{
		yield return new RequirementLine(Description.Value, Achievement.IsCompleted);
		if (MainMenu())
			yield break;

		FloorIsLavaPlayer player = Terraria.Main.LocalPlayer.GetModPlayer<FloorIsLavaPlayer>();
		if (!player.Tracking)
			yield break;

		string key = player.Failed ? "Mods.ReimaginingAchievements.Status.FloorFailed" : "Mods.ReimaginingAchievements.Status.FloorAir";
		yield return new RequirementLine(Language.GetTextValue(key), !player.Failed);
	}

	private static bool MainMenu()
	{
		return Terraria.Main.gameMenu || !Terraria.Main.LocalPlayer.active;
	}
}
