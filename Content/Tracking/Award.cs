using System.IO;
using ReimaginingAchievements.Content.Achievements;
using ReimaginingAchievements.UI;
using Terraria;
using Terraria.GameContent.Achievements;
using Terraria.ID;
using Terraria.ModLoader;

namespace ReimaginingAchievements.Content.Tracking;

public static class Award
{
	public const byte KillMessage = 0;
	public const byte GrantMessage = 1;

	public static void Grant<T>() where T : ModAchievement
	{
		Grant(typeof(T).Name);
	}

	public static void Grant(string name)
	{
		if (Main.netMode == NetmodeID.Server)
		{
			ModPacket packet = ModContent.GetInstance<global::ReimaginingAchievements.ReimaginingAchievements>().GetPacket();
			packet.Write(GrantMessage);
			packet.Write(name);
			packet.Send();
			return;
		}

		GrantLocal(name);
	}

	public static void SendKill(NPC npc)
	{
		ModPacket packet = ModContent.GetInstance<global::ReimaginingAchievements.ReimaginingAchievements>().GetPacket();
		packet.Write(KillMessage);
		packet.Write((short)npc.whoAmI);
		packet.Write((short)npc.type);
		packet.Send();
	}

	public static void Handle(BinaryReader reader)
	{
		byte message = reader.ReadByte();
		if (message == KillMessage)
		{
			int who = reader.ReadInt16();
			int type = reader.ReadInt16();
			if (Main.netMode == NetmodeID.MultiplayerClient && who >= 0 && who < Main.maxNPCs && Main.npc[who].type == type)
				KillHub.Local(Main.npc[who]);
		}
		else if (message == GrantMessage)
		{
			string name = reader.ReadString();
			if (Main.netMode == NetmodeID.MultiplayerClient)
				GrantLocal(name);
		}
	}

	private static void GrantLocal(string name)
	{
		if (Main.dedServ || Main.gameMenu)
			return;

		if (!ModContent.TryFind("ReimaginingAchievements", name, out ModAchievement achievement))
			return;

		if (achievement is OwnedAchievement owned)
		{
			owned.Grant();
			return;
		}

		if (achievement is GatedAchievement gated)
		{
			gated.Grant();
			return;
		}

		foreach (var condition in RequirementLines.GetConditions(achievement.Achievement))
		{
			if (condition is CustomFlagCondition flag && !flag.IsCompleted)
				flag.Complete();
		}
	}
}
