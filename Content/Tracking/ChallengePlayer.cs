using System.Collections.Generic;
using Microsoft.Xna.Framework;
using ReimaginingAchievements.Content.Achievements;
using ReimaginingAchievements.UI;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ReimaginingAchievements.Content.Tracking;

public class ChallengePlayer : ModPlayer
{
	public static readonly List<int> ValidDebuffs = new List<int>
	{
		30, 20, 24, 70, 22, 80, 35, 23, 31, 32,
		197, 33, 36, 195, 196, 37, 38, 39, 69, 44,
		46, 47, 149, 156, 164, 163, 144, 148, 145, 94,
		21, 88, 68, 67, 25, 120, 86, 350, 194, 199,
		353
	};

	public static readonly List<int> MechBossParts = new List<int>
	{
		127, 128, 131, 129, 130, 134, 135, 136, 126, 125
	};

	public static readonly string[] BiomeKeys =
	{
		"Forest", "Space", "Snow", "Desert", "Crimson", "Corruption", "Jungle", "Dungeon",
		"Ocean", "GlowingMushroom", "Hallow", "Underworld", "Marble", "Granite", "Temple", "Aether"
	};

	public bool GoblinsFlag;
	public int SpaTripTimer;
	public int RedPotionTimer;
	public int[] BiomeTimers = new int[16];
	public int DebuffCount;
	public int DebuffMilestone;

	private int _spaMilestone;

	public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
	{
		if (Player.whoAmI != Main.myPlayer)
			return;

		if (target.type == NPCID.EyeofCthulhu && (proj.type != ProjectileID.Shuriken || Player.mount.Active))
			target.GetGlobalNPC<ChallengeGlobalNPC>().FlagEoCShuriken = false;

		if (Player.HeldItem.type == ItemID.SniperRifle && target.type == NPCID.BlueSlime && (damageDone >= 3000 || (damageDone >= 2000 && !Main.expertMode)))
			ModContent.GetInstance<AchievementOverkill>().OverkillCondition.Complete();

		if (!MechBossParts.Contains(target.type))
			return;

		foreach (NPC npc in Main.npc)
		{
			if (npc.active && MechBossParts.Contains(npc.type))
				npc.GetGlobalNPC<ChallengeGlobalNPC>().FlagMechMelee = false;
		}
	}

	public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
	{
		if (Player.whoAmI != Main.myPlayer)
			return;

		if (target.type == NPCID.EyeofCthulhu)
			target.GetGlobalNPC<ChallengeGlobalNPC>().FlagEoCShuriken = false;

		if (target.type == NPCID.SkeletronHand && item.type == ItemID.SlapHand)
			ModContent.GetInstance<AchievementHighFive>().HighFiveCondition.Complete();

		if (target.life - damageDone <= 0 && target.type == NPCID.Vampire && Player.mount.Type == MountID.Wolf)
			ModContent.GetInstance<AchievementTeamWolf>().TeamWolfCondition.Complete();
	}

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
	{
		if (GoblinsFlag && (target.type == NPCID.GoblinPeon || target.type == NPCID.GoblinThief || target.type == NPCID.GoblinWarrior || target.type == NPCID.GoblinSorcerer || target.type == NPCID.GoblinArcher || target.type == NPCID.GoblinSummoner))
			GoblinsFlag = false;
	}

	public override void OnEnterWorld()
	{
		GoblinsFlag = false;
		SpaTripTimer = 0;
		_spaMilestone = 0;
		DebuffMilestone = 0;
		for (int i = 0; i < BiomeTimers.Length; i++)
			BiomeTimers[i] = 0;
	}

	public override void PostUpdate()
	{
		if (Player.whoAmI != Main.myPlayer)
			return;

		int defense = Player.statDefense;
		int defenseGoal = Main.expertMode ? 200 : 190;
		if (defense >= defenseGoal)
			ModContent.GetInstance<AchievementLivingWall>().LivingWallCondition.Complete();

		// The description forbids mounts. 50 px/tick is the 250 mph line used by the original check.
		if (!Player.mount.Active && System.Math.Abs(Player.velocity.X) >= 50f)
			ModContent.GetInstance<AchievementSpeed>().SpeedCondition.Complete();

		if (Main.invasionType == 1 && Main.invasionSizeStart == Main.invasionSize)
			GoblinsFlag = true;

		if (GoblinsFlag && Main.invasionType == 0)
		{
			GoblinsFlag = false;
			ModContent.GetInstance<AchievementDwarfFortress>().DwarfFortressCondition.Complete();
		}

		if (Player.lavaWet && !Player.lavaImmune)
		{
			SpaTripTimer++;
			if (SpaTripTimer > 10800)
				ModContent.GetInstance<AchievementSpaTrip>().SpaTripCondition.Complete();
			else if (SpaTripTimer >= _spaMilestone + 3600)
			{
				_spaMilestone += 3600;
				ProgressToast.Push(ModContent.GetInstance<AchievementSpaTrip>().Achievement, Status("Lava", SpaTripTimer / 60, 180));
			}
		}
		else
		{
			SpaTripTimer = 0;
			_spaMilestone = 0;
		}

		int debuffs = 0;
		foreach (int debuff in ValidDebuffs)
		{
			if (Player.HasBuff(debuff))
				debuffs++;
		}

		DebuffCount = debuffs;
		if (debuffs >= 15 && RedPotionTimer <= 0)
			ModContent.GetInstance<AchievementDebuffs>().DebuffsCondition.Complete();
		else if (debuffs >= DebuffMilestone + 5 && debuffs < 15 && RedPotionTimer <= 0)
		{
			DebuffMilestone = debuffs / 5 * 5;
			ProgressToast.Push(ModContent.GetInstance<AchievementDebuffs>().Achievement, Status("Debuffs", debuffs, 15));
		}

		if (RedPotionTimer >= 0)
			RedPotionTimer--;

		UpdateBiomes();
	}

	public override void PreUpdate()
	{
		if (Player.whoAmI != Main.myPlayer)
			return;

		if (Player.mount.Type != MountID.Slime || Player.wetSlime != 0 || Player.velocity.Y <= 0f)
			return;

		Rectangle rect = Player.getRect();
		rect.Offset(0, Player.height - 1);
		rect.Height = 2;
		rect.Inflate(12, 6);
		foreach (NPC npc in Main.npc)
		{
			if (!npc.active || npc.dontTakeDamage || npc.friendly || npc.type != NPCID.QueenSlimeBoss)
				continue;

			if (!rect.Intersects(npc.getRect()))
				continue;

			if (!npc.noTileCollide && !Collision.CanHit(Player.position, Player.width, Player.height, npc.position, npc.width, npc.height))
				continue;

			ChallengeGlobalNPC global = npc.GetGlobalNPC<ChallengeGlobalNPC>();
			global.SlimeMountCount++;
			if (global.SlimeMountCount >= 50)
				ModContent.GetInstance<AchievementSlimeMount>().SlimeMountCondition.Complete();
			else if (global.SlimeMountCount == 10 || global.SlimeMountCount == 25 || global.SlimeMountCount == 40)
				ProgressToast.Push(ModContent.GetInstance<AchievementSlimeMount>().Achievement, Status("Bounces", global.SlimeMountCount, 50));
		}
	}

	private void UpdateBiomes()
	{
		bool[] here =
		{
			Player.ZoneForest,
			Player.ZoneSkyHeight,
			Player.ZoneSnow,
			Player.ZoneDesert,
			Player.ZoneCrimson,
			Player.ZoneCorrupt,
			Player.ZoneJungle,
			Player.ZoneDungeon,
			Player.ZoneBeach,
			Player.ZoneGlowshroom,
			Player.ZoneHallow,
			Player.ZoneUnderworldHeight,
			Player.ZoneMarble,
			Player.ZoneGranite,
			Player.ZoneLihzhardTemple,
			Player.ZoneShimmer
		};

		var biomes = ModContent.GetInstance<AchievementBiomes>();
		int fresh = -1;
		for (int i = 0; i < here.Length; i++)
		{
			if (!here[i])
				continue;

			if (BiomeTimers[i] == 0)
				fresh = i;

			BiomeTimers[i] = 901;
		}

		bool all = true;
		for (int i = 0; i < BiomeTimers.Length; i++)
		{
			if (BiomeTimers[i] > 0)
				BiomeTimers[i]--;
			else
				all = false;
		}

		if (fresh >= 0 && !all && !biomes.Achievement.IsCompleted)
			ProgressToast.Push(biomes.Achievement, Terraria.Localization.Language.GetTextValue("Mods.ReimaginingAchievements.Biomes." + BiomeKeys[fresh]));

		if (all)
			biomes.BiomesCondition.Complete();
	}

	public static string Status(string key, int current, int goal)
	{
		return Terraria.Localization.Language.GetTextValue("Mods.ReimaginingAchievements.Status." + key, current, goal);
	}
}
