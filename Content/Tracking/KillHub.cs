using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ReimaginingAchievements.Content.Tracking;

public class KillRelay : GlobalNPC
{
	public override void OnKill(NPC npc)
	{
		KillHub.World(npc);
		if (Main.netMode == NetmodeID.Server)
			Award.SendKill(npc);
		else
			KillHub.Local(npc);
	}
}

public static class KillHub
{
	public static void World(NPC npc)
	{
		npc.GetGlobalNPC<JokeNPC>().HandleWorldKill(npc);
		ThirdWorld.HandleKill(npc);
	}

	public static void Local(NPC npc)
	{
		if (Main.dedServ || Main.gameMenu)
			return;

		FloorIsLavaGlobalNPC.HandleKill(npc);
		npc.GetGlobalNPC<ChallengeGlobalNPC>().HandleKill(npc);
		npc.GetGlobalNPC<JokeNPC>().HandleLocalKill(npc);
		npc.GetGlobalNPC<BatchNPC>().HandleKill(npc);
		npc.GetGlobalNPC<ThirdNPC>().HandleKill(npc);
	}
}
