using Terraria.Achievements;
using Terraria.ModLoader;

namespace ReimaginingAchievements.Common;

public interface IAchievementSource
{
	string Source { get; }
}

public interface IRequirementList
{
	System.Collections.Generic.IEnumerable<RequirementLine> GetRequirements();
}

public readonly struct RequirementLine
{
	public readonly string Text;
	public readonly bool Complete;

	public RequirementLine(string text, bool complete)
	{
		Text = text;
		Complete = complete;
	}
}

public static class AchievementSources
{
	public const string Verv = "Verv's Achievements";

	public static string Of(Achievement achievement)
	{
		if (achievement?.ModAchievement is IAchievementSource sourced && !string.IsNullOrEmpty(sourced.Source))
			return sourced.Source;

		return achievement?.ModAchievement?.Mod.DisplayNameClean ?? "Terraria";
	}
}
