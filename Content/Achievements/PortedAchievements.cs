using System.Collections.Generic;
using ReimaginingAchievements.Common;
using ReimaginingAchievements.Content.Tracking;
using Terraria;
using Terraria.GameContent.Achievements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace ReimaginingAchievements.Content.Achievements;

public class AchievementBeeStatue : PortedAchievement
{
	public CustomFlagCondition BeeStatueCondition { get; private set; }

	protected override void RegisterConditions() => BeeStatueCondition = AddCondition();
}

public class AchievementBiomes : PortedAchievement, IRequirementList
{
	public CustomFlagCondition BiomesCondition { get; private set; }

	protected override void RegisterConditions() => BiomesCondition = AddCondition();

	public IEnumerable<RequirementLine> GetRequirements()
	{
		int[] timers = InWorld ? Main.LocalPlayer.GetModPlayer<ChallengePlayer>().BiomeTimers : null;
		for (int i = 0; i < ChallengePlayer.BiomeKeys.Length; i++)
		{
			bool done = Achievement.IsCompleted || (timers != null && timers[i] > 0);
			yield return new RequirementLine(Language.GetTextValue("Mods.ReimaginingAchievements.Biomes." + ChallengePlayer.BiomeKeys[i]), done);
		}
	}

	private static bool InWorld => !Main.gameMenu && Main.LocalPlayer.active;
}

public class AchievementCopperMoonLord : PortedAchievement
{
	public CustomFlagCondition CopperMoonLordCondition { get; private set; }

	protected override void RegisterConditions() => CopperMoonLordCondition = AddCondition();
}

public class AchievementCutDown : PortedAchievement
{
	public CustomFlagCondition CutDownCondition { get; private set; }

	protected override void RegisterConditions() => CutDownCondition = AddCondition();
}

public class AchievementDebuffs : PortedAchievement, IRequirementList
{
	public CustomFlagCondition DebuffsCondition { get; private set; }

	protected override void RegisterConditions() => DebuffsCondition = AddCondition();

	public IEnumerable<RequirementLine> GetRequirements()
	{
		int count = !Main.gameMenu && Main.LocalPlayer.active ? Main.LocalPlayer.GetModPlayer<ChallengePlayer>().DebuffCount : 0;
		yield return new RequirementLine(ChallengePlayer.Status("Debuffs", count, 15), Achievement.IsCompleted);
	}
}

public class AchievementDutchman : PortedAchievement
{
	public CustomFlagCondition DutchmanCondition { get; private set; }

	protected override void RegisterConditions() => DutchmanCondition = AddCondition();
}

public class AchievementDwarfFortress : PortedAchievement
{
	public CustomFlagCondition DwarfFortressCondition { get; private set; }

	protected override void RegisterConditions() => DwarfFortressCondition = AddCondition();
}

public class AchievementEoCBiome : PortedAchievement, IRequirementList
{
	public CustomFlagCondition EoCBiomeCondition { get; private set; }

	protected override void RegisterConditions() => EoCBiomeCondition = AddCondition();

	public IEnumerable<RequirementLine> GetRequirements()
	{
		ChallengeGlobalNPC eye = ChallengeGlobalNPC.Find(NPCID.EyeofCthulhu);
		bool done = Achievement.IsCompleted;
		yield return Line("Forest", done || (eye?.FlagEoCPurity ?? false));
		yield return Line("Desert", done || (eye?.FlagEoCDesert ?? false));
		yield return Line("Jungle", done || (eye?.FlagEoCJungle ?? false));
		yield return Line("Snow", done || (eye?.FlagEoCSnow ?? false));
		yield return Line("Graveyard", done || (eye?.FlagEoCGraveyard ?? false));
		yield return Line("GlowingMushroom", done || (eye?.FlagEoCGlowingMushroom ?? false));
		yield return Line("Ocean", done || (eye?.FlagEoCBeach ?? false));
		yield return Line("Crimson", done || (eye?.FlagEoCCrimson ?? false));
		yield return Line("Corruption", done || (eye?.FlagEoCCorruption ?? false));
		yield return Line("Hallow", done || (eye?.FlagEoCHallow ?? false));
	}

	private static RequirementLine Line(string biome, bool done)
	{
		return new RequirementLine(Language.GetTextValue("Mods.ReimaginingAchievements.Biomes." + biome), done);
	}
}

public class AchievementEoCShuriken : PortedAchievement
{
	public CustomFlagCondition EocShurikenCondition { get; private set; }

	protected override void RegisterConditions() => EocShurikenCondition = AddCondition();
}

public class AchievementGoldCritter : PortedAchievement
{
	protected override void RegisterConditions()
	{
		AddItemPickupCondition(2889);
		AddItemPickupCondition(2890);
		AddItemPickupCondition(2891);
		AddItemPickupCondition(4340);
		AddItemPickupCondition(2892);
		AddItemPickupCondition(4274);
		AddItemPickupCondition(2893);
		AddItemPickupCondition(4362);
		AddItemPickupCondition(2894);
		AddItemPickupCondition(4482);
		AddItemPickupCondition(3564);
		AddItemPickupCondition(4419);
		AddItemPickupCondition(2895);
	}
}

public class AchievementGolemTraps : PortedAchievement, IRequirementList
{
	public CustomFlagCondition GolemTrapsCondition { get; private set; }

	protected override void RegisterConditions() => GolemTrapsCondition = AddCondition();

	public IEnumerable<RequirementLine> GetRequirements()
	{
		int damage = ChallengeGlobalNPC.Find(NPCID.Golem)?.GolemDamageTrap ?? 0;
		yield return new RequirementLine(ChallengePlayer.Status("Traps", damage, 5000), Achievement.IsCompleted);
	}
}

public class AchievementHighFive : PortedAchievement
{
	public CustomFlagCondition HighFiveCondition { get; private set; }

	protected override void RegisterConditions() => HighFiveCondition = AddCondition();
}

public class AchievementLivingWall : PortedAchievement, IRequirementList
{
	public CustomFlagCondition LivingWallCondition { get; private set; }

	protected override void RegisterConditions() => LivingWallCondition = AddCondition();

	public IEnumerable<RequirementLine> GetRequirements()
	{
		int defense = !Main.gameMenu && Main.LocalPlayer.active ? Main.LocalPlayer.statDefense : 0;
		int goal = Main.expertMode ? 200 : 190;
		yield return new RequirementLine(ChallengePlayer.Status("Defense", defense, goal), Achievement.IsCompleted);
	}
}

public class AchievementLunaticOrange : PortedAchievement
{
	public CustomFlagCondition LunaticOrangeCondition { get; private set; }

	protected override void RegisterConditions() => LunaticOrangeCondition = AddCondition();
}

public class AchievementOverkill : PortedAchievement
{
	public CustomFlagCondition OverkillCondition { get; private set; }

	protected override void RegisterConditions() => OverkillCondition = AddCondition();
}

public class AchievementSlimeMount : PortedAchievement, IRequirementList
{
	public CustomFlagCondition SlimeMountCondition { get; private set; }

	protected override void RegisterConditions() => SlimeMountCondition = AddCondition();

	public IEnumerable<RequirementLine> GetRequirements()
	{
		int bounces = ChallengeGlobalNPC.Find(NPCID.QueenSlimeBoss)?.SlimeMountCount ?? 0;
		yield return new RequirementLine(ChallengePlayer.Status("Bounces", bounces, 50), Achievement.IsCompleted);
	}
}

public class AchievementSpaTrip : PortedAchievement, IRequirementList
{
	public CustomFlagCondition SpaTripCondition { get; private set; }

	protected override void RegisterConditions() => SpaTripCondition = AddCondition();

	public IEnumerable<RequirementLine> GetRequirements()
	{
		int seconds = !Main.gameMenu && Main.LocalPlayer.active ? Main.LocalPlayer.GetModPlayer<ChallengePlayer>().SpaTripTimer / 60 : 0;
		yield return new RequirementLine(ChallengePlayer.Status("Lava", seconds, 180), Achievement.IsCompleted);
	}
}

public class AchievementSpeed : PortedAchievement, IRequirementList
{
	public CustomFlagCondition SpeedCondition { get; private set; }

	protected override void RegisterConditions() => SpeedCondition = AddCondition();

	public IEnumerable<RequirementLine> GetRequirements()
	{
		int mph = 0;
		if (!Main.gameMenu && Main.LocalPlayer.active && !Main.LocalPlayer.mount.Active)
			mph = (int)(System.Math.Abs(Main.LocalPlayer.velocity.X) * 5f);

		yield return new RequirementLine(ChallengePlayer.Status("Speed", mph, 250), Achievement.IsCompleted);
	}
}

public class AchievementTeamWolf : PortedAchievement
{
	public CustomFlagCondition TeamWolfCondition { get; private set; }

	protected override void RegisterConditions() => TeamWolfCondition = AddCondition();
}

public class AchievementThisIsFine : PortedAchievement
{
	public CustomFlagCondition ThisIsFineCondition { get; private set; }

	protected override void RegisterConditions() => ThisIsFineCondition = AddCondition();
}
