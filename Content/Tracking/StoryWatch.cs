using ReimaginingAchievements.Common;
using ReimaginingAchievements.Content.Achievements;
using Terraria;
using Terraria.ModLoader;

namespace ReimaginingAchievements.Content.Tracking;

/// <summary>Polls the supported story mods and grants the matching gated achievements.</summary>
public class StoryWatch : ModSystem
{
	private const int PollInterval = 30;

	private int _timer;

	public static bool AvatarDown { get; private set; }
	public static bool NamelessDown { get; private set; }

	public override void OnWorldLoad()
	{
		_timer = 0;
		AvatarDown = false;
		NamelessDown = false;
	}

	public override void Unload()
	{
		ForeignFlags.Clear();
	}

	public override void PostUpdateEverything()
	{
		if (Main.gameMenu || Main.dedServ)
			return;

		bool wotg = ModLoader.HasMod("NoxusBoss");

		// Cheap per-frame checks: these flags only stay up for a few frames.
		if (wotg)
			WatchWotGMoments();

		if (++_timer < PollInterval)
			return;

		_timer = 0;
		if (wotg)
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

	private static void WatchWotGMoments()
	{
		// MeteorShowerCountdown only runs for ~8 seconds and RoH's animation for about a second.
		if (ForeignFlags.StaticInt("NoxusBoss", "StargazingScene", "MeteorShowerCountdown") > 0
			|| ForeignFlags.StaticBool("NoxusBoss", "StargazingScene", "SolynSpokeAboutMeteorShower"))
			ModContent.GetInstance<WrathOfTheStars>().Grant();

		if (ForeignFlags.StaticInt("NoxusBoss", "RoHDestructionSystem", "AnimationTimer") > 0)
			ModContent.GetInstance<WhatDidYouExpect>().Grant();

		if (ForeignFlags.StaticBool("NoxusBoss", "EmptinessSprayPlayerDeletionSystem", "PlayerWasDeletedByNamelessDeity"))
			ModContent.GetInstance<ParryNameless>().Grant();

		// CreditsState 1 = SitDownForAWhile: the player and Solyn rest under the tree.
		if (ForeignFlags.InstanceInt("NoxusBoss", "EndCreditsScene", "State") >= 1)
			ModContent.GetInstance<SincerelyForYou>().Grant();
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
	}

	private static void WatchCalamity()
	{
		// Calamity's public Mod.Call is the stable API; the property read is only a fallback.
		if (CalamityDowned("supremecalamitas", "downedCalamitas"))
			ModContent.GetInstance<SupremeCalamitas>().Grant();

		if (CalamityDowned("devourerofgods", "downedDoG"))
			ModContent.GetInstance<DevourerOfGods>().Grant();

		if (CalamityDowned("yharon", "downedYharon"))
			ModContent.GetInstance<YharonDragon>().Grant();

		if (CalamityDowned("exomechs", "downedExoMechs"))
			ModContent.GetInstance<ExoMechs>().Grant();
	}

	private static bool CalamityDowned(string callName, string property)
	{
		return ForeignFlags.CallBool("CalamityMod", "GetBossDowned", callName)
			|| ForeignFlags.StaticBool("CalamityMod", "DownedBossSystem", property);
	}

	private static void WatchEntropy()
	{
		// Calamity Entropy stores these as public static fields on EDownedBosses.
		if (ForeignFlags.StaticBool("CalamityEntropy", "EDownedBosses", "downedProphet"))
			ModContent.GetInstance<TheProphet>().Grant();

		if (ForeignFlags.StaticBool("CalamityEntropy", "EDownedBosses", "downedNihilityTwin"))
			ModContent.GetInstance<NihilityTwins>().Grant();

		if (ForeignFlags.StaticBool("CalamityEntropy", "EDownedBosses", "downedCruiser"))
			ModContent.GetInstance<TheCruiser>().Grant();

		if (ForeignFlags.StaticBool("CalamityEntropy", "EDownedBosses", "downedLuminaris"))
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
