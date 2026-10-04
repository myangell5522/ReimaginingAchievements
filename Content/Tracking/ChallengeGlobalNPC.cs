using System.Collections.Generic;
using ReimaginingAchievements.Content.Achievements;
using ReimaginingAchievements.UI;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ReimaginingAchievements.Content.Tracking;

public class ChallengeGlobalNPC : GlobalNPC
{
	private static readonly int[] GolemTrapProjectiles =
	{
		99, 1013, 98, 184, 185, 186, 188, 108, 1014, 1007, 654, 1002
	};

	public bool FlagCopperMoonLord = true;
	public bool FlagEoCPurity;
	public bool FlagEoCDesert;
	public bool FlagEoCJungle;
	public bool FlagEoCSnow;
	public bool FlagEoCGraveyard;
	public bool FlagEoCGlowingMushroom;
	public bool FlagEoCBeach;
	public bool FlagEoCCrimson;
	public bool FlagEoCCorruption;
	public bool FlagEoCHallow;
	public bool FlagEoCShuriken = true;
	public bool FlagLunaticOrange = true;
	public bool FlagPirateShip = true;
	public int GolemDamageTrap;
	public int SlimeMountCount;
	public bool FlagMechMelee = true;
	private int _golemMilestone;

	public override bool InstancePerEntity => true;

	public static ChallengeGlobalNPC Find(int type)
	{
		if (Main.gameMenu)
			return null;

		for (int i = 0; i < Main.maxNPCs; i++)
		{
			NPC npc = Main.npc[i];
			if (npc.active && npc.type == type)
				return npc.GetGlobalNPC<ChallengeGlobalNPC>();
		}

		return null;
	}

	public override void AI(NPC npc)
	{
		if (Main.dedServ)
			return;

		if (npc.type == NPCID.MoonLordCore && FlagCopperMoonLord)
		{
			for (int i = 0; i < Main.maxProjectiles; i++)
			{
				Projectile projectile = Main.projectile[i];
				if (projectile.active && projectile.WipableTurret)
					FlagCopperMoonLord = false;
			}

			foreach (Player player in Main.player)
			{
				if (player.active && (player.HeldItem.type != ItemID.CopperShortsword || player.numMinions > 0))
					FlagCopperMoonLord = false;
			}
		}

		if (npc.type == NPCID.EyeofCthulhu)
			UpdateEye(npc);

		if (npc.type == NPCID.CultistBoss && FlagLunaticOrange)
			FailOrangeGear();

		if (!ChallengePlayer.MechBossParts.Contains(npc.type) || !FlagMechMelee)
			return;

		foreach (Player player in Main.player)
		{
			if (!player.active)
				continue;

			for (int slot = 0; slot < 10; slot++)
			{
				Item armor = player.armor[slot];
				if (armor.type != ItemID.None && (armor.OriginalRarity < 0 || armor.OriginalRarity > 6))
					FlagMechMelee = false;
			}
		}
	}

	public override void OnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone)
	{
		if (Main.dedServ)
			return;

		if (npc.type == NPCID.PirateShipCannon && projectile.type != ProjectileID.CannonballFriendly)
			FailDutchman();

		if (npc.type != NPCID.Golem && npc.type != NPCID.GolemFistLeft && npc.type != NPCID.GolemFistRight && npc.type != NPCID.GolemHead)
			return;

		if (!Contains(GolemTrapProjectiles, projectile.type))
			return;

		foreach (NPC other in Main.npc)
		{
			if (!other.active || other.type != NPCID.Golem)
				continue;

			ChallengeGlobalNPC body = other.GetGlobalNPC<ChallengeGlobalNPC>();
			body.GolemDamageTrap += damageDone;
			if (body.GolemDamageTrap < 5000 && body.GolemDamageTrap >= body._golemMilestone + 1000)
			{
				body._golemMilestone = body.GolemDamageTrap / 1000 * 1000;
				if (body._golemMilestone == 1000 || body._golemMilestone == 2000 || body._golemMilestone == 4000)
					ProgressToast.Push(ModContent.GetInstance<AchievementGolemTraps>().Achievement, ChallengePlayer.Status("Traps", body.GolemDamageTrap, 5000));
			}
		}
	}

	public override void OnHitByItem(NPC npc, Player player, Item item, NPC.HitInfo hit, int damageDone)
	{
		if (npc.type == NPCID.PirateShipCannon)
			FailDutchman();
	}

	public override void OnSpawn(NPC npc, IEntitySource source)
	{
		if (npc.type != NPCID.CultistBoss)
			return;

		foreach (Player player in Main.player)
		{
			if (player.active && player.numMinions > 0)
				FlagLunaticOrange = false;
		}
	}

	public override void HitEffect(NPC npc, NPC.HitInfo hit)
	{
		if (npc.type == NPCID.MoonLordCore && hit.DamageType == DamageClass.Summon)
			FlagCopperMoonLord = false;
	}

	public override void OnHitNPC(NPC npc, NPC target, NPC.HitInfo hit)
	{
		if (npc.life - target.damage <= 0 && target.SpawnedFromStatue && npc.type == NPCID.QueenBee)
			Award.Grant<AchievementBeeStatue>();
	}

	public void HandleKill(NPC npc)
	{
		if (npc.type == NPCID.MoonLordCore && FlagCopperMoonLord)
			ModContent.GetInstance<AchievementCopperMoonLord>().CopperMoonLordCondition.Complete();

		if (npc.type == NPCID.EyeofCthulhu && FlagEoCShuriken)
			ModContent.GetInstance<AchievementEoCShuriken>().EocShurikenCondition.Complete();

		if (npc.type == NPCID.CultistBoss && FlagLunaticOrange)
			ModContent.GetInstance<AchievementLunaticOrange>().LunaticOrangeCondition.Complete();

		if (npc.type == NPCID.PirateShip && FlagPirateShip)
			ModContent.GetInstance<AchievementDutchman>().DutchmanCondition.Complete();

		if (npc.type == NPCID.Golem && GolemDamageTrap >= 5000)
			ModContent.GetInstance<AchievementGolemTraps>().GolemTrapsCondition.Complete();

		if (!ChallengePlayer.MechBossParts.Contains(npc.type) || !FlagMechMelee)
			return;

		if (npc.type == NPCID.PrimeCannon || npc.type == NPCID.PrimeLaser || npc.type == NPCID.PrimeSaw || npc.type == NPCID.PrimeVice)
			return;

		foreach (NPC other in Main.npc)
		{
			if (npc.whoAmI != other.whoAmI && other.active && (other.type == NPCID.Spazmatism || other.type == NPCID.Retinazer))
				return;
		}

		ModContent.GetInstance<AchievementCutDown>().CutDownCondition.Complete();
	}

	private void UpdateEye(NPC npc)
	{
		if (npc.HasValidTarget && npc.target >= 0 && npc.target < Main.maxPlayers)
		{
			Player target = Main.player[npc.target];
			if (target.ZoneOverworldHeight)
			{
				bool wasPurity = FlagEoCPurity;
				bool wasDesert = FlagEoCDesert;
				bool wasJungle = FlagEoCJungle;
				bool wasSnow = FlagEoCSnow;
				bool wasGraveyard = FlagEoCGraveyard;
				bool wasMushroom = FlagEoCGlowingMushroom;
				bool wasBeach = FlagEoCBeach;
				bool wasCrimson = FlagEoCCrimson;
				bool wasCorruption = FlagEoCCorruption;
				bool wasHallow = FlagEoCHallow;
				if (target.ZoneForest)
					FlagEoCPurity = true;
				if (target.ZoneDesert)
					FlagEoCDesert = true;
				if (target.ZoneJungle)
					FlagEoCJungle = true;
				if (target.ZoneSnow)
					FlagEoCSnow = true;
				if (target.ZoneGraveyard)
					FlagEoCGraveyard = true;
				if (target.ZoneGlowshroom)
					FlagEoCGlowingMushroom = true;
				if (target.ZoneBeach)
					FlagEoCBeach = true;
				if (target.ZoneCrimson)
					FlagEoCCrimson = true;
				if (target.ZoneCorrupt)
					FlagEoCCorruption = true;
				if (target.ZoneHallow)
					FlagEoCHallow = true;

				ToastEye(wasPurity, FlagEoCPurity, "Forest");
				ToastEye(wasDesert, FlagEoCDesert, "Desert");
				ToastEye(wasJungle, FlagEoCJungle, "Jungle");
				ToastEye(wasSnow, FlagEoCSnow, "Snow");
				ToastEye(wasGraveyard, FlagEoCGraveyard, "Graveyard");
				ToastEye(wasMushroom, FlagEoCGlowingMushroom, "GlowingMushroom");
				ToastEye(wasBeach, FlagEoCBeach, "Ocean");
				ToastEye(wasCrimson, FlagEoCCrimson, "Crimson");
				ToastEye(wasCorruption, FlagEoCCorruption, "Corruption");
				ToastEye(wasHallow, FlagEoCHallow, "Hallow");
			}

			if (FlagEoCPurity && FlagEoCDesert && FlagEoCJungle && FlagEoCSnow && FlagEoCGraveyard && FlagEoCGlowingMushroom && FlagEoCBeach && FlagEoCCrimson && FlagEoCCorruption && FlagEoCHallow)
				ModContent.GetInstance<AchievementEoCBiome>().EoCBiomeCondition.Complete();
		}

		if (!FlagEoCShuriken)
			return;

		foreach (Player player in Main.player)
		{
			if (!player.active)
				continue;

			for (int slot = 0; slot < 10; slot++)
			{
				Item armor = player.armor[slot];
				// Blue rarity or lower. Negative rarities are expert exclusives and fail the run.
				if (armor.type != ItemID.None && (armor.OriginalRarity < 0 || armor.OriginalRarity > 1))
					FlagEoCShuriken = false;
			}
		}
	}

	private static void ToastEye(bool before, bool after, string biome)
	{
		if (before || !after || ModContent.GetInstance<AchievementEoCBiome>().Achievement.IsCompleted)
			return;

		ProgressToast.Push(ModContent.GetInstance<AchievementEoCBiome>().Achievement, Terraria.Localization.Language.GetTextValue("Mods.ReimaginingAchievements.Biomes." + biome));
	}

	private void FailOrangeGear()
	{
		foreach (Player player in Main.player)
		{
			if (!player.active)
				continue;

			for (int slot = 0; slot < 10; slot++)
			{
				Item armor = player.armor[slot];
				if (armor.type != ItemID.None && (armor.OriginalRarity < 0 || armor.OriginalRarity > 3))
					FlagLunaticOrange = false;
			}
		}
	}

	private static void FailDutchman()
	{
		foreach (NPC npc in Main.npc)
		{
			if (npc.active && npc.type == NPCID.PirateShip)
				npc.GetGlobalNPC<ChallengeGlobalNPC>().FlagPirateShip = false;
		}
	}

	private static bool Contains(int[] values, int value)
	{
		for (int i = 0; i < values.Length; i++)
		{
			if (values[i] == value)
				return true;
		}

		return false;
	}
}
