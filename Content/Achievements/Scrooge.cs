using System.Collections.Generic;
using ReimaginingAchievements.Common;
using Terraria.Achievements;
using Terraria.GameContent.Achievements;
using Terraria.Localization;
using Terraria.ModLoader;

namespace ReimaginingAchievements.Content.Achievements;

public class Scrooge : ModAchievement, IRequirementList
{
	public const int GoalCopper = 500000;

	public CustomIntCondition Counter { get; private set; }

	public override void SetStaticDefaults()
	{
		Achievement.SetCategory(AchievementCategory.Challenger);
		Counter = AddIntCondition(GoalCopper);
	}

	public override Position GetDefaultPosition() => new After("CHAMPION_OF_TERRARIA");

	public void AddCopper(int copper)
	{
		if (Counter == null || copper <= 0 || Counter.IsCompleted)
			return;

		Counter.Value += copper;
	}

	public IEnumerable<RequirementLine> GetRequirements()
	{
		int gold = Counter == null ? 0 : Counter.Value / 10000;
		string text = Language.GetTextValue("Mods.ReimaginingAchievements.Status.Tax", gold, 50);
		yield return new RequirementLine(text, Achievement.IsCompleted);
	}
}
