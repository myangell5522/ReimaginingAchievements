using ReimaginingAchievements.Common;
using ReimaginingAchievements.Content.Achievements;
using Terraria;
using Terraria.ModLoader;

namespace ReimaginingAchievements.Content.Tracking;

public class StoryWatch : ModSystem
{
	public static bool AvatarDown { get; private set; }
	public static bool NamelessDown { get; private set; }

	public override void PostUpdateEverything()
	{
		if (Main.gameMenu)
			return;

		if (ModLoader.HasMod("NoxusBoss"))
			WatchWotG();

		if (ModLoader.HasMod("CalamityMod"))
			WatchCalamity();

		if (ModLoader.HasMod("CalamityEntropy"))
			WatchEntropy();
	}

	public void TryApple(Item item)
	{
		int type = ForeignFlags.ItemType("NoxusBoss", "GoodApple");
		if (type > 0 && item.type == type)
			ModContent.GetInstance<GoodAppleBite>().Grant();
	}

	private static void WatchWotG()
	{
		AvatarDown = ForeignFlags.CallBool("NoxusBoss", "GetBossDefeated", "avatarofemptiness");
		NamelessDown = ForeignFlags.CallBool("NoxusBoss", "GetBossDefeated", "namelessdeity");
		if (AvatarDown)
			ModContent.GetInstance<RememberItsBest>().Grant();

		if (NamelessDown)
			ModContent.GetInstance<NamelessMartyr>().Grant();

		if (ForeignFlags.CallBool("NoxusBoss", "GetBossDefeated", "mars"))
			ModContent.GetInstance<ToasterPanic>().Grant();

		if (ForeignFlags.InstanceBool("NoxusBoss", "StargazingEvent", "Finished"))
			ModContent.GetInstance<UponAStar>().Grant();

		if (ForeignFlags.StaticBool("NoxusBoss", "SolynCampsiteWorldGen", "CampHasBeenMade"))
			ModContent.GetInstance<GreatLandlord>().Grant();

		if (ForeignFlags.StaticBool("NoxusBoss", "WorldSaveSystem", "HasCompletedGenesis"))
			ModContent.GetInstance<PerfectFlower>().Grant();

		if (ForeignFlags.StaticInt("NoxusBoss", "StargazingScene", "MeteorShowerCountdown") > 0
			|| ForeignFlags.StaticBool("NoxusBoss", "StargazingScene", "SolynSpokeAboutMeteorShower"))
			ModContent.GetInstance<WrathOfTheStars>().Grant();

		if (ForeignFlags.StaticBool("NoxusBoss", "EmptinessSprayPlayerDeletionSystem", "PlayerWasDeletedByNamelessDeity"))
			ModContent.GetInstance<ParryNameless>().Grant();

		if (ForeignFlags.StaticInt("NoxusBoss", "RoHDestructionSystem", "AnimationTimer") > 0)
			ModContent.GetInstance<WhatDidYouExpect>().Grant();

		if (ForeignFlags.InstanceInt("NoxusBoss", "EndCreditsScene", "State") >= 1)
			ModContent.GetInstance<SincerelyForYou>().Grant();
	}

	private static void WatchCalamity()
	{
		if (ForeignFlags.Downed("CalamityMod", "downedCalamitas"))
			ModContent.GetInstance<SupremeCalamitas>().Grant();

		if (ForeignFlags.Downed("CalamityMod", "downedDoG"))
			ModContent.GetInstance<DevourerOfGods>().Grant();

		if (ForeignFlags.Downed("CalamityMod", "downedYharon"))
			ModContent.GetInstance<YharonDragon>().Grant();

		if (ForeignFlags.Downed("CalamityMod", "downedExoMechs"))
			ModContent.GetInstance<ExoMechs>().Grant();
	}

	private static void WatchEntropy()
	{
		if (ForeignFlags.Downed("CalamityEntropy", "downedProphet"))
			ModContent.GetInstance<TheProphet>().Grant();

		if (ForeignFlags.Downed("CalamityEntropy", "downedNihilityTwin"))
			ModContent.GetInstance<NihilityTwins>().Grant();

		if (ForeignFlags.Downed("CalamityEntropy", "downedCruiser"))
			ModContent.GetInstance<TheCruiser>().Grant();

		if (ForeignFlags.Downed("CalamityEntropy", "downedLuminaris"))
			ModContent.GetInstance<Luminaris>().Grant();
	}
}

public class StoryItem : GlobalItem
{
	public override bool? UseItem(Item item, Player player)
	{
		if (player.whoAmI == Main.myPlayer && ModLoader.HasMod("NoxusBoss"))
			ModContent.GetInstance<StoryWatch>().TryApple(item);

		return null;
	}
}
