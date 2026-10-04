using ReimaginingAchievements.Content.Achievements;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ReimaginingAchievements.Content.Tracking;

public class JokeNPC : GlobalNPC
{
	public override bool InstancePerEntity => true;

	public bool EyePhase2;
	public bool CopperHit;
	public bool Spoiled;

	public override void OnSpawn(NPC npc, IEntitySource source)
	{
		EyePhase2 = false;
		CopperHit = false;
		Spoiled = false;
		JokeWorld.NoteBossSpawn(npc.type);
	}

	public override void PostAI(NPC npc)
	{
		if (npc.type == NPCID.EyeofCthulhu && npc.life > 0 && npc.ai[0] > 0f)
			EyePhase2 = true;
	}

	public override void OnHitNPC(NPC npc, NPC target, NPC.HitInfo hit)
	{
		if (!target.boss || target.lifeMax <= 10000)
			return;

		target.GetGlobalNPC<JokeNPC>().Spoiled = true;
	}

	public override void OnKill(NPC npc)
	{
		if (npc.type == NPCID.SantaClaus)
			ModContent.GetInstance<MrGrinch>().Grant();

		if (npc.type == NPCID.EyeofCthulhu && !EyePhase2)
			ModContent.GetInstance<LetMeFinish>().Grant();

		if (IsMech(npc.type) && !JokeWorld.AltarBroken && MechsDown(npc))
			ModContent.GetInstance<ItsHarder>().Grant();

		if (npc.type == NPCID.GolemHead && JokeGear.LocalIsBare())
			ModContent.GetInstance<ItsHardest>().Grant();

		if (npc.type == NPCID.WallofFlesh && !JokeWorld.CharitySpoiled)
			ModContent.GetInstance<AintACharity>().Grant();

		if (npc.boss && npc.lifeMax > 10000 && CopperHit && !Spoiled)
			ModContent.GetInstance<WeakAreMighty>().Grant();
	}

	public static void NoteItemHit(NPC target, bool copperShortsword)
	{
		if (!target.boss || target.lifeMax <= 10000)
			return;

		JokeNPC tracker = target.GetGlobalNPC<JokeNPC>();
		if (copperShortsword)
			tracker.CopperHit = true;
		else
			tracker.Spoiled = true;
	}

	private static bool IsMech(int type)
	{
		return type == NPCID.TheDestroyer || type == NPCID.Retinazer || type == NPCID.Spazmatism || type == NPCID.SkeletronPrime;
	}

	private static bool MechsDown(NPC npc)
	{
		bool destroyer = NPC.downedMechBoss1 || npc.type == NPCID.TheDestroyer;
		bool prime = NPC.downedMechBoss3 || npc.type == NPCID.SkeletronPrime;
		bool twins = NPC.downedMechBoss2;
		if (npc.type == NPCID.Retinazer)
			twins = !NPC.AnyNPCs(NPCID.Spazmatism);
		else if (npc.type == NPCID.Spazmatism)
			twins = !NPC.AnyNPCs(NPCID.Retinazer);

		return destroyer && prime && twins;
	}
}

public class JokePlayerHits : ModPlayer
{
	public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
	{
		if (Player.whoAmI != Main.myPlayer)
			return;

		JokeNPC.NoteItemHit(target, item.type == ItemID.CopperShortsword);
	}

	public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
	{
		if (Player.whoAmI != Main.myPlayer)
			return;

		JokeNPC.NoteItemHit(target, false);
	}
}

public static class JokeGear
{
	public static bool LocalIsBare()
	{
		Player player = Main.LocalPlayer;
		if (!player.active)
			return false;

		for (int slot = 3; slot <= 9; slot++)
		{
			if (!player.armor[slot].IsAir)
				return false;
		}

		for (int i = 0; i < player.buffTime.Length; i++)
		{
			if (player.buffTime[i] > 0)
				return false;
		}

		return true;
	}
}

public class JokeItem : GlobalItem
{
	private static readonly int[] RecallItems =
	{
		ItemID.MagicMirror,
		ItemID.IceMirror,
		ItemID.CellPhone,
		ItemID.Shellphone,
		ItemID.ShellphoneSpawn,
		ItemID.ShellphoneOcean,
		ItemID.ShellphoneHell,
		ItemID.RecallPotion
	};

	public override void OnSpawn(Item item, IEntitySource source)
	{
		JokeWorld.BossLoot.Remove(item.whoAmI);
		if (source is EntitySource_Parent parent && parent.Entity is NPC npc && npc.boss)
			JokeWorld.NoteBossLoot(item.whoAmI);
	}

	public override void PostUpdate(Item item)
	{
		if (JokeWorld.BossLoot.Contains(item.whoAmI) && item.lavaWet)
			JokeWorld.WatchLava(item.whoAmI);
	}

	public override bool OnPickup(Item item, Player player)
	{
		if (player.whoAmI == Main.myPlayer)
			player.GetModPlayer<JokePlayer>().AddCoins(CoinValue(item));

		return true;
	}

	public override bool? UseItem(Item item, Player player)
	{
		if (player.whoAmI != Main.myPlayer || !IsRecall(item.type))
			return null;

		player.GetModPlayer<JokePlayer>().ArmRecall();
		return null;
	}

	private static bool IsRecall(int type)
	{
		for (int i = 0; i < RecallItems.Length; i++)
		{
			if (RecallItems[i] == type)
				return true;
		}

		return false;
	}

	private static int CoinValue(Item item)
	{
		if (item.type == ItemID.CopperCoin)
			return item.stack;

		if (item.type == ItemID.SilverCoin)
			return item.stack * 100;

		if (item.type == ItemID.GoldCoin)
			return item.stack * 10000;

		if (item.type == ItemID.PlatinumCoin)
			return item.stack * 1000000;

		return 0;
	}
}

public class JokeProjectile : GlobalProjectile
{
	public override void OnSpawn(Projectile projectile, IEntitySource source)
	{
		if (projectile.owner != Main.myPlayer || source is not EntitySource_ItemUse_WithAmmo ammo)
			return;

		if (!ContentSamples.ItemsByType.TryGetValue(ammo.AmmoItemIdUsed, out Item ammoItem))
			return;

		if (ammoItem.ammo == AmmoID.Bullet)
			Main.LocalPlayer.GetModPlayer<JokePlayer>().RegisterBullet();
	}
}

public class JokeTile : GlobalTile
{
	public override void KillTile(int i, int j, int type, ref bool fail, ref bool effectOnly, ref bool noItem)
	{
		if (!fail && !effectOnly && type == TileID.DemonAltar)
			JokeWorld.AltarBroken = true;
	}
}
