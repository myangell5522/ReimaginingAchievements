using System.IO;
using ReimaginingAchievements.Content.Tracking;
using ReimaginingAchievements.UI;
using Terraria.Achievements;
using Terraria.GameContent.UI.States;
using Terraria.ModLoader;
using Terraria.UI;

namespace ReimaginingAchievements;

public class ReimaginingAchievements : Mod
{
	private static Achievement _pendingGoto;

	public override void Load()
	{
		On_IngameFancyUI.OpenAchievementsAndGoto += OpenAchievementsAndGoto;
		On_UserInterface.SetState += SetState;
	}

	public override void Unload()
	{
		_pendingGoto = null;
		ProgressToast.Reset();
	}

	public override void HandlePacket(BinaryReader reader, int whoAmI)
	{
		Award.Handle(reader);
	}

	// orig would call GotoAchievement on the vanilla menu, which is never activated anymore.
	private static void OpenAchievementsAndGoto(On_IngameFancyUI.orig_OpenAchievementsAndGoto orig, Achievement achievement)
	{
		_pendingGoto = achievement;
		IngameFancyUI.OpenAchievements();
		_pendingGoto = null;
	}

	private static void SetState(On_UserInterface.orig_SetState orig, UserInterface self, UIState state)
	{
		if (state is UIAchievementsMenu vanilla)
			state = new AchievementsMenuState { GotoTarget = _pendingGoto, PreviousUIState = vanilla.PreviousUIState };

		orig(self, state);
	}
}
