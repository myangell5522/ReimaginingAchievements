using Terraria.Achievements;
using Terraria.ModLoader;

namespace ReimaginingAchievements.Common;

public abstract class PortedAchievement : ModAchievement, IAchievementSource
{
	public virtual string Source => AchievementSources.Verv;

	public sealed override void SetStaticDefaults()
	{
		Achievement.SetCategory(AchievementCategory.Challenger);
		RegisterConditions();
	}

	protected abstract void RegisterConditions();
}
