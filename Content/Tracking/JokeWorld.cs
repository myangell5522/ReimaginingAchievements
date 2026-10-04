using System.Collections.Generic;
using Microsoft.Xna.Framework;
using ReimaginingAchievements.Content.Achievements;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace ReimaginingAchievements.Content.Tracking;

public class JokeWorld : ModSystem
{
	public static bool SawPreHardmode;
	public static bool AltarBroken;
	public static bool CharitySpoiled;
	public static readonly HashSet<int> SeenBosses = new HashSet<int>();
	public static readonly HashSet<int> BossLoot = new HashSet<int>();
	public static readonly HashSet<int> LavaWatch = new HashSet<int>();

	private static readonly int[] PreHardmodeBosses =
	{
		NPCID.KingSlime,
		NPCID.EyeofCthulhu,
		NPCID.EaterofWorldsHead,
		NPCID.BrainofCthulhu,
		NPCID.QueenBee,
		NPCID.SkeletronHead,
		NPCID.Deerclops
	};

	private bool _loaded;

	public static bool IsPreHardmodeBoss(int type)
	{
		for (int i = 0; i < PreHardmodeBosses.Length; i++)
		{
			if (PreHardmodeBosses[i] == type)
				return true;
		}

		return false;
	}

	public override void SaveWorldData(TagCompound tag)
	{
		tag["sawPre"] = SawPreHardmode;
		tag["altar"] = AltarBroken;
		tag["charity"] = CharitySpoiled;
		tag["seen"] = new List<int>(SeenBosses);
	}

	public override void LoadWorldData(TagCompound tag)
	{
		_loaded = true;
		SawPreHardmode = tag.GetBool("sawPre");
		AltarBroken = tag.GetBool("altar");
		CharitySpoiled = tag.GetBool("charity");
		SeenBosses.Clear();
		foreach (int type in tag.GetList<int>("seen"))
			SeenBosses.Add(type);
	}

	public override void OnWorldLoad()
	{
		BossLoot.Clear();
		LavaWatch.Clear();
		if (!_loaded)
		{
			SawPreHardmode = !Main.hardMode;
			AltarBroken = false;
			CharitySpoiled = false;
			SeenBosses.Clear();
		}

		if (!Main.hardMode)
			SawPreHardmode = true;

		_loaded = false;
	}

	public override void OnWorldUnload()
	{
		_loaded = false;
		SeenBosses.Clear();
		BossLoot.Clear();
		LavaWatch.Clear();
	}

	public override void PostUpdateNPCs()
	{
		if (!CharitySpoiled && !Main.hardMode)
		{
			for (int i = 0; i < Main.maxNPCs; i++)
			{
				NPC npc = Main.npc[i];
				if (!npc.active || !npc.townNPC || npc.type == NPCID.Guide)
					continue;

				if ((uint)npc.type < (uint)NPCID.Sets.IsTownPet.Length && NPCID.Sets.IsTownPet[npc.type])
					continue;

				CharitySpoiled = true;
				break;
			}
		}

	}

	public override void PostUpdateEverything()
	{
		CheckLava();
	}

	public static void NoteBossSpawn(int type)
	{
		if (!IsPreHardmodeBoss(type) || !SawPreHardmode)
			return;

		if (!Main.hardMode)
		{
			SeenBosses.Add(type);
			return;
		}

		if (SeenBosses.Contains(type))
			return;

		SeenBosses.Add(type);
		Award.Grant<YouExist>();
	}

	public static void NoteBossLoot(int itemIndex)
	{
		BossLoot.Add(itemIndex);
	}

	public static void WatchLava(int itemIndex)
	{
		LavaWatch.Add(itemIndex);
	}

	private static void CheckLava()
	{
		if (LavaWatch.Count == 0)
			return;

		int[] watch = new int[LavaWatch.Count];
		LavaWatch.CopyTo(watch);
		LavaWatch.Clear();
		for (int i = 0; i < watch.Length; i++)
		{
			int index = watch[i];
			if (!BossLoot.Contains(index))
				continue;

			Item item = Main.item[index];
			if (item.active && item.type != ItemID.None)
				continue;

			BossLoot.Remove(index);
			Award.Grant<Noooo>();
		}
	}
}
