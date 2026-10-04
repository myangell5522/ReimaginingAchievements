using ReimaginingAchievements.Common;
using Terraria.Achievements;
using Terraria.GameContent.Achievements;
using Terraria.ModLoader;

namespace ReimaginingAchievements.Content.Achievements;

public abstract class GatedAchievement : ModAchievement, IAchievementSource
{
	public CustomFlagCondition Flag { get; private set; }

	public abstract string RequiredMod { get; }

	public string Source => ModLoader.TryGetMod(RequiredMod, out Mod mod) ? mod.DisplayNameClean : RequiredMod;

	public override bool IsLoadingEnabled(Mod mod) => ModLoader.HasMod(RequiredMod);

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
