using ReimaginingAchievements.UI;
using Terraria;
using Terraria.Achievements;
using Terraria.ModLoader;
using Terraria.UI;

namespace ReimaginingAchievements;

public class ReimaginingAchievements : Mod
{
	private static Achievement _pendingGoto;

	public override void Load()
	{
		On_IngameFancyUI.OpenAchievements += OpenAchievements;
		On_IngameFancyUI.OpenAchievementsAndGoto += OpenAchievementsAndGoto;
	}

	public override void Unload()
	{
		_pendingGoto = null;
		ProgressToast.Reset();
	}

	private static void OpenAchievements(On_IngameFancyUI.orig_OpenAchievements orig)
	{
		orig();
		var state = new AchievementsMenuState();
		state.GotoTarget = _pendingGoto;
		Main.InGameUI.SetState(state);
	}

	private static void OpenAchievementsAndGoto(On_IngameFancyUI.orig_OpenAchievementsAndGoto orig, Achievement achievement)
	{
		_pendingGoto = achievement;
		orig(achievement);
		_pendingGoto = null;
	}
}
