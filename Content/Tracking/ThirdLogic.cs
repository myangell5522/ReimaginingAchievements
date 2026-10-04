using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using ReimaginingAchievements.Content.Achievements;
using Terraria;
using Terraria.GameContent.Events;
using Terraria.ID;
using Terraria.ModLoader;

namespace ReimaginingAchievements.Content.Tracking;

public struct Strike
{
	public const int ByItem = 0;
	public const int ByProjectile = 1;
	public const int ByMinion = 2;
	public const int BySentry = 3;
	public const int ByPassive = 4;

	public int Kind;
	public int Item;
	public int Prefix;
	public int Ammo;
	public int Proj;
}

public static class ThirdLogic
{
	private const int Prism = 1 << 0;
	private const int Rainbow = 1 << 1;
	private const int Guardian = 1 << 2;
	private const int Art = 1 << 3;
	private const int Broken = 1 << 4;
	private const int Summons = 1 << 5;
	private const int Passive = 1 << 6;
	private const int Ware = 1 << 7;
	private const int Scope = 1 << 8;
	private const int Copper = 1 << 9;

	public static readonly HashSet<int> Throwables = new HashSet<int>
	{
		ItemID.Grenade, ItemID.StickyGrenade, ItemID.BouncyGrenade, ItemID.Beenade, ItemID.PartyGirlGrenade,
		ItemID.Bomb, ItemID.StickyBomb, ItemID.BouncyBomb, ItemID.ScarabBomb, ItemID.Dynamite,
		ItemID.StickyDynamite, ItemID.BouncyDynamite, ItemID.BombFish, ItemID.MolotovCocktail
	};
	public static readonly HashSet<int> Bombs = new HashSet<int>
	{
		ItemID.Bomb, ItemID.StickyBomb, ItemID.BouncyBomb, ItemID.ScarabBomb, ItemID.Dynamite,
		ItemID.StickyDynamite, ItemID.BouncyDynamite, ItemID.BombFish, ItemID.DirtBomb, ItemID.DirtStickyBomb,
		ItemID.WetBomb, ItemID.LavaBomb, ItemID.HoneyBomb, ItemID.DryBomb
	};
	public static readonly HashSet<int> Lances = new HashSet<int>
	{
		ItemID.JoustingLance, ItemID.ShadowJoustingLance, ItemID.HallowJoustingLance
	};
	public static readonly Dictionary<int, HashSet<int>> WareItems = new Dictionary<int, HashSet<int>>();

	private static readonly HashSet<int> GuardianFamilies = new HashSet<int>
	{
		BatchLogic.Golem, BatchLogic.Duke, BatchLogic.Empress, BatchLogic.Cultist, BatchLogic.Moon,
		BatchLogic.Betsy, BatchLogic.Pumpking, BatchLogic.IceQueen, BatchLogic.Saucer
	};
	private static readonly HashSet<int> MiniBosses = new HashSet<int>
	{
		NPCID.IceGolem, NPCID.SandElemental, NPCID.BigMimicCorruption, NPCID.BigMimicCrimson,
		NPCID.BigMimicHallow, NPCID.BigMimicJungle, NPCID.Mothron, NPCID.GoblinSummoner,
		NPCID.PirateCaptain, NPCID.DD2DarkMageT1, NPCID.DD2DarkMageT3
	};

	private static readonly Dictionary<int, Fight> Ledger = new Dictionary<int, Fight>();
	private static readonly Group SlimeGroup = new Group(BatchLogic.King, BatchLogic.QueenSlime);
	private static readonly Group JungleGroup = new Group(BatchLogic.Bee, BatchLogic.Plantera, BatchLogic.Golem);
	private static readonly Group CrowdGroup = new Group();
	private static readonly HashSet<long> DeathBosses = new HashSet<long>();

	private static double _prevAbs = -1;
	private static bool _prevDay;
	private static uint _explosiveTick;
	private static int _explosiveCount;

	private static bool _invasion;
	private static bool _invasionDD2;
	private static int _invasionHits;
	private static bool _invasionSpoiled;
	private static bool _invasionBoss;

	public static double Delta { get; private set; }
	public static bool Dawn { get; private set; }
	public static bool MancheRunning;
	public static int MancheHits;
	public static bool MancheSpoiled;
	public static int StormSpent;
	public static int AngerTimer;
	public static int BayTimer;
	public static uint LoveThrown;
	public static bool NamelessPotion;

	public static void Reset()
	{
		Ledger.Clear();
		SlimeGroup.Disarm();
		JungleGroup.Disarm();
		CrowdGroup.Disarm();
		DeathBosses.Clear();
		_prevAbs = -1;
		_prevDay = Main.dayTime;
		_explosiveCount = 0;
		_invasion = false;
		MancheRunning = false;
		StormSpent = 0;
		AngerTimer = 0;
		BayTimer = 0;
		LoveThrown = 0;
		NamelessPotion = false;
	}

	public static void UpdateClock()
	{
		double abs = Main.dayTime ? Main.time : Main.dayLength + Main.time;
		double delta = _prevAbs < 0 ? 0 : abs - _prevAbs;
		if (delta < 0)
			delta += Main.dayLength + Main.nightLength;

		if (delta > 3600)
			delta = 0;

		Delta = delta;
		_prevAbs = abs;
		Dawn = Main.dayTime && !_prevDay;
		_prevDay = Main.dayTime;
	}

	public static int Key(NPC npc)
	{
		int family = BatchLogic.Family(npc.type);
		if (family != 0)
			return family;

		if (npc.boss)
			return 1000 + npc.type;

		return 100000 + npc.whoAmI;
	}

	public static bool IsBossLike(NPC npc)
	{
		if (npc.type == NPCID.DungeonGuardian)
			return false;

		int family = BatchLogic.Family(npc.type);
		if (family == BatchLogic.Eater)
			return true;

		return npc.boss || family != 0 && BatchLogic.IsPrimary(npc.type);
	}

	public static bool IsFinal(NPC npc)
	{
		int family = BatchLogic.Family(npc.type);
		if (family == 0)
			return true;

		if (family != BatchLogic.Eater && !BatchLogic.IsPrimary(npc.type))
			return false;

		for (int i = 0; i < Main.maxNPCs; i++)
		{
			NPC other = Main.npc[i];
			if (!other.active || other.whoAmI == npc.whoAmI || BatchLogic.Family(other.type) != family)
				continue;

			if (family == BatchLogic.Eater || BatchLogic.IsPrimary(other.type))
				return false;
		}

		return true;
	}

	public static bool AnyBoss()
	{
		for (int i = 0; i < Main.maxNPCs; i++)
		{
			if (Main.npc[i].active && IsBossLike(Main.npc[i]))
				return true;
		}

		return false;
	}

	public static Strike FromProjectile(Projectile projectile)
	{
		ThirdProjectile tracked = projectile.GetGlobalProjectile<ThirdProjectile>();
		int kind = Strike.ByProjectile;
		if (projectile.sentry || ProjectileID.Sets.SentryShot[projectile.type])
			kind = Strike.BySentry;
		else if (projectile.minion || ProjectileID.Sets.MinionShot[projectile.type])
			kind = Strike.ByMinion;

		return new Strike
		{
			Kind = kind,
			Item = tracked.Item,
			Prefix = tracked.Prefix,
			Ammo = tracked.Ammo,
			Proj = projectile.type
		};
	}

	public static void NoteHit(NPC target, Strike strike, int damage)
	{
		if (!BatchLogic.Client() || damage <= 0 || target.friendly || target.townNPC)
			return;

		int family = BatchLogic.Family(target.type);
		int mask = 0;
		if (strike.Item == ItemID.LastPrism)
			mask |= Prism;
		if (strike.Item == ItemID.RainbowGun || strike.Item == ItemID.RainbowRod)
			mask |= Rainbow;
		if (strike.Proj == ProjectileID.StardustGuardian || strike.Proj == ProjectileID.StardustGuardianExplosion)
			mask |= Guardian;
		if (Throwables.Contains(strike.Item))
			mask |= Art;
		if (strike.Prefix == PrefixID.Broken)
			mask |= Broken;
		if (strike.Kind == Strike.ByMinion || strike.Kind == Strike.BySentry)
			mask |= Summons;
		if (strike.Kind == Strike.ByPassive)
			mask |= Passive;
		if (WareItems.TryGetValue(family, out HashSet<int> ware) && ware.Contains(strike.Item))
			mask |= Ware;
		if (strike.Item == ItemID.SniperRifle && strike.Ammo == ItemID.MusketBall)
			mask |= Scope;
		if (strike.Item == ItemID.CopperShortsword)
			mask |= Copper;

		Fight fight = FightOf(Key(target));
		fight.Hits++;
		fight.Valid &= mask;

		if (_invasion)
		{
			if (strike.Kind == Strike.BySentry)
				_invasionHits++;
			else
				_invasionSpoiled = true;
		}

		if (MancheRunning)
		{
			if (Lances.Contains(strike.Item))
				MancheHits++;
			else
				MancheSpoiled = true;
		}

		if (strike.Item == ItemID.LaserMachinegun)
			StormSpent = 0;

		if (strike.Item == ItemID.ThunderStaff && AngerTimer > 0)
			ModContent.GetInstance<NowStrike>().Grant();
	}

	public static void OnLocalKill(NPC npc)
	{
		if (!IsFinal(npc))
			return;

		int key = Key(npc);
		Ledger.TryGetValue(key, out Fight fight);
		Ledger.Remove(key);

		bool boss = IsBossLike(npc);
		if (boss)
			GroupKill(key);

		int family = BatchLogic.Family(npc.type);
		Player player = Main.LocalPlayer;

		if (family == BatchLogic.Moon && Valid(fight, Prism))
			ModContent.GetInstance<ImaFiringMahLazer>().Grant();

		if (family == BatchLogic.Moon && Valid(fight, Art))
			ModContent.GetInstance<FinestArt>().Grant();

		if (family == BatchLogic.Moon && Main.zenithWorld)
			ModContent.GetInstance<AbsoluteChaos>().Grant();

		if (npc.type == NPCID.RainbowSlime && Valid(fight, Rainbow) && RainbowRider(player))
			ModContent.GetInstance<LotOfRainbows>().Grant();

		if (GuardianFamilies.Contains(family) && Valid(fight, Guardian))
			ModContent.GetInstance<YareYareDaze>().Grant();

		if ((boss || MiniBosses.Contains(npc.type)) && Valid(fight, Broken))
			ModContent.GetInstance<NatureOfAThing>().Grant();

		if (boss && Valid(fight, Summons))
			ModContent.GetInstance<Pacifist>().Grant();

		if (boss && Valid(fight, Passive))
			ModContent.GetInstance<IDidntTouchYou>().Grant();

		if (IsMech(family) && Valid(fight, Ware))
			ModContent.GetInstance<ThisWare>().Grant();

		if (boss && (family >= BatchLogic.QueenSlime || family == 0 && Main.hardMode) && Valid(fight, Scope))
			ModContent.GetInstance<NoScope>().Grant();

		if (Valid(fight, Copper))
		{
			int display = ChallengeBoss(npc.type);
			if (display != 0)
				ModContent.GetInstance<TrueChallenge>().Complete("BOSS_" + display);
		}

		if (IsMech(family) && VanityArmor(player))
			ModContent.GetInstance<SteelFashioned>().Grant();

		if (boss && NinjaLook(player))
			ModContent.GetInstance<LikeANinja>().Grant();

		if (npc.type == NPCID.TheDestroyer && npc.GetGlobalNPC<ThirdNPC>().Age < 600)
			ModContent.GetInstance<ExtraCheese>().Grant();

		if (DeathBosses.Contains(BossMark(npc)) && !player.dead)
			ModContent.GetInstance<OhNoAnyway>().Grant();

		if (npc.type == NPCID.Angler && player.GetModPlayer<ThirdPlayer>().AnglerTalk > 0)
			ModContent.GetInstance<Meanie>().Grant();

		if (family == BatchLogic.Duke)
			player.GetModPlayer<ThirdPlayer>().DukeKilled();

		if (npc.type == NPCID.DD2DarkMageT1 || npc.type == NPCID.DD2OgreT2 || npc.type == NPCID.DD2Betsy)
			_invasionBoss = true;

		if (ThirdNPC.IsNameless(npc) && Valid(fight, Broken) && !NamelessPotion)
			ModContent.GetInstance<WhyNameless>().Grant();
	}

	public static void NoteDeath()
	{
		for (int i = 0; i < Main.maxNPCs; i++)
		{
			NPC npc = Main.npc[i];
			if (npc.active && IsBossLike(npc))
				DeathBosses.Add(BossMark(npc));
		}
	}

	public static void NoteExplosive(Projectile projectile)
	{
		uint now = Main.GameUpdateCount;
		if (_explosiveTick != now)
		{
			_explosiveTick = now;
			_explosiveCount = 0;
		}

		if (++_explosiveCount >= 10)
			ModContent.GetInstance<Kaboom>().Grant();

		Player player = Main.LocalPlayer;
		if (!BatchLogic.Client() || !player.mount.Active || !player.mount.Cart || BayTimer > 0)
			return;

		float dx = projectile.Center.X - player.Center.X;
		if (Math.Abs(dx) < 40f * 16f && Math.Abs(projectile.Center.Y - player.Center.Y) < 20f * 16f && Math.Sign(dx) == -player.direction)
			BayTimer = 120;
	}

	public static void Tick()
	{
		if (Main.GameUpdateCount % 60 == 0)
			PruneLedger();

		var alive = new HashSet<int>();
		for (int i = 0; i < Main.maxNPCs; i++)
		{
			NPC npc = Main.npc[i];
			if (npc.active && IsBossLike(npc))
				alive.Add(Key(npc));
		}

		if (alive.Count == 0)
		{
			DeathBosses.Clear();
			NamelessPotion = false;
		}

		SlimeGroup.Update(alive, 0);
		JungleGroup.Update(alive, 0);
		CrowdGroup.Update(alive, 5);
		TickInvasion();
	}

	public static void BuildWare()
	{
		WareItems.Clear();
		WareItems[BatchLogic.Destroyer] = new HashSet<int>();
		WareItems[BatchLogic.Twins] = new HashSet<int>();
		WareItems[BatchLogic.Prime] = new HashSet<int>();
		for (int i = 0; i < Recipe.numRecipes; i++)
		{
			Recipe recipe = Main.recipe[i];
			if (recipe?.createItem == null || recipe.createItem.damage <= 0)
				continue;

			foreach (Item ingredient in recipe.requiredItem)
			{
				if (ingredient.type == ItemID.SoulofMight)
					WareItems[BatchLogic.Destroyer].Add(recipe.createItem.type);
				else if (ingredient.type == ItemID.SoulofSight)
					WareItems[BatchLogic.Twins].Add(recipe.createItem.type);
				else if (ingredient.type == ItemID.SoulofFright)
					WareItems[BatchLogic.Prime].Add(recipe.createItem.type);
			}
		}
	}

	private static void TickInvasion()
	{
		bool active = Main.invasionType > 0 || DD2Event.Ongoing;
		if (active && !_invasion)
		{
			_invasion = true;
			_invasionDD2 = DD2Event.Ongoing;
			_invasionHits = 0;
			_invasionSpoiled = false;
			_invasionBoss = false;
		}
		else if (!active && _invasion)
		{
			_invasion = false;
			bool won = !_invasionDD2 || DD2Event.WonThisRun
				|| Main.netMode == NetmodeID.MultiplayerClient && !DD2Event.LostThisRun && _invasionBoss;
			if (won && _invasionHits > 0 && !_invasionSpoiled)
				ModContent.GetInstance<UseMoreGun>().Grant();
		}
	}

	private static void GroupKill(int key)
	{
		if (SlimeGroup.Kill(key))
			ModContent.GetInstance<SlimeSlayer>().Grant();

		if (JungleGroup.Kill(key))
			ModContent.GetInstance<JungleJam>().Grant();

		if (CrowdGroup.Kill(key))
			ModContent.GetInstance<NahIdWin>().Grant();
	}

	private static void PruneLedger()
	{
		if (Ledger.Count == 0)
			return;

		var alive = new HashSet<int>();
		for (int i = 0; i < Main.maxNPCs; i++)
		{
			if (Main.npc[i].active)
				alive.Add(Key(Main.npc[i]));
		}

		var stale = new List<int>();
		foreach (int key in Ledger.Keys)
		{
			if (!alive.Contains(key))
				stale.Add(key);
		}

		foreach (int key in stale)
			Ledger.Remove(key);
	}

	private static bool Valid(Fight fight, int bit)
	{
		return fight != null && fight.Hits > 0 && (fight.Valid & bit) != 0;
	}

	private static Fight FightOf(int key)
	{
		if (!Ledger.TryGetValue(key, out Fight fight))
		{
			fight = new Fight();
			Ledger[key] = fight;
		}

		return fight;
	}

	private static long BossMark(NPC npc)
	{
		return ((long)npc.whoAmI << 32) | (uint)npc.type;
	}

	private static bool IsMech(int family)
	{
		return family == BatchLogic.Destroyer || family == BatchLogic.Twins || family == BatchLogic.Prime;
	}

	private static int ChallengeBoss(int type)
	{
		return BatchLogic.Family(type) switch
		{
			BatchLogic.King => NPCID.KingSlime,
			BatchLogic.Eye => NPCID.EyeofCthulhu,
			BatchLogic.Eater => NPCID.EaterofWorldsHead,
			BatchLogic.Brain => NPCID.BrainofCthulhu,
			BatchLogic.Bee => NPCID.QueenBee,
			BatchLogic.Skeletron => NPCID.SkeletronHead,
			BatchLogic.Deer => NPCID.Deerclops,
			BatchLogic.Wall => NPCID.WallofFlesh,
			BatchLogic.QueenSlime => NPCID.QueenSlimeBoss,
			BatchLogic.Destroyer => NPCID.TheDestroyer,
			BatchLogic.Twins => NPCID.Retinazer,
			BatchLogic.Prime => NPCID.SkeletronPrime,
			BatchLogic.Plantera => NPCID.Plantera,
			BatchLogic.Golem => NPCID.Golem,
			BatchLogic.Duke => NPCID.DukeFishron,
			BatchLogic.Empress => NPCID.HallowBoss,
			BatchLogic.Cultist => NPCID.CultistBoss,
			BatchLogic.Moon => NPCID.MoonLordCore,
			_ => 0
		};
	}

	private static bool VanityArmor(Player player)
	{
		for (int slot = 0; slot < 3; slot++)
		{
			Item item = player.armor[slot];
			if (item.IsAir || !item.vanity)
				return false;
		}

		return true;
	}

	private static bool NinjaLook(Player player)
	{
		int accessories = 0;
		for (int slot = 3; slot < 10; slot++)
		{
			if (!player.IsItemSlotUnlockedAndUsable(slot) || player.armor[slot].IsAir)
				continue;

			accessories++;
			if (player.dye[slot].type != ItemID.ShadowDye)
				return false;
		}

		if (accessories == 0)
			return false;

		Point tile = player.Center.ToTileCoordinates();
		if (!WorldGen.InWorld(tile.X, tile.Y, 1))
			return false;

		Tile wall = Main.tile[tile.X, tile.Y];
		return wall.WallType > WallID.None && wall.WallColor == PaintID.ShadowPaint;
	}

	private static bool RainbowRider(Player player)
	{
		if (!player.mount.Active || player.mount.Type != MountID.Unicorn)
			return false;

		bool dyed = false;
		for (int slot = 0; slot < player.dye.Length; slot++)
		{
			if (player.dye[slot].type == ItemID.RainbowDye)
				dyed = true;
		}

		if (!dyed)
			return false;

		int y = (int)((player.position.Y + player.height + 8f) / 16f);
		int left = (int)(player.position.X / 16f);
		int right = (int)((player.position.X + player.width) / 16f);
		for (int x = left; x <= right; x++)
		{
			if (WorldGen.InWorld(x, y, 1) && Main.tile[x, y].HasTile && Main.tile[x, y].TileType == TileID.RainbowBrick)
				return true;
		}

		return false;
	}

	private class Fight
	{
		public int Hits;
		public int Valid = -1;
	}

	private class Group
	{
		private HashSet<int> _members;
		private readonly HashSet<int> _dead = new HashSet<int>();
		private readonly bool _open;
		private bool _armed;

		public Group(params int[] members)
		{
			_open = members.Length == 0;
			_members = new HashSet<int>(members);
		}

		public void Disarm()
		{
			_armed = false;
			_dead.Clear();
			if (_open)
				_members.Clear();
		}

		public void Update(HashSet<int> alive, int crowd)
		{
			if (!_armed)
			{
				bool ready = _open ? alive.Count >= crowd : _members.IsSubsetOf(alive);
				if (!ready)
					return;

				if (_open)
					_members = new HashSet<int>(alive);

				_armed = true;
				_dead.Clear();
				return;
			}

			foreach (int member in _members)
			{
				if (!alive.Contains(member) && !_dead.Contains(member))
				{
					Disarm();
					return;
				}
			}
		}

		public bool Kill(int key)
		{
			if (!_armed || !_members.Contains(key))
				return false;

			_dead.Add(key);
			if (_dead.Count < _members.Count)
				return false;

			Disarm();
			return true;
		}
	}
}
