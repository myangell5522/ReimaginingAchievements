using System;
using System.Collections.Generic;
using ReimaginingAchievements.Common;
using ReimaginingAchievements.Content.Tracking;
using Terraria;
using Terraria.Achievements;
using Terraria.GameContent.Achievements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace ReimaginingAchievements.Content.Achievements;

public abstract class BatchCounter : OwnedAchievement, IRequirementList
{
	protected abstract string StatusKey { get; }

	protected abstract int Current { get; }

	protected abstract int Goal { get; }

	public IEnumerable<RequirementLine> GetRequirements()
	{
		string text = Language.GetTextValue("Mods.ReimaginingAchievements.Status." + StatusKey, Current, Goal);
		yield return new RequirementLine(text, Achievement.IsCompleted);
	}

	protected static int Read(Func<BatchPlayer, int> read)
	{
		if (Main.gameMenu || !Main.LocalPlayer.active)
			return 0;

		return read(Main.LocalPlayer.GetModPlayer<BatchPlayer>());
	}
}

public class HighwayToHell : OwnedAchievement
{
}

public class LookAtMe : OwnedAchievement
{
}

public class RideOfALifetime : BatchCounter
{
	protected override string StatusKey => "Ride";

	protected override int Goal => BatchPlayer.RequiredBiomes();

	protected override int Current => Read(player => player.RideSeen);
}

public class SixFeetAboveground : OwnedAchievement
{
}

public class Been50Years : OwnedAchievement
{
}

public class NoFishyBusiness : OwnedAchievement
{
}

public class SelfDefenseForDummies : OwnedAchievement
{
}

public class BorrowedArsenal : OwnedAchievement
{
}

public class SheWouldNever : OwnedAchievement
{
}

public class OhhExtraShiny : ModAchievement
{
	public CustomFlagCondition Leaf { get; private set; }

	public override void SetStaticDefaults()
	{
		Achievement.SetCategory(AchievementCategory.Challenger);
		AddItemPickupCondition(ItemID.LargeAmber);
		AddItemPickupCondition(ItemID.LargeAmethyst);
		AddItemPickupCondition(ItemID.LargeDiamond);
		AddItemPickupCondition(ItemID.LargeEmerald);
		AddItemPickupCondition(ItemID.LargeRuby);
		AddItemPickupCondition(ItemID.LargeSapphire);
		AddItemPickupCondition(ItemID.LargeTopaz);
		AddItemPickupCondition(ItemID.Magiluminescence);
		Leaf = AddCondition("Leaf");
	}

	public override Position GetDefaultPosition() => new After("CHAMPION_OF_TERRARIA");

	public void GrantLeaf()
	{
		if (Leaf != null && !Leaf.IsCompleted)
			Leaf.Complete();
	}
}

public class Relocation : OwnedAchievement
{
}

public class CartoonVillain : OwnedAchievement
{
}

public class FirefistOffice : OwnedAchievement
{
}

public class Unflappable : BatchCounter
{
	protected override string StatusKey => "Buffs";

	protected override int Goal => 12;

	protected override int Current => Read(player => player.StillBuffs);
}

public class ReRevitalized : OwnedAchievement
{
}

public class FitsOnTheWall : ModAchievement
{
	private static readonly int[] Items =
	{
		ItemID.KingSlimeTrophy, ItemID.KingSlimeMasterTrophy,
		ItemID.EyeofCthulhuTrophy, ItemID.EyeofCthulhuMasterTrophy,
		ItemID.EaterofWorldsTrophy, ItemID.EaterofWorldsMasterTrophy,
		ItemID.BrainofCthulhuTrophy, ItemID.BrainofCthulhuMasterTrophy,
		ItemID.QueenBeeTrophy, ItemID.QueenBeeMasterTrophy,
		ItemID.SkeletronTrophy, ItemID.SkeletronMasterTrophy,
		ItemID.DeerclopsTrophy, ItemID.DeerclopsMasterTrophy,
		ItemID.WallofFleshTrophy, ItemID.WallofFleshMasterTrophy,
		ItemID.QueenSlimeTrophy, ItemID.QueenSlimeMasterTrophy,
		ItemID.DestroyerTrophy, ItemID.DestroyerMasterTrophy,
		ItemID.RetinazerTrophy, ItemID.SpazmatismTrophy, ItemID.TwinsMasterTrophy,
		ItemID.SkeletronPrimeTrophy, ItemID.SkeletronPrimeMasterTrophy,
		ItemID.PlanteraTrophy, ItemID.PlanteraMasterTrophy,
		ItemID.GolemTrophy, ItemID.GolemMasterTrophy,
		ItemID.DukeFishronTrophy, ItemID.DukeFishronMasterTrophy,
		ItemID.FairyQueenTrophy, ItemID.FairyQueenMasterTrophy,
		ItemID.AncientCultistTrophy, ItemID.LunaticCultistMasterTrophy,
		ItemID.MoonLordTrophy, ItemID.MoonLordMasterTrophy,
		ItemID.BossTrophyBetsy, ItemID.BetsyMasterTrophy
	};

	public override void SetStaticDefaults()
	{
		Achievement.SetCategory(AchievementCategory.Challenger);
		for (int i = 0; i < Items.Length; i++)
			AddItemPickupCondition(Items[i]);
	}

	public override Position GetDefaultPosition() => new After("CHAMPION_OF_TERRARIA");
}

public class RgbNoHitter : OwnedAchievement
{
}

public class VeryBadTime : OwnedAchievement
{
}

public class Bowling : OwnedAchievement
{
}

public class WrongKindOfEvil : BatchCounter
{
	protected override string StatusKey => "Evil";

	protected override int Goal => 200;

	protected override int Current => Read(player => player.EvilTiles);
}

public class LovelyMelody : BatchCounter
{
	protected override string StatusKey => "Melody";

	protected override int Goal => 300;

	protected override int Current => Read(player => player.MelodySeconds);
}

public class CantStarve : OwnedAchievement
{
}

public class Scrooged : OwnedAchievement
{
}

public class ItTakesAVillage : OwnedAchievement
{
}
