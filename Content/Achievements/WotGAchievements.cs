using System.Collections.Generic;
using ReimaginingAchievements.Common;
using ReimaginingAchievements.Content.Tracking;
using Terraria.Localization;

namespace ReimaginingAchievements.Content.Achievements;

public class RememberItsBest : GatedAchievement
{
	public override string RequiredMod => "NoxusBoss";
}

public class NamelessMartyr : GatedAchievement, ILiveText, IRequirementList
{
	public override string RequiredMod => "NoxusBoss";

	public string LiveName =>
		StoryWatch.AvatarDown || Achievement.IsCompleted
			? Language.GetTextValue("Mods.ReimaginingAchievements.Live.MartyrName")
			: Language.GetTextValue("Mods.ReimaginingAchievements.Live.HiddenName");

	public string LiveDescription => StageText();

	public IEnumerable<RequirementLine> GetRequirements()
	{
		yield return new RequirementLine(StageText(), Achievement.IsCompleted);
	}

	private string StageText()
	{
		if (Achievement.IsCompleted || StoryWatch.NamelessDown)
			return Language.GetTextValue("Mods.ReimaginingAchievements.Live.MartyrDone");

		if (StoryWatch.AvatarDown)
			return Language.GetTextValue("Mods.ReimaginingAchievements.Live.MartyrTest");

		return Language.GetTextValue("Mods.ReimaginingAchievements.Live.MartyrHidden");
	}
}

public class UponAStar : GatedAchievement
{
	public override string RequiredMod => "NoxusBoss";
}

public class WrathOfTheStars : GatedAchievement
{
	public override string RequiredMod => "NoxusBoss";
}

public class GreatLandlord : GatedAchievement
{
	public override string RequiredMod => "NoxusBoss";
}

public class ToasterPanic : GatedAchievement
{
	public override string RequiredMod => "NoxusBoss";
}

public class PerfectFlower : GatedAchievement
{
	public override string RequiredMod => "NoxusBoss";
}

public class GoodAppleBite : GatedAchievement
{
	public override string RequiredMod => "NoxusBoss";
}

public class ParryNameless : GatedAchievement
{
	public override string RequiredMod => "NoxusBoss";
}

public class WhatDidYouExpect : GatedAchievement
{
	public override string RequiredMod => "NoxusBoss";
}

public class SincerelyForYou : GatedAchievement
{
	public override string RequiredMod => "NoxusBoss";
}
