using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using ReimaginingAchievements.Content.Achievements;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Events;
using Terraria.GameContent.UI;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace ReimaginingAchievements.Content.Tracking;

public class ThirdNPC : GlobalNPC
{
	private static int _namelessType = -2;

	public override bool InstancePerEntity => true;

	public int Age;
	public int Eggs;
	public bool TownFinal;

	public static bool IsNameless(NPC npc)
	{
		if (_namelessType == -2)
			_namelessType = ModContent.TryFind("NoxusBoss", "NamelessDeityBoss", out ModNPC nameless) ? nameless.Type : -1;

		return _namelessType >= 0 && npc.type == _namelessType;
	}

	public static bool IsNamelessAlive()
	{
		if (_namelessType == -1)
			return false;

		for (int i = 0; i < Main.maxNPCs; i++)
		{
			if (Main.npc[i].active && IsNameless(Main.npc[i]))
				return true;
		}

		return false;
	}

	public override void PostAI(NPC npc)
	{
		Age++;
	}

	public override void OnHitByItem(NPC npc, Player player, Item item, NPC.HitInfo hit, int damageDone)
	{
		if (player.whoAmI != Main.myPlayer)
			return;

		ThirdLogic.NoteHit(npc, new Strike { Kind = Strike.ByItem, Item = item.type, Prefix = item.prefix, Proj = -1 }, damageDone);
	}

	public override void OnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone)
	{
		if (projectile.owner != Main.myPlayer || projectile.npcProj || !BatchLogic.Client())
			return;

		ThirdLogic.NoteHit(npc, ThirdLogic.FromProjectile(projectile), damageDone);
		ThirdProjectile tracked = projectile.GetGlobalProjectile<ThirdProjectile>();

		if (projectile.type == ProjectileID.RottenEgg && ++Eggs >= 50)
			ModContent.GetInstance<StopIt>().Grant();

		if (projectile.type == ProjectileID.Meowmere)
		{
			tracked.Dealt += damageDone;
			if (tracked.Dealt >= 2500)
				ModContent.GetInstance<MassacreByCat>().Grant();
		}

		bool bee = projectile.type == ProjectileID.Bee || projectile.type == ProjectileID.GiantBee;
		if (bee && npc.type == NPCID.QueenBee && npc.life <= 0 && !tracked.FromWeapon)
			ModContent.GetInstance<Traitor>().Grant();
	}

	public override void OnHitNPC(NPC npc, NPC target, NPC.HitInfo hit)
	{
		if (npc.townNPC && target.life <= 0)
			target.GetGlobalNPC<ThirdNPC>().TownFinal = true;
	}

	public void HandleKill(NPC npc)
	{
		ThirdLogic.OnLocalKill(npc);
	}
}

public class ThirdProjectile : GlobalProjectile
{
	public override bool InstancePerEntity => true;

	public int Item;
	public int Prefix;
	public int Ammo;
	public bool FromWeapon;
	public int Dealt;
	private bool _seen;

	public override void OnSpawn(Projectile projectile, IEntitySource source)
	{
		if (source is EntitySource_ItemUse use && use.Item != null)
		{
			Item = use.Item.type;
			Prefix = use.Item.prefix;
			FromWeapon = true;
			if (source is EntitySource_ItemUse_WithAmmo withAmmo)
				Ammo = withAmmo.AmmoItemIdUsed;
		}
		else if (source is EntitySource_Parent parent && parent.Entity is Projectile owner && owner.active)
		{
			ThirdProjectile from = owner.GetGlobalProjectile<ThirdProjectile>();
			Item = from.Item;
			Prefix = from.Prefix;
			Ammo = from.Ammo;
			FromWeapon = from.FromWeapon;
		}

		if (projectile.owner != Main.myPlayer || Main.netMode == NetmodeID.Server)
			return;

		if (IsSpray(projectile.type))
			ThirdWorld.SprayUsed = true;

		if (projectile.type == ProjectileID.LovePotion)
			ThirdLogic.LoveThrown = Main.GameUpdateCount;
	}

	public override void PostAI(Projectile projectile)
	{
		if (_seen || Main.dedServ)
			return;

		_seen = true;
		if (projectile.type == ProjectileID.Explosives)
			ThirdLogic.NoteExplosive(projectile);
	}

	public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
	{
		if (projectile.npcProj && target.life <= 0)
			target.GetGlobalNPC<ThirdNPC>().TownFinal = true;
	}

	private static bool IsSpray(int type)
	{
		return type == ProjectileID.PureSpray || type == ProjectileID.HallowSpray || type == ProjectileID.CorruptSpray
			|| type == ProjectileID.MushroomSpray || type == ProjectileID.CrimsonSpray || type == ProjectileID.HolyWater
			|| type == ProjectileID.UnholyWater || type == ProjectileID.BloodWater;
	}
}

public class ThirdItem : GlobalItem
{
	public override void OnConsumeItem(Item item, Player player)
	{
		if (player.whoAmI != Main.myPlayer || !ItemID.Sets.IsFood[item.type])
			return;

		if (player.HasBuff(BuffID.Poisoned) || player.HasBuff(BuffID.Venom))
			ModContent.GetInstance<AppleADay>().Grant();
	}

	public override bool? UseItem(Item item, Player player)
	{
		if (player.whoAmI != Main.myPlayer || Main.netMode == NetmodeID.Server)
			return null;

		if (item.type == ItemID.Meowmere && player.mount.Active && player.mount.Type == MountID.MeowmereMinecart)
			ModContent.GetInstance<NyaNyaNya>().Grant();

		if (item.type == ItemID.PirateMap && ThirdPlayer.Wears(player, ItemID.SailorHat) && ThirdPlayer.Wears(player, ItemID.SailorShirt) && ThirdPlayer.Wears(player, ItemID.SailorPants))
			ModContent.GetInstance<Impostor>().Grant();

		if (ThirdLogic.Bombs.Contains(item.type))
			ModContent.GetInstance<Bomberman>().Add(1);

		return null;
	}

	public override void PostReforge(Item item)
	{
		if (Main.dedServ || Main.gameMenu)
			return;

		int spent = Main.LocalPlayer.GetModPlayer<ThirdPlayer>().TakeReforgeCost();
		if (spent <= 0)
			return;

		ModContent.GetInstance<Broke>().Add(spent);
		ModContent.GetInstance<BrokeAgain>().Add(spent);
	}
}

public class ThirdPlayer : ModPlayer
{
	private static readonly int[] AnkhDebuffs =
	{
		BuffID.Chilled, BuffID.Weak, BuffID.BrokenArmor, BuffID.Bleeding, BuffID.Poisoned, BuffID.Slow,
		BuffID.Confused, BuffID.Silenced, BuffID.Cursed, BuffID.Darkness, BuffID.Stoned
	};
	private static readonly HashSet<int> FallSavers = new HashSet<int>
	{
		ItemID.LuckyHorseshoe, ItemID.ObsidianHorseshoe, ItemID.BlueHorseshoeBalloon, ItemID.WhiteHorseshoeBalloon,
		ItemID.YellowHorseshoeBalloon, ItemID.BalloonHorseshoeFart, ItemID.BalloonHorseshoeHoney, ItemID.BalloonHorseshoeSharkron
	};
	private static readonly HashSet<int> DrownSavers = new HashSet<int>
	{
		ItemID.BreathingReed, ItemID.DivingHelmet, ItemID.DivingGear, ItemID.JellyfishDivingGear, ItemID.ArcticDivingGear,
		ItemID.NeptunesShell, ItemID.MoonShell, ItemID.CelestialShell, ItemID.GillsPotion
	};
	private static readonly HashSet<int> LavaSavers = new HashSet<int>
	{
		ItemID.LavaCharm, ItemID.LavaWaders, ItemID.MoltenCharm, ItemID.TerrasparkBoots, ItemID.ObsidianSkinPotion
	};
	private static readonly HashSet<int> Horses = new HashSet<int>
	{
		MountID.PaintedHorse, MountID.MajesticHorse, MountID.DarkHorse
	};
	private static readonly int[,] LovePairs =
	{
		{ NPCID.Nurse, NPCID.ArmsDealer },
		{ NPCID.Mechanic, NPCID.GoblinTinkerer }
	};

	public int AnglerTalk;

	private int _ticks;
	private int _scratch;
	private bool _fishFight;
	private bool _fishSpoiled;
	private bool _underControl;
	private double _coma;
	private readonly Dictionary<int, uint> _lovestruck = new Dictionary<int, uint>();
	private readonly HashSet<int> _combo = new HashSet<int>();
	private int _dodges;
	private int _shimmer;
	private int _khanSide;
	private bool _inTemple;
	private bool _templeRun;
	private bool _golemSeen;
	private bool _zenithSold;
	private long _coins = -1;
	private int _lifeBeforeNurse;
	private int _debuffsBeforeNurse;

	public static bool Wears(Player player, int type)
	{
		for (int slot = 0; slot < player.armor.Length; slot++)
		{
			if (player.armor[slot].type == type)
				return true;
		}

		return false;
	}

	public override void OnEnterWorld()
	{
		ThirdLogic.Reset();
		_ticks = 0;
		_scratch = 0;
		_fishFight = false;
		_underControl = false;
		_coma = 0;
		_lovestruck.Clear();
		_combo.Clear();
		_dodges = 0;
		_shimmer = 0;
		_khanSide = 0;
		_inTemple = false;
		_templeRun = false;
		_golemSeen = NPC.AnyNPCs(NPCID.Golem);
		_zenithSold = false;
		_coins = -1;

		SeedCollector seeds = ModContent.GetInstance<SeedCollector>();
		bool[] flags =
		{
			Main.drunkWorld, Main.notTheBeesWorld, Main.getGoodWorld, Main.tenthAnniversaryWorld,
			Main.dontStarveWorld, Main.remixWorld, Main.noTrapsWorld, Main.zenithWorld
		};
		for (int i = 0; i < flags.Length; i++)
		{
			if (flags[i])
				seeds.Complete(SeedCollector.Seeds[i]);
		}
	}

	public override void OnHurt(Player.HurtInfo info)
	{
		if (Player.whoAmI != Main.myPlayer)
			return;

		if (info.Damage >= 400)
			_scratch = 1200;

		_dodges = 0;
		_templeRun = false;
		ThirdLogic.BayTimer = 0;
	}

	public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
	{
		if (Player.whoAmI != Main.myPlayer)
			return;

		_scratch = 0;
		_underControl = false;
		ThirdLogic.NoteDeath();
		ThirdLogic.MancheSpoiled = true;

		int cause = damageSource.SourceOtherIndex;
		HashSet<int> savers = cause switch
		{
			0 => FallSavers,
			1 => DrownSavers,
			2 => LavaSavers,
			_ => null
		};
		if (savers != null && CarriesUnused(savers))
			ModContent.GetInstance<ThatsEmbarrassing>().Grant();

		if (cause == 7 && HeadInSand() && Wears(Player, ItemID.GoldHelmet) && Wears(Player, ItemID.GoldChainmail) && Wears(Player, ItemID.GoldGreaves))
			ModContent.GetInstance<PharaohsFate>().Grant();
	}

	public override void ModifyNursePrice(NPC nurse, int health, bool removeDebuffs, ref int price)
	{
		_lifeBeforeNurse = Player.statLife;
		_debuffsBeforeNurse = Debuffs();
	}

	public override void OnCatchNPC(NPC npc, Item item, bool failed)
	{
		if (failed || Player.whoAmI != Main.myPlayer || npc.type != NPCID.TruffleWorm)
			return;

		Vector2 below = new Vector2(npc.position.X, npc.position.Y + npc.height);
		if (!Collision.SolidCollision(below, npc.width, 6))
			ModContent.GetInstance<OutOfThinAir>().Grant();
	}

	public override void PostNurseHeal(NPC nurse, int health, bool removeDebuffs, int price)
	{
		if (Player.whoAmI == Main.myPlayer && _lifeBeforeNurse * 2 < Player.statLifeMax2 && _debuffsBeforeNurse >= 3)
			ModContent.GetInstance<OverlyDramatic>().Grant();
	}

	public override void OnConsumeMana(Item item, int manaConsumed)
	{
		if (Player.whoAmI != Main.myPlayer || item.type != ItemID.LaserMachinegun)
			return;

		ThirdLogic.StormSpent += manaConsumed;
		if (ThirdLogic.StormSpent >= 400)
			ModContent.GetInstance<TrueStormtrooper>().Grant();
	}

	public override void PostSellItem(NPC vendor, Item[] shopInventory, Item item)
	{
		if (Player.whoAmI != Main.myPlayer)
			return;

		if (item.type == ItemID.CrystalShard)
			ModContent.GetInstance<BreakingBad>().Add(item.stack);

		if (item.type == ItemID.Zenith)
			_zenithSold = true;
	}

	public int TakeReforgeCost()
	{
		long now = TotalCoins();
		long spent = _coins < 0 ? 0 : _coins - now;
		_coins = now;
		return (int)Math.Clamp(spent, 0, int.MaxValue);
	}

	public void DukeKilled()
	{
		if (_fishFight && !_fishSpoiled)
			ModContent.GetInstance<BiggerFish>().Grant();

		_fishFight = false;
	}

	public void NoteDodge()
	{
		if (!ThirdLogic.AnyBoss())
			return;

		if (++_dodges >= 3)
			ModContent.GetInstance<CantTouchThis>().Grant();
	}

	public void NoteStomp(NPC npc)
	{
		if (npc.life <= 0 && !npc.townNPC && npc.lifeMax > 5 && !npc.friendly)
			ModContent.GetInstance<OneUp>().Grant();

		_combo.Add(npc.whoAmI);
		if (_combo.Count >= 15)
			ModContent.GetInstance<ComboBreaker>().Grant();
	}

	public override void PostUpdate()
	{
		if (Player.whoAmI != Main.myPlayer || !BatchLogic.Client())
			return;

		_ticks++;
		ThirdLogic.UpdateClock();
		ThirdLogic.Tick();

		UpdateTimers();
		UpdateFish();
		UpdateDay();
		UpdateLove();
		UpdateMount();
		UpdateStates();
		UpdateRoutes();
		UpdateMancha();

		if (_ticks % 30 == 0)
			UpdateSlow();
	}

	private void UpdateTimers()
	{
		if (Player.dead)
			_scratch = 0;

		if (_scratch > 0 && --_scratch == 0)
			ModContent.GetInstance<TisButAScratch>().Grant();

		if (ThirdLogic.BayTimer > 0 && --ThirdLogic.BayTimer == 0 && !Player.dead)
			ModContent.GetInstance<MichaelBay>().Grant();

		if (AnglerTalk > 0)
			AnglerTalk--;

		if (Player.talkNPC >= 0 && Main.npc[Player.talkNPC].type == NPCID.Angler)
			AnglerTalk = 30;

		if (ThirdLogic.AngerTimer > 0)
			ThirdLogic.AngerTimer--;

		if (!Player.channel || Player.HeldItem.type != ItemID.LaserMachinegun)
			ThirdLogic.StormSpent = 0;

		if (Main.InReforgeMenu)
		{
			if (_coins < 0)
				_coins = TotalCoins();
		}
		else
		{
			_coins = -1;
		}

		if (_zenithSold && Main.npcShop == 0)
		{
			_zenithSold = false;
			if (!Player.HasItem(ItemID.Zenith))
				ModContent.GetInstance<JustWhy>().Grant();
		}
	}

	private void UpdateFish()
	{
		bool duke = NPC.AnyNPCs(NPCID.DukeFishron);
		if (!duke)
		{
			_fishFight = false;
			return;
		}

		if (!_fishFight)
		{
			_fishFight = true;
			_fishSpoiled = false;
		}

		if (!Player.wet || Player.mount.Active && Player.mount.Type == MountID.CuteFishron)
			_fishSpoiled = true;
	}

	private void UpdateDay()
	{
		if (Player.sleeping.isSleeping)
		{
			_coma += ThirdLogic.Delta;
			if (_coma >= 3 * (Main.dayLength + Main.nightLength))
				ModContent.GetInstance<TemporaryComa>().Grant();
		}
		else
		{
			_coma = 0;
		}

		if (ThirdLogic.Dawn)
		{
			if (_underControl && !Player.dead)
				ModContent.GetInstance<UnderControl>().Grant();

			_underControl = Debuffs() >= 3 && !Player.dead;
		}
		else if (_underControl && (Player.dead || Debuffs() < 3))
		{
			_underControl = false;
		}
	}

	private void UpdateLove()
	{
		uint now = Main.GameUpdateCount;
		if (ThirdLogic.LoveThrown == 0 || now - ThirdLogic.LoveThrown > 1200)
		{
			_lovestruck.Clear();
			return;
		}

		for (int i = 0; i < Main.maxNPCs; i++)
		{
			NPC npc = Main.npc[i];
			if (npc.active && npc.townNPC && npc.HasBuff(BuffID.Lovestruck) && !_lovestruck.ContainsKey(npc.type))
				_lovestruck[npc.type] = now;
		}

		for (int pair = 0; pair < LovePairs.GetLength(0); pair++)
		{
			if (_lovestruck.TryGetValue(LovePairs[pair, 0], out uint first) && _lovestruck.TryGetValue(LovePairs[pair, 1], out uint second)
				&& Math.Abs((long)first - second) <= 600)
				ModContent.GetInstance<LovesInTheAir>().Grant();
		}
	}

	private void UpdateMount()
	{
		bool slime = Player.mount.Active && (Player.mount.Type == MountID.Slime || Player.mount.Type == MountID.QueenSlime);
		if (!slime || Player.velocity.Y == 0f)
			_combo.Clear();

		bool boss = ThirdLogic.AnyBoss();
		if (boss && Player.mount.Active && Player.mount.Type == MountID.GolfCartSomebodySaveMe && Player.HasBuff(BuffID.Tipsy))
			ModContent.GetInstance<FloridaMan>().Grant();

		bool horse = Player.mount.Active && Horses.Contains(Player.mount.Type);
		bool bare = Player.armor[0].IsAir && Player.armor[1].IsAir && Player.armor[2].IsAir;
		bool jumped = Vector2.Distance(Player.position, Player.oldPosition) > 160f;
		if (!horse || !bare || jumped)
		{
			_khanSide = 0;
		}
		else if (Player.ZoneBeach)
		{
			int side = Player.Center.X < Main.maxTilesX * 8f ? -1 : 1;
			if (_khanSide == 0)
				_khanSide = side;
			else if (_khanSide != side)
				ModContent.GetInstance<GreatKhan>().Grant();
		}
	}

	private void UpdateStates()
	{
		if (Player.HasBuff(BuffID.Webbed) && Player.HasBuff(BuffID.Stoned) && Player.HasBuff(BuffID.Frozen) && Player.grapCount > 0)
			ModContent.GetInstance<GetMeOuttaHere>().Grant();

		if (Player.aggro <= -2000)
			ModContent.GetInstance<Stealth>().Grant();

		if (Player.sitting.isSitting && Player.HasBuff(BuffID.WellFed3) && OnToilet())
			ModContent.GetInstance<Defecator>().Grant();

		if (Player.shimmering && Collision.SolidCollision(Player.position, Player.width, Player.height))
		{
			if (++_shimmer >= 600)
				ModContent.GetInstance<WorldwideSlideshow>().Grant();
		}
		else
		{
			_shimmer = 0;
		}

		bool ankh = true;
		foreach (int debuff in AnkhDebuffs)
		{
			if (!Player.HasBuff(debuff))
			{
				ankh = false;
				break;
			}
		}

		if (ankh)
			ModContent.GetInstance<AntiAnkh>().Grant();

		if (ThirdNPC.IsNamelessAlive() && HasPotionBuff())
			ThirdLogic.NamelessPotion = true;
	}

	private void UpdateRoutes()
	{
		bool temple = Player.ZoneLihzhardTemple;
		if (temple && !_inTemple)
			_templeRun = Player.HasBuff(BuffID.Invisibility);

		if (!temple || !Player.HasBuff(BuffID.Invisibility))
			_templeRun = false;

		_inTemple = temple;

		bool golem = NPC.AnyNPCs(NPCID.Golem);
		if (golem && !_golemSeen && _templeRun)
			ModContent.GetInstance<MetalGearTemple>().Grant();

		_golemSeen = golem;
	}

	private void UpdateMancha()
	{
		if (!ThirdLogic.MancheRunning)
		{
			if (Main.bloodMoon && Main.hardMode && !Main.dayTime && Main.time < 600)
			{
				ThirdLogic.MancheRunning = true;
				ThirdLogic.MancheHits = 0;
				ThirdLogic.MancheSpoiled = false;
			}

			return;
		}

		if (!Main.bloodMoon)
		{
			ThirdLogic.MancheRunning = false;
			if (ThirdLogic.MancheHits > 0 && !ThirdLogic.MancheSpoiled)
				ModContent.GetInstance<LaManchaland>().Grant();

			return;
		}

		if (Player.dead || Player.mount.Active)
		{
			ThirdLogic.MancheSpoiled = true;
			return;
		}

		for (int slot = 0; slot < 10; slot++)
		{
			if (!Player.armor[slot].IsAir)
			{
				ThirdLogic.MancheSpoiled = true;
				return;
			}
		}
	}

	private void UpdateSlow()
	{
		ModContent.GetInstance<Alchemist>().Check(Player);

		if (!Main.hardMode && WorldGen.tEvil == 0 && WorldGen.tBlood == 0 && !ThirdWorld.SprayUsed && _ticks > 18000)
			ModContent.GetInstance<PureInsanity>().Grant();

		if (WorldGen.tEvil + WorldGen.tBlood + WorldGen.tGood >= 100)
			ModContent.GetInstance<GoodRiddance>().Grant();

		string name = Player.name.Trim();
		if ((name.Equals("Big Boss", StringComparison.OrdinalIgnoreCase) || name.Equals("Snake", StringComparison.OrdinalIgnoreCase)) && Wears(Player, ItemID.Sunglasses))
			ModContent.GetInstance<PrettySolidName>().Grant();

		bool maid = Wears(Player, ItemID.MaidHead) && Wears(Player, ItemID.MaidShirt) && Wears(Player, ItemID.MaidPants)
			|| Wears(Player, ItemID.MaidHead2) && Wears(Player, ItemID.MaidShirt2) && Wears(Player, ItemID.MaidPants2);
		bool tail = Wears(Player, ItemID.FoxTail) || Wears(Player, ItemID.DogTail) || Wears(Player, ItemID.BunnyTail) || Wears(Player, ItemID.LizardTail);
		if (maid && tail)
			ModContent.GetInstance<IfThatsWhatYouWant>().Grant();

		var banners = new Dictionary<int, int>();
		for (int i = 0; i < Main.InventorySlotsTotal; i++)
		{
			Item item = Player.inventory[i];
			if (item.IsAir || item.createTile != TileID.Banners)
				continue;

			banners.TryGetValue(item.type, out int count);
			count += item.stack;
			banners[item.type] = count;
			if (count >= 20)
			{
				ModContent.GetInstance<ExtinctionEvent>().Grant();
				break;
			}
		}
	}

	private int Debuffs()
	{
		int count = 0;
		for (int i = 0; i < Player.buffType.Length; i++)
		{
			int type = Player.buffType[i];
			if (type > 0 && Player.buffTime[i] > 0 && Main.debuff[type])
				count++;
		}

		return count;
	}

	private bool HasPotionBuff()
	{
		foreach (int buff in VanillaCatalog.Potions())
		{
			if (Player.HasBuff(buff))
				return true;
		}

		return false;
	}

	private bool CarriesUnused(HashSet<int> savers)
	{
		for (int i = 0; i < Main.InventorySlotsTotal; i++)
		{
			if (savers.Contains(Player.inventory[i].type))
				return true;
		}

		return false;
	}

	private bool HeadInSand()
	{
		Point head = new Vector2(Player.Center.X, Player.position.Y + 4f).ToTileCoordinates();
		if (!WorldGen.InWorld(head.X, head.Y, 1))
			return false;

		Tile tile = Main.tile[head.X, head.Y];
		return tile.HasTile && TileID.Sets.Falling[tile.TileType];
	}

	private bool OnToilet()
	{
		Point feet = new Vector2(Player.Center.X, Player.position.Y + Player.height - 8f).ToTileCoordinates();
		for (int dy = -1; dy <= 1; dy++)
		{
			int y = feet.Y + dy;
			if (WorldGen.InWorld(feet.X, y, 1) && Main.tile[feet.X, y].HasTile && Main.tile[feet.X, y].TileType == TileID.Toilets)
				return true;
		}

		return false;
	}

	private long TotalCoins()
	{
		long total = Utils.CoinsCount(out _, Player.inventory);
		total += Utils.CoinsCount(out _, Player.bank.item);
		total += Utils.CoinsCount(out _, Player.bank2.item);
		total += Utils.CoinsCount(out _, Player.bank3.item);
		total += Utils.CoinsCount(out _, Player.bank4.item);
		return total;
	}
}

public class ThirdWorld : ModSystem
{
	private const int Events = 10;
	private const int ScanColumns = 12;

	public static bool SprayUsed;

	private static bool _hadMushroom;
	private static bool _hadJungle;
	private static double _clock;
	private static readonly double[] Seen = new double[Events];
	private static double _prevAbs = -1;
	private static bool _prevDay;
	private static readonly List<long> Wedding = new List<long>();
	private static readonly HashSet<long> WeddingDead = new HashSet<long>();
	private static int _scanX;
	private static int _mushroom;
	private static int _jungle;

	public override void Load()
	{
		On_Player.NinjaDodge += (orig, self) =>
		{
			orig(self);
			NoteDodge(self);
		};
		On_Player.ShadowDodge += (orig, self) =>
		{
			orig(self);
			NoteDodge(self);
		};
		On_Player.BrainOfConfusionDodge += (orig, self) =>
		{
			orig(self);
			NoteDodge(self);
		};
		On_Player.ApplyDamageToNPC += OnApplyDamage;
		On_EmoteBubble.MakeLocalPlayerEmote += (orig, emoteId) =>
		{
			orig(emoteId);
			if (emoteId == EmoteID.EmotionAnger)
				ThirdLogic.AngerTimer = 300;
		};
	}

	public override void PostAddRecipes()
	{
		ThirdLogic.BuildWare();
	}

	public override void OnWorldLoad()
	{
		Clear();
	}

	public override void OnWorldUnload()
	{
		Clear();
	}

	public override void SaveWorldData(TagCompound tag)
	{
		tag["sprayUsed"] = SprayUsed;
		tag["hadMushroom"] = _hadMushroom;
		tag["hadJungle"] = _hadJungle;
		tag["clock"] = _clock;
		tag["eventSeen"] = new List<double>(Seen);
	}

	public override void LoadWorldData(TagCompound tag)
	{
		SprayUsed = tag.GetBool("sprayUsed");
		_hadMushroom = tag.GetBool("hadMushroom");
		_hadJungle = tag.GetBool("hadJungle");
		_clock = tag.GetDouble("clock");
		IList<double> seen = tag.GetList<double>("eventSeen");
		for (int i = 0; i < Events && i < seen.Count; i++)
			Seen[i] = seen[i];
	}

	public override void PostUpdateWorld()
	{
		double abs = Main.dayTime ? Main.time : Main.dayLength + Main.time;
		double delta = _prevAbs < 0 ? 0 : abs - _prevAbs;
		if (delta < 0)
			delta += Main.dayLength + Main.nightLength;

		if (delta <= 3600)
			_clock += delta;

		_prevAbs = abs;
		bool dawn = Main.dayTime && !_prevDay;
		_prevDay = Main.dayTime;

		UpdateEvents();
		if (dawn)
			SnapshotWedding();

		Scan();
	}

	public static void HandleKill(NPC npc)
	{
		if (npc.townNPC && Wedding.Count >= 20 && npc.lastInteraction != 255)
		{
			long mark = Mark(npc);
			if (Wedding.Contains(mark) && WeddingDead.Add(mark) && WeddingDead.Count >= Wedding.Count)
			{
				Award.Grant<RedWedding>();
				Wedding.Clear();
				WeddingDead.Clear();
			}
		}

		if (ThirdLogic.IsBossLike(npc) && ThirdLogic.IsFinal(npc) && npc.GetGlobalNPC<ThirdNPC>().TownFinal)
			Award.Grant<TakingCredit>();
	}

	private static void NoteDodge(Player player)
	{
		if (player.whoAmI == Main.myPlayer && BatchLogic.Client())
			player.GetModPlayer<ThirdPlayer>().NoteDodge();
	}

	private static void OnApplyDamage(On_Player.orig_ApplyDamageToNPC orig, Player self, NPC npc, int damage, float knockback, int direction, bool crit, DamageClass damageType, bool damageVariation)
	{
		int before = npc.life;
		orig(self, npc, damage, knockback, direction, crit, damageType, damageVariation);
		if (self.whoAmI != Main.myPlayer || !BatchLogic.Client())
			return;

		int dealt = Math.Max(before - npc.life, 1);
		ThirdLogic.NoteHit(npc, new Strike { Kind = Strike.ByPassive, Proj = -1 }, dealt);

		if (self.mount.Active && (self.mount.Type == MountID.Slime || self.mount.Type == MountID.QueenSlime))
			self.GetModPlayer<ThirdPlayer>().NoteStomp(npc);
	}

	private static void Clear()
	{
		SprayUsed = false;
		_hadMushroom = false;
		_hadJungle = false;
		_clock = 0;
		Array.Clear(Seen);
		_prevAbs = -1;
		_prevDay = Main.dayTime;
		Wedding.Clear();
		WeddingDead.Clear();
		_scanX = 0;
		_mushroom = 0;
		_jungle = 0;
	}

	private static void UpdateEvents()
	{
		bool[] active =
		{
			Main.bloodMoon,
			Main.invasionType == InvasionID.GoblinArmy,
			Main.slimeRain,
			Main.invasionType == InvasionID.PirateInvasion,
			Main.eclipse,
			Main.invasionType == InvasionID.SnowLegion,
			Main.pumpkinMoon,
			Main.snowMoon,
			Main.invasionType == InvasionID.MartianMadness,
			DD2Event.Ongoing
		};

		bool all = true;
		for (int i = 0; i < Events; i++)
		{
			if (active[i])
				Seen[i] = _clock + 1;

			if (Seen[i] <= 0 || _clock + 1 - Seen[i] > 2 * (Main.dayLength + Main.nightLength))
				all = false;
		}

		if (all)
			Award.Grant<NoisyBunch>();
	}

	private static void SnapshotWedding()
	{
		Wedding.Clear();
		WeddingDead.Clear();
		for (int i = 0; i < Main.maxNPCs; i++)
		{
			NPC npc = Main.npc[i];
			if (!npc.active || !npc.townNPC || npc.homeless)
				continue;

			if ((uint)npc.type < (uint)NPCID.Sets.IsTownPet.Length && NPCID.Sets.IsTownPet[npc.type])
				continue;

			Wedding.Add(Mark(npc));
		}
	}

	private static long Mark(NPC npc)
	{
		return ((long)npc.whoAmI << 32) | (uint)npc.type;
	}

	private static void Scan()
	{
		for (int column = 0; column < ScanColumns; column++)
		{
			if (_scanX >= Main.maxTilesX)
			{
				if (_mushroom > 0)
					_hadMushroom = true;

				if (_jungle > 0)
					_hadJungle = true;

				if (_hadMushroom && _mushroom == 0 || _hadJungle && _jungle == 0)
					Award.Grant<WaitThisIsntRight>();

				_scanX = 0;
				_mushroom = 0;
				_jungle = 0;
				return;
			}

			int x = _scanX++;
			for (int y = 0; y < Main.maxTilesY; y++)
			{
				Tile tile = Main.tile[x, y];
				if (!tile.HasTile)
					continue;

				if (tile.TileType == TileID.MushroomGrass)
					_mushroom++;
				else if (tile.TileType == TileID.JungleGrass)
					_jungle++;
			}
		}
	}
}
