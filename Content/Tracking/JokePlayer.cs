using Microsoft.Xna.Framework;
using ReimaginingAchievements.Content.Achievements;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ReimaginingAchievements.Content.Tracking;

public class JokePlayer : ModPlayer
{
	private const int RecallWindow = 120;
	private const int PlatinumCopper = 1000000;

	public int NurseHeals;
	public int CoinsToday;
	public int DynamiteBought;
	private int _recall;
	private int _lastTax = -1;
	private bool _wasDay;
	private bool _falling;

	public int PlatinumToday => CoinsToday / PlatinumCopper;

	public override void OnEnterWorld()
	{
		_lastTax = -1;
		_recall = 0;
		_falling = false;
		_wasDay = Main.dayTime;
		NurseHeals = 0;
		CoinsToday = 0;
		DynamiteBought = 0;
	}

	public override void PostUpdate()
	{
		if (Player.whoAmI != Main.myPlayer)
			return;

		if (!_wasDay && Main.dayTime)
		{
			NurseHeals = 0;
			CoinsToday = 0;
		}

		_wasDay = Main.dayTime;
		if (_recall > 0)
			_recall--;

		TrackTax();
		TrackFall();
		if (Player.talkNPC < 0 || Main.npc[Player.talkNPC].type != NPCID.Demolitionist)
			DynamiteBought = 0;
	}

	public override void Kill(double damage, int hitDirection, bool pvp, Terraria.DataStructures.PlayerDeathReason damageSource)
	{
		if (Player.whoAmI != Main.myPlayer || _recall <= 0 || Main.GameMode == GameModeID.Creative)
			return;

		ModContent.GetInstance<MoneyIsSafe>().Grant();
		_recall = 0;
	}

	public override void PostNurseHeal(NPC nurse, int health, bool removeDebuffs, int price)
	{
		if (Player.whoAmI != Main.myPlayer)
			return;

		NurseHeals++;
		if (NurseHeals >= 10)
			ModContent.GetInstance<SufferingStronger>().Grant();
	}

	public override void PostBuyItem(NPC vendor, Item[] shopInventory, Item item)
	{
		if (Player.whoAmI != Main.myPlayer || vendor.type != NPCID.Demolitionist || item.type != ItemID.Dynamite)
			return;

		int stack = item.stack > 0 ? item.stack : 1;
		DynamiteBought += stack;
		if (DynamiteBought >= 500)
			ModContent.GetInstance<BoomBoomBoom>().Grant();
	}

	public void AddCoins(int copper)
	{
		if (copper <= 0)
			return;

		CoinsToday += copper;
		if (CoinsToday >= PlatinumCopper * 5)
			ModContent.GetInstance<RichGetRicher>().Grant();
	}

	public void ArmRecall()
	{
		_recall = RecallWindow;
	}

	public void RegisterBullet()
	{
		BulletWindow.Register();
	}

	private void TrackTax()
	{
		int tax = Player.taxMoney;
		if (_lastTax < 0)
		{
			_lastTax = tax;
			return;
		}

		if (tax < _lastTax)
			ModContent.GetInstance<Scrooge>().AddCopper(_lastTax - tax);

		_lastTax = tax;
	}

	private void TrackFall()
	{
		if (Player.dead || TouchingSupport())
		{
			_falling = false;
			return;
		}

		if (_falling)
		{
			if (Player.velocity.Y < -0.05f)
			{
				_falling = false;
				return;
			}

			int feet = (int)((Player.position.Y + Player.height) / 16f);
			if (Player.ZoneUnderworldHeight && feet >= Main.maxTilesY - 80)
			{
				_falling = false;
				ModContent.GetInstance<OnlyWayIsDown>().Grant();
			}

			return;
		}

		if (Player.ZoneSkyHeight && Player.velocity.Y > 0.5f)
			_falling = true;
	}

	private bool TouchingSupport()
	{
		if (Player.grappling[0] >= 0 || Player.pulley || Player.sitting.isSitting || Player.sleeping.isSleeping)
			return true;

		if (Player.mount.Active && Player.mount.Cart)
			return true;

		if (Collision.SolidCollision(Player.position, Player.width, Player.height))
			return true;

		if (Collision.SolidCollision(Player.position + new Vector2(0f, Player.height - 2f), Player.width, 8))
			return true;

		return false;
	}
}

public static class BulletWindow
{
	private const int Window = 30;
	private const int Needed = 20;
	private static readonly int[] Frames = new int[Needed];
	private static int _count;

	public static void Register()
	{
		int now = (int)Main.GameUpdateCount;
		if (_count == Frames.Length)
		{
			for (int i = 1; i < Frames.Length; i++)
				Frames[i - 1] = Frames[i];

			_count--;
		}

		Frames[_count++] = now;
		int alive = 0;
		for (int i = 0; i < _count; i++)
		{
			if (now - Frames[i] < Window)
				alive++;
		}

		if (alive >= Needed)
			ModContent.GetInstance<BulletStorm>().Grant();
	}
}
