using ReimaginingAchievements.Content.Achievements;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ReimaginingAchievements.Content.Tracking;

public class FloorIsLavaGlobalNPC : GlobalNPC
{
	public static bool Killed;

	public override void OnKill(NPC npc)
	{
		if (npc.type == NPCID.MoonLordCore)
			Killed = true;
	}
}

public class FloorIsLavaPlayer : ModPlayer
{
	public bool Tracking;
	public bool Failed;

	public override void PostUpdate()
	{
		if (Player.whoAmI != Main.myPlayer)
			return;

		bool alive = NPC.AnyNPCs(NPCID.MoonLordCore);
		if (alive)
		{
			if (!Tracking)
			{
				Tracking = true;
				Failed = Player.dead || TouchingSupport();
				FloorIsLavaGlobalNPC.Killed = false;
			}
			else if (Player.dead || TouchingSupport())
			{
				Failed = true;
			}

			return;
		}

		if (!Tracking)
			return;

		if (FloorIsLavaGlobalNPC.Killed && !Failed)
			ModContent.GetInstance<FloorIsLava>().Condition.Complete();

		Tracking = false;
		Failed = false;
		FloorIsLavaGlobalNPC.Killed = false;
	}

	private bool TouchingSupport()
	{
		if (Player.grappling[0] >= 0 || Player.pulley || Player.sitting.isSitting)
			return true;

		if (Player.sleeping.isSleeping)
			return true;

		if (Player.mount.Active && Player.mount.Cart)
			return true;

		if (Collision.SolidCollision(Player.position, Player.width, Player.height))
			return true;

		if (Collision.SolidCollision(Player.position + new Microsoft.Xna.Framework.Vector2(0f, Player.height - 2f), Player.width, 8))
			return true;

		int left = (int)(Player.position.X / 16f);
		int right = (int)((Player.position.X + Player.width) / 16f);
		int feet = (int)((Player.position.Y + Player.height + 4f) / 16f);
		for (int x = left; x <= right; x++)
		{
			Tile tile = Framing.GetTileSafely(x, feet);
			if (!tile.HasTile)
				continue;

			if (TileID.Sets.Platforms[tile.TileType] || (Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType]))
				return true;
		}

		return false;
	}
}
