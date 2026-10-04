using ReimaginingAchievements.Common;
using Terraria.Achievements;
using Terraria.GameContent.Achievements;
using Terraria.ModLoader;

namespace ReimaginingAchievements.Content.Achievements;

public abstract class GatedAchievement : ModAchievement, IAchievementSource, IModGated
{
	public CustomFlagCondition Flag { get; private set; }

	public abstract string RequiredMod { get; }

	public bool ModMissing => !ModLoader.HasMod(RequiredMod);

	public string RequiredDisplayName
	{
		get
		{
			if (ModLoader.TryGetMod(RequiredMod, out Mod mod))
				return mod.DisplayNameClean;

			return RequiredMod switch
			{
				"NoxusBoss" => "Calamity: Wrath of the Gods",
				"CalamityMod" => "Calamity Mod",
				"CalamityEntropy" => "Calamity Entropy",
				_ => RequiredMod
			};
		}
	}

	public string Source => RequiredDisplayName;

	public override void SetStaticDefaults()
	{
		Achievement.SetCategory(AchievementCategory.Challenger);
		Flag = AddCondition();
	}

	public override Position GetDefaultPosition() => new After("CHAMPION_OF_TERRARIA");

	public void Grant()
	{
		if (ModMissing)
			return;

		if (Flag != null && !Flag.IsCompleted)
			Flag.Complete();
	}
}
