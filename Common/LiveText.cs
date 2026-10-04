using Terraria.Achievements;

namespace ReimaginingAchievements.Common;

public interface ILiveText
{
	string LiveName { get; }

	string LiveDescription { get; }
}

public interface IModGated
{
	bool ModMissing { get; }

	string RequiredDisplayName { get; }
}

public static class LiveText
{
	public static string Name(Achievement achievement)
	{
		if (achievement == null)
			return "";

		if (achievement.Hidden && !achievement.IsCompleted)
			return "???";

		if (achievement.ModAchievement is ILiveText live && !string.IsNullOrEmpty(live.LiveName))
			return live.LiveName;

		return achievement.FriendlyName.Value;
	}

	public static string Description(Achievement achievement)
	{
		if (achievement == null)
			return "";

		if (achievement.Hidden && !achievement.IsCompleted)
			return "???";

		if (IsModLocked(achievement))
			return "???";

		if (achievement.ModAchievement is ILiveText live && !string.IsNullOrEmpty(live.LiveDescription))
			return live.LiveDescription;

		return achievement.Description.Value;
	}

	public static bool IsModLocked(Achievement achievement)
	{
		return achievement?.ModAchievement is IModGated gated && gated.ModMissing && !achievement.IsCompleted;
	}
}
