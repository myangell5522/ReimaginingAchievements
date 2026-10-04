using ReimaginingAchievements.Content.Achievements;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ReimaginingAchievements.Content.Tracking;

public class ChallengeGlobalItem : GlobalItem
{
	public override bool? UseItem(Item item, Player player)
	{
		if (item.OriginalRarity < 0 || item.OriginalRarity > 3)
		{
			foreach (NPC npc in Main.npc)
			{
				if (npc.active && npc.type == NPCID.CultistBoss)
					npc.GetGlobalNPC<ChallengeGlobalNPC>().FlagLunaticOrange = false;
			}
		}

		if (item.type == ItemID.Teacup && player.sitting.isSitting && player.HasBuff(BuffID.OnFire) && (player.HasBuff(BuffID.Frostburn) || player.HasBuff(BuffID.CursedInferno)))
			ModContent.GetInstance<AchievementThisIsFine>().ThisIsFineCondition.Complete();

		if (item.type == ItemID.RedPotion)
			player.GetModPlayer<ChallengePlayer>().RedPotionTimer = 3600;

		return null;
	}
}
