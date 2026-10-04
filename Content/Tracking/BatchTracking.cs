using System.Collections.Generic;
using Microsoft.Xna.Framework;
using ReimaginingAchievements.Content.Achievements;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ReimaginingAchievements.Content.Tracking;

public class BatchPlayer : ModPlayer
{
	private const int HighwayTime = 90 * 60;
	private const int MelodyTime = 5 * 60 * 60;
	private const int EmpressTime = 60 * 60;
	private const int StillBuffGoal = 12;
	private const int EvilGoal = 200;

	private static readonly HashSet<int> MinionBuffs = new HashSet<int>
	{
		BuffID.Pygmies, BuffID.Ravens, BuffID.HornetMinion, BuffID.ImpMinion, BuffID.SpiderMinion,
		BuffID.TwinEyesMinion, BuffID.PirateMinion, BuffID.SharknadoMinion, BuffID.UFOMinion,
		BuffID.DeadlySphere, BuffID.StardustMinion, BuffID.StardustGuardianMinion, BuffID.StardustDragonMinion,
		BuffID.BatOfLight, BuffID.VampireFrog, BuffID.StormTiger, BuffID.Smolstar, BuffID.EmpressBlade,
		BuffID.FlinxMinion, BuffID.AbigailMinion
	};

	private readonly bool[] _ride = new bool[16];
	private int _highway;
	private int _melody;
	private int _empressCalm;
	private int _empressGone;
	private int _snowTick;
	private bool _empressReady;

	public int RideSeen;
	public int StillBuffs;
	public int EvilTiles;
	public int MelodySeconds;
	public bool Fired;

	public override void OnEnterWorld()
	{
		_highway = 0;
		_melody = 0;
		_empressCalm = 0;
		_empressGone = 0;
		_empressReady = false;
		Fired = false;
		ClearRide();
		BatchLogic.Reset();
	}

	public override void PostUpdate()
	{
		if (Player.whoAmI != Main.myPlayer || !BatchLogic.Client())
			return;

		UpdateHighway();
		UpdateLook();
		UpdateRide();
		UpdateAltar();
		UpdateShiny();
		UpdateBuffs();
		UpdateRevitalized();
		UpdateEmpress();
		UpdateEvil();
		UpdateMelody();
		UpdateSnow();
		BatchLogic.TickOffice();
	}

	public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
	{
		if (Player.whoAmI != Main.myPlayer || !BatchLogic.Client())
			return;

		BatchLogic.NotePlayerHit(target, item.type, damageDone);
		BatchLogic.NoteTownHurt(target);
	}

	public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
	{
		if (Player.whoAmI != Main.myPlayer || !BatchLogic.Client())
			return;

		int weapon = proj.GetGlobalProjectile<BatchProjectile>().Weapon;
		if (weapon <= 0)
			weapon = Player.HeldItem.type;

		BatchLogic.NotePlayerHit(target, weapon, damageDone);
		BatchLogic.NoteTownHurt(target);
	}

	public override void OnHitByNPC(NPC npc, Player.HurtInfo hurtInfo)
	{
		TryBackstab(npc.Center.X);
	}

	public override void OnHitByProjectile(Projectile proj, Player.HurtInfo hurtInfo)
	{
		TryBackstab(proj.Center.X);
	}

	public override void OnHurt(Player.HurtInfo info)
	{
		if (info.Cancelled || info.HitDirection == 0 || info.HitDirection != Player.direction)
			return;

		TryBackstab(Player.Center.X - info.HitDirection);
	}

	public static int RequiredBiomes()
	{
		if (Main.gameMenu)
			return 14;

		int count = 15;
		if (!NPC.downedPlantBoss)
			count--;

		return count;
	}

	public static bool BiomeRequired(int index)
	{
		if (index == 4)
			return WorldGen.crimson;

		if (index == 5)
			return !WorldGen.crimson;

		if (index == 14)
			return NPC.downedPlantBoss;

		return true;
	}

	private void UpdateHighway()
	{
		if (Player.position.Y <= 16f * 8f)
		{
			_highway = HighwayTime;
			return;
		}

		if (_highway <= 0)
			return;

		_highway--;
		int feet = (int)((Player.position.Y + Player.height) / 16f);
		if (Player.ZoneUnderworldHeight && feet >= Main.maxTilesY - 80)
			ModContent.GetInstance<HighwayToHell>().Grant();
	}

	private void UpdateLook()
	{
		if (Player.aggro >= 1700 && Player.HasBuff(BuffID.Battle) && Player.HasBuff(BuffID.WaterCandle))
			ModContent.GetInstance<LookAtMe>().Grant();
	}

	private void UpdateRide()
	{
		bool riding = Player.mount.Active && Player.mount.Cart;
		bool moving = System.Math.Abs(Player.velocity.X) > 0.2f || System.Math.Abs(Player.velocity.Y) > 0.2f;
		if (!riding || !moving)
		{
			if (RideSeen > 0)
				ClearRide();

			return;
		}

		bool[] here = Here();
		for (int i = 0; i < here.Length; i++)
		{
			if (here[i] && BiomeRequired(i))
				_ride[i] = true;
		}

		RideSeen = 0;
		for (int i = 0; i < _ride.Length; i++)
		{
			if (_ride[i])
				RideSeen++;
		}

		if (RideSeen >= RequiredBiomes())
			ModContent.GetInstance<RideOfALifetime>().Grant();
	}

	private void ClearRide()
	{
		for (int i = 0; i < _ride.Length; i++)
			_ride[i] = false;

		RideSeen = 0;
	}

	private bool[] Here()
	{
		return new bool[]
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
	}

	private void UpdateAltar()
	{
		if (!Main.getGoodWorld)
			return;

		int tileX = (int)(Player.Center.X / 16f);
		int tileY = (int)(Player.Center.Y / 16f);
		for (int x = tileX - 4; x <= tileX + 4; x++)
		{
			for (int y = tileY - 4; y <= tileY + 4; y++)
			{
				Tile tile = Framing.GetTileSafely(x, y);
				if (tile.HasTile && tile.TileType == TileID.LihzahrdAltar)
				{
					ModContent.GetInstance<Been50Years>().Grant();
					return;
				}
			}
		}
	}

	private void UpdateShiny()
	{
		if (Player.HasBuff(BuffID.LeafCrystal))
			ModContent.GetInstance<OhhExtraShiny>().GrantLeaf();
	}

	private void UpdateBuffs()
	{
		int count = 0;
		for (int i = 0; i < Player.buffType.Length; i++)
		{
			if (Player.buffTime[i] <= 0)
				continue;

			int buff = Player.buffType[i];
			if (buff <= 0 || buff >= Main.buffNoTimeDisplay.Length || !Main.buffNoTimeDisplay[buff])
				continue;

			if (IsPet(buff) || IsMount(buff) || MinionBuffs.Contains(buff))
				continue;

			count++;
		}

		StillBuffs = count;
		if (count >= StillBuffGoal)
			ModContent.GetInstance<Unflappable>().Grant();
	}

	private static bool IsPet(int buff)
	{
		if (buff < Main.vanityPet.Length && Main.vanityPet[buff])
			return true;

		return buff < Main.lightPet.Length && Main.lightPet[buff];
	}

	private static bool IsMount(int buff)
	{
		BuffID.Sets.BuffMountData[] mounts = BuffID.Sets.BasicMountData;
		return buff < mounts.Length && mounts[buff] != null;
	}

	private void UpdateRevitalized()
	{
		if (Player.statLifeMax >= 500
			&& Player.statManaMax >= 200
			&& Player.extraAccessory
			&& Player.usedAegisCrystal
			&& Player.usedAegisFruit
			&& Player.usedArcaneCrystal
			&& Player.usedGalaxyPearl
			&& Player.usedGummyWorm
			&& Player.usedAmbrosia)
			ModContent.GetInstance<ReRevitalized>().Grant();
	}

	private void UpdateEmpress()
	{
		bool alive = NPC.AnyNPCs(NPCID.HallowBoss);
		if (!alive)
		{
			_empressGone++;
			if (_empressGone > 10)
			{
				_empressReady = false;
				_empressCalm = 0;
			}

			Fired = false;
			return;
		}

		_empressGone = 0;
		if (_empressReady)
		{
			Fired = false;
			return;
		}

		NPC empress = null;
		for (int i = 0; i < Main.maxNPCs; i++)
		{
			NPC npc = Main.npc[i];
			if (npc.active && npc.type == NPCID.HallowBoss)
			{
				empress = npc;
				break;
			}
		}

		bool near = empress != null && Main.dayTime && Vector2.Distance(Player.Center, empress.Center) < 2200f;
		bool summoned = HasSummon();
		if (!near || Fired || summoned)
			_empressCalm = 0;
		else
			_empressCalm++;

		if (_empressCalm >= EmpressTime)
			_empressReady = true;

		Fired = false;
	}

	private bool HasSummon()
	{
		for (int i = 0; i < Main.maxProjectiles; i++)
		{
			Projectile proj = Main.projectile[i];
			if (proj.active && proj.owner == Player.whoAmI && (proj.minion || proj.sentry))
				return true;
		}

		return false;
	}

	public bool EmpressReady => _empressReady;

	public void ClearEmpress()
	{
		_empressReady = false;
		_empressCalm = 0;
	}

	private void UpdateEvil()
	{
		BatchLogic.TickSpray();
		EvilTiles = BatchLogic.EvilCount;
		if (EvilTiles >= EvilGoal)
			ModContent.GetInstance<WrongKindOfEvil>().Grant();
	}

	private void UpdateMelody()
	{
		bool seated = Player.sitting.isSitting || Player.sleeping.isSleeping;
		bool attacking = Player.itemAnimation > 0 && Player.HeldItem.damage > 0;
		if (seated && !attacking && Main.musicBox2 >= 0)
			_melody++;
		else
			_melody = 0;

		if (_melody > MelodyTime)
			_melody = MelodyTime;

		MelodySeconds = _melody / 60;
		if (_melody >= MelodyTime)
			ModContent.GetInstance<LovelyMelody>().Grant();
	}

	private void UpdateSnow()
	{
		if (++_snowTick < 60)
			return;

		_snowTick = 0;
		NPC santa = null;
		NPC tax = null;
		for (int i = 0; i < Main.maxNPCs; i++)
		{
			NPC npc = Main.npc[i];
			if (!npc.active || npc.homeless || !npc.townNPC)
				continue;

			if (npc.type == NPCID.SantaClaus)
				santa = npc;
			else if (npc.type == NPCID.TaxCollector)
				tax = npc;
		}

		if (santa == null || tax == null)
			return;

		if (Vector2.Distance(santa.Center, tax.Center) > 30f * 16f)
			return;

		if (BatchLogic.InSnow(santa) && BatchLogic.InSnow(tax))
			ModContent.GetInstance<Scrooged>().Grant();
	}

	private void TryBackstab(float sourceX)
	{
		if (!BatchLogic.Client() || !Player.HasBuff(BuffID.CompanionCube) || !IsDark())
			return;

		if ((sourceX - Player.Center.X) * Player.direction >= 0f)
			return;

		ModContent.GetInstance<SheWouldNever>().Grant();
	}

	private bool IsDark()
	{
		int tileX = (int)(Player.Center.X / 16f);
		int tileY = (int)(Player.Center.Y / 16f);
		int sum = 0;
		int samples = 0;
		for (int x = tileX - 1; x <= tileX + 1; x++)
		{
			for (int y = tileY - 1; y <= tileY + 1; y++)
			{
				Color color = Lighting.GetColor(x, y);
				sum += color.R + color.G + color.B;
				samples++;
			}
		}

		return samples > 0 && sum / samples < 30;
	}
}

public class BatchNPC : GlobalNPC
{
	public override bool InstancePerEntity => true;

	public bool SpawnedInHell;
	public bool PlayerHit;
	public bool TownHit;
	public bool FishBlow;
	public int DukeSide;
	private bool _started;

	private void Start(NPC npc)
	{
		_started = true;
		if (BatchLogic.IsHellSpawn(npc.type) && BatchLogic.InHell(npc))
			SpawnedInHell = true;

		if (npc.type == NPCID.DukeFishron && BatchLogic.Client())
		{
			Player player = Main.LocalPlayer;
			if (player.ZoneBeach && Vector2.Distance(player.Center, npc.Center) < 1200f)
				DukeSide = player.Center.X < Main.maxTilesX * 8f ? -1 : 1;
		}

		if (BatchLogic.IsFightBoss(npc))
			BatchLogic.OnBossSpawn(npc);
	}

	public override void PostAI(NPC npc)
	{
		if (!BatchLogic.Client())
			return;

		if (!_started)
			Start(npc);

		if (!SpawnedInHell || !BatchLogic.InSpace(npc))
			return;

		SpawnedInHell = false;
		ModContent.GetInstance<SixFeetAboveground>().Grant();
	}

	public override void OnHitNPC(NPC npc, NPC target, NPC.HitInfo hit)
	{
		if (!BatchLogic.Client() || hit.Damage <= 0)
			return;

		if (npc.townNPC)
			BatchLogic.NoteTownHit(target, hit.Damage);
		else
			BatchLogic.NoteOtherHit(target, hit.Damage);
	}

	public void HandleKill(NPC npc)
	{
		if (!BatchLogic.Client())
			return;

		if (BatchLogic.IsMonster(npc) && !PlayerHit && TownHit)
			ModContent.GetInstance<SelfDefenseForDummies>().Grant();

		if (BatchLogic.IsMonster(npc) && !OnScreen(npc))
			ModContent.GetInstance<CartoonVillain>().Grant();

		if ((npc.type == NPCID.DukeFishron || npc.type == NPCID.BloodNautilus) && FishBlow)
			ModContent.GetInstance<NoFishyBusiness>().Grant();

		if (npc.type == NPCID.DukeFishron && DukeSide != 0 && Main.LocalPlayer.ZoneBeach)
		{
			int side = Main.LocalPlayer.Center.X < Main.maxTilesX * 8f ? -1 : 1;
			if (side != DukeSide)
				ModContent.GetInstance<Relocation>().Grant();
		}

		if (npc.type == NPCID.HallowBoss && Main.LocalPlayer.GetModPlayer<BatchPlayer>().EmpressReady)
		{
			ModContent.GetInstance<RgbNoHitter>().Grant();
			Main.LocalPlayer.GetModPlayer<BatchPlayer>().ClearEmpress();
		}

		if (BatchLogic.IsPrimary(npc.type) && BatchLogic.GuardianChasing())
			ModContent.GetInstance<VeryBadTime>().Grant();

		BatchLogic.NoteTownDead(npc);
		BatchLogic.OnBossKilled(npc);
	}

	private static bool OnScreen(NPC npc)
	{
		var screen = new Rectangle((int)Main.screenPosition.X, (int)Main.screenPosition.Y, Main.screenWidth, Main.screenHeight);
		return screen.Contains((int)npc.Center.X, (int)npc.Center.Y);
	}
}

public class BatchProjectile : GlobalProjectile
{
	public override bool InstancePerEntity => true;

	public int Weapon;
	public bool FromTown;
	public int BoulderKills;

	public override void OnSpawn(Projectile projectile, IEntitySource source)
	{
		Weapon = 0;
		FromTown = false;
		BoulderKills = 0;

		if (source is EntitySource_ItemUse itemUse)
			Weapon = itemUse.Item.type;
		else if (source is EntitySource_Parent parent)
		{
			if (parent.Entity is Projectile parentProjectile)
			{
				BatchProjectile tracked = parentProjectile.GetGlobalProjectile<BatchProjectile>();
				Weapon = tracked.Weapon;
				FromTown = tracked.FromTown;
			}
			else if (parent.Entity is NPC npc && npc.townNPC)
				FromTown = true;
		}

		if (!BatchLogic.Client() || projectile.owner != Main.myPlayer)
			return;

		if (projectile.friendly && projectile.damage > 0 && !projectile.minion && !projectile.sentry)
			Main.LocalPlayer.GetModPlayer<BatchPlayer>().Fired = true;
	}

	public override void PostAI(Projectile projectile)
	{
		BatchLogic.NoteSpray(projectile);
	}

	public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
	{
		if (!BatchLogic.Client() || damageDone <= 0)
			return;

		if (IsBoulder(projectile.type) && target.life <= 0 && BatchLogic.IsMonster(target))
		{
			BoulderKills++;
			if (BoulderKills >= 5)
				ModContent.GetInstance<Bowling>().Grant();
		}

		if (FromTown)
			BatchLogic.NoteTownHit(target, damageDone);
		else if (projectile.owner == Main.myPlayer)
			BatchLogic.NotePlayerHit(target, Weapon > 0 ? Weapon : Main.LocalPlayer.HeldItem.type, damageDone);
		else
			BatchLogic.NoteOtherHit(target, damageDone);

		if (projectile.owner == Main.myPlayer)
			BatchLogic.NoteTownHurt(target);
	}

	private static bool IsBoulder(int type)
	{
		return type == ProjectileID.Boulder || type == ProjectileID.BouncyBoulder || type == ProjectileID.MiniBoulder;
	}
}

public static class BatchLogic
{
	public const int King = 1;
	public const int Eye = 2;
	public const int Eater = 3;
	public const int Brain = 4;
	public const int Bee = 5;
	public const int Skeletron = 6;
	public const int Deer = 7;
	public const int Wall = 8;
	public const int QueenSlime = 9;
	public const int Destroyer = 10;
	public const int Twins = 11;
	public const int Prime = 12;
	public const int Plantera = 13;
	public const int Golem = 14;
	public const int Duke = 15;
	public const int Empress = 16;
	public const int Cultist = 17;
	public const int Moon = 18;
	public const int Betsy = 19;
	public const int Ogre = 20;
	public const int Dread = 21;
	public const int Pumpking = 22;
	public const int IceQueen = 23;
	public const int Wood = 24;
	public const int Tree = 25;
	public const int Dutchman = 26;
	public const int Saucer = 27;
	public const int Santa = 28;

	private static readonly Dictionary<int, int> Loot = BuildLoot();
	private static readonly HashSet<int> Starve = new HashSet<int>
	{
		ItemID.BatBat, ItemID.TentacleSpike, ItemID.LucyTheAxe, ItemID.WeatherPain,
		ItemID.PewMaticHorn, ItemID.HoundiusShootius, ItemID.HamBat, ItemID.AbigailsFlower
	};
	private static readonly HashSet<int> Fish = new HashSet<int>
	{
		ItemID.Swordfish, ItemID.PurpleClubberfish, ItemID.ReaverShark, ItemID.SawtoothShark,
		ItemID.Rockfish, ItemID.ObsidianSwordfish, ItemID.Tsunami, ItemID.Flairon,
		ItemID.BubbleGun, ItemID.RazorbladeTyphoon, ItemID.TempestStaff
	};
	private static readonly HashSet<ushort> CrimsonTiles = new HashSet<ushort>
	{
		TileID.Crimstone, TileID.CrimsonGrass, TileID.CrimsonJungleGrass, TileID.FleshIce,
		TileID.Crimsand, TileID.CrimsonSandstone, TileID.CrimsonHardenedSand, TileID.CrimsonVines,
		TileID.CrimsonPlants, TileID.CrimsonThorns
	};
	private static readonly HashSet<ushort> CorruptTiles = new HashSet<ushort>
	{
		TileID.Ebonstone, TileID.CorruptGrass, TileID.CorruptJungleGrass, TileID.CorruptIce,
		TileID.Ebonsand, TileID.CorruptSandstone, TileID.CorruptHardenedSand, TileID.CorruptVines,
		TileID.CorruptPlants, TileID.CorruptThorns
	};
	private static readonly HashSet<ushort> SnowTiles = new HashSet<ushort>
	{
		TileID.SnowBlock, TileID.IceBlock, TileID.SnowBrick, TileID.IceBrick, TileID.CorruptIce,
		TileID.FleshIce, TileID.HallowedIce, TileID.BreakableIce, TileID.SnowCloud
	};

	private static readonly Dictionary<int, Fight> Ledger = new Dictionary<int, Fight>();
	private static readonly List<TownMark> Office = new List<TownMark>();
	private static readonly HashSet<long> Evil = new HashSet<long>();
	private static int _officeFamily;
	private static int _bossGone;
	private static int _lastSpray = -1000;

	public static int EvilCount => Evil.Count;

	public static bool Client()
	{
		return !Main.gameMenu && Main.netMode != NetmodeID.Server && Main.myPlayer >= 0 && Main.LocalPlayer.active;
	}

	public static void Reset()
	{
		Ledger.Clear();
		Office.Clear();
		Evil.Clear();
		_officeFamily = 0;
		_bossGone = 0;
		_lastSpray = -1000;
	}

	public static bool InHell(Entity entity)
	{
		return entity.Center.Y / 16f > Main.UnderworldLayer;
	}

	public static bool InSpace(Entity entity)
	{
		return entity.Center.Y / 16f < Main.worldSurface * 0.35;
	}

	public static bool IsHellSpawn(int type)
	{
		return type == NPCID.Demon || type == NPCID.VoodooDemon || type == NPCID.LavaSlime || type == NPCID.Hellbat
			|| type == NPCID.BoneSerpentHead || type == NPCID.FireImp || type == NPCID.RedDevil;
	}

	public static bool IsMonster(NPC npc)
	{
		return npc.active && npc.lifeMax > 5 && !npc.friendly && !npc.townNPC && npc.type != NPCID.TargetDummy;
	}

	public static bool IsFightBoss(NPC npc)
	{
		return npc.boss && npc.type != NPCID.DungeonGuardian;
	}

	public static bool IsPrimary(int type)
	{
		return type == NPCID.KingSlime || type == NPCID.EyeofCthulhu || type == NPCID.EaterofWorldsHead
			|| type == NPCID.BrainofCthulhu || type == NPCID.QueenBee || type == NPCID.SkeletronHead
			|| type == NPCID.Deerclops || type == NPCID.WallofFlesh || type == NPCID.QueenSlimeBoss
			|| type == NPCID.TheDestroyer || type == NPCID.Retinazer || type == NPCID.Spazmatism
			|| type == NPCID.SkeletronPrime || type == NPCID.Plantera || type == NPCID.GolemHead
			|| type == NPCID.DukeFishron || type == NPCID.HallowBoss || type == NPCID.CultistBoss
			|| type == NPCID.MoonLordCore || type == NPCID.DD2Betsy || type == NPCID.DD2OgreT2
			|| type == NPCID.DD2OgreT3 || type == NPCID.BloodNautilus || type == NPCID.Pumpking
			|| type == NPCID.IceQueen || type == NPCID.MourningWood || type == NPCID.Everscream
			|| type == NPCID.PirateShip || type == NPCID.MartianSaucerCore || type == NPCID.SantaNK1;
	}

	public static int Family(int type)
	{
		if (type == NPCID.KingSlime)
			return King;
		if (type == NPCID.EyeofCthulhu)
			return Eye;
		if (type == NPCID.EaterofWorldsHead || type == NPCID.EaterofWorldsBody || type == NPCID.EaterofWorldsTail)
			return Eater;
		if (type == NPCID.BrainofCthulhu || type == NPCID.Creeper)
			return Brain;
		if (type == NPCID.QueenBee)
			return Bee;
		if (type == NPCID.SkeletronHead || type == NPCID.SkeletronHand)
			return Skeletron;
		if (type == NPCID.Deerclops)
			return Deer;
		if (type == NPCID.WallofFlesh || type == NPCID.WallofFleshEye)
			return Wall;
		if (type == NPCID.QueenSlimeBoss)
			return QueenSlime;
		if (type == NPCID.TheDestroyer || type == NPCID.TheDestroyerBody || type == NPCID.TheDestroyerTail)
			return Destroyer;
		if (type == NPCID.Retinazer || type == NPCID.Spazmatism)
			return Twins;
		if (type == NPCID.SkeletronPrime || type == NPCID.PrimeCannon || type == NPCID.PrimeSaw || type == NPCID.PrimeVice || type == NPCID.PrimeLaser)
			return Prime;
		if (type == NPCID.Plantera || type == NPCID.PlanterasTentacle)
			return Plantera;
		if (type == NPCID.Golem || type == NPCID.GolemHead || type == NPCID.GolemFistLeft || type == NPCID.GolemFistRight)
			return Golem;
		if (type == NPCID.DukeFishron)
			return Duke;
		if (type == NPCID.HallowBoss)
			return Empress;
		if (type == NPCID.CultistBoss)
			return Cultist;
		if (type == NPCID.MoonLordCore || type == NPCID.MoonLordHand || type == NPCID.MoonLordHead || type == NPCID.MoonLordFreeEye)
			return Moon;
		if (type == NPCID.DD2Betsy)
			return Betsy;
		if (type == NPCID.DD2OgreT2 || type == NPCID.DD2OgreT3)
			return Ogre;
		if (type == NPCID.BloodNautilus)
			return Dread;
		if (type == NPCID.Pumpking)
			return Pumpking;
		if (type == NPCID.IceQueen)
			return IceQueen;
		if (type == NPCID.MourningWood)
			return Wood;
		if (type == NPCID.Everscream)
			return Tree;
		if (type == NPCID.PirateShip || type == NPCID.PirateShipCannon)
			return Dutchman;
		if (type == NPCID.MartianSaucerCore || type == NPCID.MartianSaucer || type == NPCID.MartianSaucerCannon || type == NPCID.MartianSaucerTurret)
			return Saucer;
		if (type == NPCID.SantaNK1)
			return Santa;

		return 0;
	}

	public static bool GuardianChasing()
	{
		Player player = Main.LocalPlayer;
		for (int i = 0; i < Main.maxNPCs; i++)
		{
			NPC npc = Main.npc[i];
			if (!npc.active || npc.type != NPCID.DungeonGuardian || npc.target != player.whoAmI)
				continue;

			if (Vector2.Distance(npc.Center, player.Center) < 4000f)
				return true;
		}

		return false;
	}

	public static bool InSnow(NPC npc)
	{
		int originX = (int)(npc.Center.X / 16f);
		int originY = (int)(npc.Center.Y / 16f);
		int snow = 0;
		for (int x = originX - 40; x <= originX + 40; x += 2)
		{
			for (int y = originY - 25; y <= originY + 25; y += 2)
			{
				if (!WorldGen.InWorld(x, y, 10))
					continue;

				Tile tile = Main.tile[x, y];
				if (tile.HasTile && SnowTiles.Contains(tile.TileType))
					snow++;
			}
		}

		return snow >= 40;
	}

	public static void NotePlayerHit(NPC target, int weapon, int damage)
	{
		if (!Client() || damage <= 0)
			return;

		BatchNPC tracked = target.GetGlobalNPC<BatchNPC>();
		tracked.PlayerHit = true;
		tracked.FishBlow = Fish.Contains(weapon);

		int family = Family(target.type);
		if (family == 0)
			return;

		Fight fight = FightOf(family);
		fight.Hits++;
		fight.VillageSpoiled = true;
		if (!IsBorrowed(weapon, family))
			fight.BorrowedSpoiled = true;

		if (family == Deer)
		{
			fight.StarveHits++;
			if (!Starve.Contains(weapon))
				fight.StarveSpoiled = true;
		}
	}

	public static void NoteTownHit(NPC target, int damage)
	{
		if (!Client() || damage <= 0)
			return;

		BatchNPC tracked = target.GetGlobalNPC<BatchNPC>();
		tracked.FishBlow = false;
		if (target.life <= 0)
			tracked.TownHit = true;

		int family = Family(target.type);
		if (family == 0)
			return;

		Fight fight = FightOf(family);
		fight.VillageHits++;
		fight.BorrowedSpoiled = true;
		fight.StarveSpoiled = true;
	}

	public static void NoteOtherHit(NPC target, int damage)
	{
		if (!Client() || damage <= 0)
			return;

		BatchNPC tracked = target.GetGlobalNPC<BatchNPC>();
		tracked.FishBlow = false;

		int family = Family(target.type);
		if (family == 0)
			return;

		Fight fight = FightOf(family);
		fight.BorrowedSpoiled = true;
		fight.VillageSpoiled = true;
		fight.StarveSpoiled = true;
	}

	public static void OnBossSpawn(NPC npc)
	{
		if (!Client() || OtherFightBoss(npc))
			return;

		int family = Family(npc.type);
		if (family != 0 && family == _officeFamily && Office.Count > 0)
			return;

		_officeFamily = family;
		TakeSnapshot();
	}

	public static void OnBossKilled(NPC npc)
	{
		if (!IsPrimary(npc.type))
			return;

		int family = Family(npc.type);
		if (Ledger.TryGetValue(family, out Fight fight))
		{
			if (fight.Hits > 0 && !fight.BorrowedSpoiled)
				ModContent.GetInstance<BorrowedArsenal>().Grant();

			if (family == Deer && fight.StarveHits > 0 && !fight.StarveSpoiled)
				ModContent.GetInstance<CantStarve>().Grant();

			if (fight.VillageHits > 0 && !fight.VillageSpoiled)
				ModContent.GetInstance<ItTakesAVillage>().Grant();

			Ledger.Remove(family);
		}

		if (Office.Count >= 3 && !OfficeHurt() && OfficeDead())
			ModContent.GetInstance<FirefistOffice>().Grant();
	}

	public static void TickOffice()
	{
		if (AnyFightBoss())
		{
			_bossGone = 0;
			return;
		}

		_bossGone++;
		if (_bossGone == 60)
		{
			Office.Clear();
			_officeFamily = 0;
		}
	}

	public static void NoteTownDead(NPC npc)
	{
		for (int i = 0; i < Office.Count; i++)
		{
			TownMark mark = Office[i];
			if (mark.Who == npc.whoAmI && mark.Type == npc.type)
				mark.Dead = true;
		}
	}

	public static void NoteTownHurt(NPC npc)
	{
		for (int i = 0; i < Office.Count; i++)
		{
			TownMark mark = Office[i];
			if (mark.Who == npc.whoAmI && mark.Type == npc.type)
				mark.Hurt = true;
		}
	}

	public static void TickSpray()
	{
		if (_lastSpray >= 0 && (int)Main.GameUpdateCount - _lastSpray > 90 && Evil.Count < 200)
			Evil.Clear();
	}

	public static void NoteSpray(Projectile projectile)
	{
		if (!Client())
			return;

		bool crimson;
		if (projectile.type == ProjectileID.CorruptSpray)
			crimson = true;
		else if (projectile.type == ProjectileID.CrimsonSpray)
			crimson = false;
		else
			return;

		int now = (int)Main.GameUpdateCount;
		if (now - _lastSpray > 90)
			Evil.Clear();

		_lastSpray = now;
		int tileX = (int)(projectile.Center.X / 16f);
		int tileY = (int)(projectile.Center.Y / 16f);
		for (int x = tileX - 2; x <= tileX + 2; x++)
		{
			for (int y = tileY - 2; y <= tileY + 2; y++)
			{
				if (!WorldGen.InWorld(x, y, 10))
					continue;

				Tile tile = Main.tile[x, y];
				if (!tile.HasTile)
					continue;

				if (crimson ? CrimsonTiles.Contains(tile.TileType) : CorruptTiles.Contains(tile.TileType))
					Evil.Add(((long)x << 32) | (uint)y);
			}
		}
	}

	private static void TakeSnapshot()
	{
		Office.Clear();
		Player player = Main.LocalPlayer;
		for (int i = 0; i < Main.maxNPCs; i++)
		{
			NPC npc = Main.npc[i];
			if (!npc.active || !npc.townNPC || npc.homeless || IsTownPet(npc))
				continue;

			if (Vector2.Distance(player.Center, npc.Center) > 250f * 16f)
				continue;

			Office.Add(new TownMark { Who = npc.whoAmI, Type = npc.type });
		}
	}

	private static bool OfficeDead()
	{
		for (int i = 0; i < Office.Count; i++)
		{
			if (!Office[i].Dead)
				return false;
		}

		return true;
	}

	private static bool OfficeHurt()
	{
		for (int i = 0; i < Office.Count; i++)
		{
			if (Office[i].Hurt)
				return true;
		}

		return false;
	}

	private static bool OtherFightBoss(NPC self)
	{
		for (int i = 0; i < Main.maxNPCs; i++)
		{
			NPC npc = Main.npc[i];
			if (npc.active && npc.whoAmI != self.whoAmI && IsFightBoss(npc))
				return true;
		}

		return false;
	}

	private static bool AnyFightBoss()
	{
		for (int i = 0; i < Main.maxNPCs; i++)
		{
			NPC npc = Main.npc[i];
			if (npc.active && IsFightBoss(npc))
				return true;
		}

		return false;
	}

	private static bool IsTownPet(NPC npc)
	{
		return (uint)npc.type < (uint)NPCID.Sets.IsTownPet.Length && NPCID.Sets.IsTownPet[npc.type];
	}

	private static Fight FightOf(int family)
	{
		if (!Ledger.TryGetValue(family, out Fight fight))
		{
			fight = new Fight();
			Ledger[family] = fight;
		}

		return fight;
	}

	private static bool IsBorrowed(int weapon, int family)
	{
		return weapon > 0 && Loot.TryGetValue(weapon, out int source) && source != family;
	}

	private static Dictionary<int, int> BuildLoot()
	{
		var loot = new Dictionary<int, int>();
		Add(loot, King, ItemID.SlimeGun);
		Add(loot, Bee, ItemID.BeeGun, ItemID.BeeKeeper, ItemID.BeesKnees, ItemID.Beenade);
		Add(loot, Skeletron, ItemID.BookofSkulls, ItemID.BoneGlove);
		Add(loot, Deer, ItemID.LucyTheAxe, ItemID.WeatherPain, ItemID.PewMaticHorn, ItemID.HoundiusShootius);
		Add(loot, Wall, ItemID.Pwnhammer, ItemID.BreakerBlade, ItemID.ClockworkAssaultRifle, ItemID.LaserRifle, ItemID.FireWhip);
		Add(loot, QueenSlime, ItemID.Smolstar);
		Add(loot, Plantera, ItemID.GrenadeLauncher, ItemID.VenusMagnum, ItemID.NettleBurst, ItemID.LeafBlower, ItemID.FlowerPow, ItemID.WaspGun, ItemID.Seedler, ItemID.TheAxe, ItemID.PygmyStaff);
		Add(loot, Golem, ItemID.GolemFist, ItemID.PossessedHatchet, ItemID.Stynger, ItemID.HeatRay, ItemID.StaffofEarth, ItemID.Picksaw);
		Add(loot, Duke, ItemID.Flairon, ItemID.Tsunami, ItemID.BubbleGun, ItemID.RazorbladeTyphoon, ItemID.TempestStaff);
		Add(loot, Empress, ItemID.FairyQueenMagicItem, ItemID.FairyQueenRangedItem, ItemID.RainbowWhip, ItemID.PiercingStarlight, ItemID.EmpressBlade);
		Add(loot, Moon, ItemID.Meowmere, ItemID.Terrarian, ItemID.StarWrath, ItemID.SDMG, ItemID.LastPrism, ItemID.LunarFlareBook, ItemID.RainbowCrystalStaff, ItemID.MoonlordTurretStaff, ItemID.PortalGun);
		Add(loot, Betsy, ItemID.DD2BetsyBow, ItemID.MonkStaffT3);
		Add(loot, Ogre, ItemID.BookStaff, ItemID.DD2PhoenixBow, ItemID.DD2SquireDemonSword, ItemID.MonkStaffT1, ItemID.MonkStaffT2);
		Add(loot, Dread, ItemID.SanguineStaff);
		Add(loot, Pumpking, ItemID.TheHorsemansBlade, ItemID.BatScepter, ItemID.RavenStaff, ItemID.CandyCornRifle, ItemID.JackOLanternLauncher);
		Add(loot, IceQueen, ItemID.BlizzardStaff, ItemID.NorthPole, ItemID.SnowmanCannon);
		Add(loot, Wood, ItemID.StakeLauncher);
		Add(loot, Tree, ItemID.ChristmasTreeSword, ItemID.Razorpine);
		Add(loot, Dutchman, ItemID.CoinGun, ItemID.PirateStaff);
		Add(loot, Saucer, ItemID.Xenopopper, ItemID.XenoStaff, ItemID.LaserMachinegun, ItemID.ElectrosphereLauncher, ItemID.InfluxWaver, ItemID.ChargedBlasterCannon);
		Add(loot, Santa, ItemID.ChainGun, ItemID.ElfMelter);
		return loot;
	}

	private static void Add(Dictionary<int, int> loot, int family, params int[] items)
	{
		for (int i = 0; i < items.Length; i++)
			loot[items[i]] = family;
	}

	private class Fight
	{
		public int Hits;
		public bool BorrowedSpoiled;
		public int VillageHits;
		public bool VillageSpoiled;
		public int StarveHits;
		public bool StarveSpoiled;
	}

	private class TownMark
	{
		public int Who;
		public int Type;
		public bool Dead;
		public bool Hurt;
	}
}
