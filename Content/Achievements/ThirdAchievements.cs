using System.Collections.Generic;
using ReimaginingAchievements.Common;
using ReimaginingAchievements.Content.Tracking;
using Terraria.Achievements;
using Terraria.GameContent.Achievements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace ReimaginingAchievements.Content.Achievements;

public abstract class AccountCounter : ModAchievement, IRequirementList
{
	public CustomIntCondition Counter { get; private set; }

	protected abstract int Goal { get; }

	protected abstract string StatusKey { get; }

	protected virtual int Divisor => 1;

	public override void SetStaticDefaults()
	{
		Achievement.SetCategory(AchievementCategory.Challenger);
		Counter = AddIntCondition(Goal);
	}

	public override Position GetDefaultPosition() => new After("CHAMPION_OF_TERRARIA");

	public void Add(int amount)
	{
		if (Counter == null || amount <= 0 || Counter.IsCompleted)
			return;

		Counter.Value = (int)System.Math.Min((long)Counter.Value + amount, Goal);
	}

	public IEnumerable<RequirementLine> GetRequirements()
	{
		int value = Counter == null ? 0 : Counter.Value;
		string text = Language.GetTextValue("Mods.ReimaginingAchievements.Status." + StatusKey, value / Divisor, Goal / Divisor);
		yield return new RequirementLine(text, Achievement.IsCompleted);
	}
}

public abstract class NamedConditions : ModAchievement
{
	private readonly Dictionary<string, CustomFlagCondition> _conditions = new Dictionary<string, CustomFlagCondition>();

	protected abstract IEnumerable<string> ConditionNames { get; }

	public override void SetStaticDefaults()
	{
		Achievement.SetCategory(AchievementCategory.Challenger);
		foreach (string name in ConditionNames)
			_conditions[name] = AddCondition(name);
	}

	public override Position GetDefaultPosition() => new After("CHAMPION_OF_TERRARIA");

	public void Complete(string name)
	{
		if (_conditions.TryGetValue(name, out CustomFlagCondition condition) && !condition.IsCompleted)
			condition.Complete();
	}
}

public class PureInsanity : OwnedAchievement
{
}

public class TisButAScratch : OwnedAchievement
{
}

public class BiggerFish : OwnedAchievement
{
}

public class ImaFiringMahLazer : OwnedAchievement
{
}

public class SlimeSlayer : OwnedAchievement
{
}

public class LikeANinja : OwnedAchievement
{
}

public class GetMeOuttaHere : OwnedAchievement
{
}

public class GoodRiddance : OwnedAchievement
{
}

public class MassacreByCat : OwnedAchievement
{
}

public class MichaelBay : OwnedAchievement
{
}

public class Kaboom : OwnedAchievement
{
}

public class SteelFashioned : OwnedAchievement
{
}

public class LotOfRainbows : OwnedAchievement
{
}

public class PrettySolidName : OwnedAchievement
{
}

public class OutOfThinAir : OwnedAchievement
{
}

public class NoisyBunch : OwnedAchievement
{
}

public class UnderControl : OwnedAchievement
{
}

public class AppleADay : OwnedAchievement
{
}

public class Traitor : OwnedAchievement
{
}

public class Meanie : OwnedAchievement
{
}

public class OneUp : OwnedAchievement
{
}

public class ComboBreaker : OwnedAchievement
{
}

public class YareYareDaze : OwnedAchievement
{
}

public class FinestArt : OwnedAchievement
{
}

public class StopIt : OwnedAchievement
{
}

public class ThatsEmbarrassing : OwnedAchievement
{
}

public class TemporaryComa : OwnedAchievement
{
}

public class LovesInTheAir : OwnedAchievement
{
}

public class NahIdWin : OwnedAchievement
{
}

public class JungleJam : OwnedAchievement
{
}

public class NyaNyaNya : OwnedAchievement
{
}

public class Impostor : OwnedAchievement
{
}

public class Broke : AccountCounter
{
	protected override int Goal => 2000000;

	protected override string StatusKey => "Reforge";

	protected override int Divisor => 10000;
}

public class BrokeAgain : AccountCounter
{
	protected override int Goal => 10000000;

	protected override string StatusKey => "Reforge";

	protected override int Divisor => 10000;
}

public class CantTouchThis : OwnedAchievement
{
}

public class OhNoAnyway : OwnedAchievement
{
}

public class Stealth : OwnedAchievement
{
}

public class OverlyDramatic : OwnedAchievement
{
}

public class NatureOfAThing : OwnedAchievement
{
}

public class FloridaMan : OwnedAchievement
{
}

public class Defecator : OwnedAchievement
{
}

public class ExtraCheese : OwnedAchievement
{
}

public class Bomberman : AccountCounter
{
	protected override int Goal => 100;

	protected override string StatusKey => "Bombs";
}

public class Pacifist : OwnedAchievement
{
}

public class LaManchaland : OwnedAchievement
{
}

public class TakingCredit : OwnedAchievement
{
}

public class WorldwideSlideshow : OwnedAchievement
{
}

public class IDidntTouchYou : OwnedAchievement
{
}

public class ThisWare : OwnedAchievement
{
}

public class SeedCollector : NamedConditions
{
	public static readonly string[] Seeds =
	{
		"SEED_drunk", "SEED_bees", "SEED_ftw", "SEED_anniversary",
		"SEED_dontstarve", "SEED_remix", "SEED_notraps", "SEED_zenith"
	};

	protected override IEnumerable<string> ConditionNames => Seeds;
}

public class AbsoluteChaos : OwnedAchievement
{
}

public class TrueStormtrooper : OwnedAchievement
{
}

public class GreatKhan : OwnedAchievement
{
}

public class MetalGearTemple : OwnedAchievement
{
}

public class NowStrike : OwnedAchievement
{
}

public class BreakingBad : AccountCounter
{
	protected override int Goal => 9000;

	protected override string StatusKey => "Shards";
}

public class PharaohsFate : OwnedAchievement
{
}

public class ExtinctionEvent : OwnedAchievement
{
}

public class AntiAnkh : OwnedAchievement
{
}

public class NoScope : OwnedAchievement
{
}

public class RedWedding : OwnedAchievement
{
}

public class JustWhy : OwnedAchievement
{
}

public class IfThatsWhatYouWant : OwnedAchievement
{
}

public class WaitThisIsntRight : OwnedAchievement
{
}

public class TrueChallenge : NamedConditions
{
	public static readonly int[] Bosses =
	{
		NPCID.KingSlime, NPCID.EyeofCthulhu, NPCID.EaterofWorldsHead, NPCID.BrainofCthulhu,
		NPCID.QueenBee, NPCID.SkeletronHead, NPCID.Deerclops, NPCID.WallofFlesh,
		NPCID.QueenSlimeBoss, NPCID.TheDestroyer, NPCID.Retinazer, NPCID.SkeletronPrime,
		NPCID.Plantera, NPCID.Golem, NPCID.DukeFishron, NPCID.HallowBoss,
		NPCID.CultistBoss, NPCID.MoonLordCore
	};

	protected override IEnumerable<string> ConditionNames
	{
		get
		{
			foreach (int boss in Bosses)
				yield return "BOSS_" + boss;
		}
	}
}

public class UseMoreGun : OwnedAchievement
{
}

public class WhyNameless : GatedAchievement
{
	public override string RequiredMod => "NoxusBoss";
}
