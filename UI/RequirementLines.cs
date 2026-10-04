using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using ReimaginingAchievements.Common;
using Terraria;
using Terraria.Achievements;
using Terraria.GameContent.Achievements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace ReimaginingAchievements.UI;

public static class RequirementLines
{
	private const int MaxLines = 12;

	private static readonly FieldInfo ConditionsField = typeof(Achievement).GetField("_conditions", BindingFlags.Instance | BindingFlags.NonPublic);

	public static List<RequirementLine> Build(Achievement achievement)
	{
		if (achievement == null)
			return new List<RequirementLine>();

		if (achievement.Hidden && !achievement.IsCompleted)
			return new List<RequirementLine> { new RequirementLine("???", false) };

		if (LiveText.IsModLocked(achievement) && achievement.ModAchievement is IModGated gated)
			return new List<RequirementLine> { new RequirementLine(Language.GetTextValue("Mods.ReimaginingAchievements.UI.RequiresMod", gated.RequiredDisplayName), false) };

		if (achievement.ModAchievement is IRequirementList custom)
			return Trim(new List<RequirementLine>(custom.GetRequirements()));

		return Trim(FromConditions(achievement));
	}

	private static List<RequirementLine> Trim(List<RequirementLine> lines)
	{
		if (lines.Count <= MaxLines)
			return lines;

		var ordered = new List<RequirementLine>(lines.Count);
		foreach (RequirementLine line in lines)
		{
			if (!line.Complete)
				ordered.Add(line);
		}

		foreach (RequirementLine line in lines)
		{
			if (line.Complete)
				ordered.Add(line);
		}

		var trimmed = ordered.GetRange(0, MaxLines);
		string more = Language.GetTextValue("Mods.ReimaginingAchievements.UI.MoreLines", lines.Count - MaxLines);
		trimmed.Add(new RequirementLine(more, false));
		return trimmed;
	}

	public static string Describe(Achievement achievement, AchievementCondition condition)
	{
		if (achievement != null && achievement.Hidden && !achievement.IsCompleted)
			return "???";

		string key = "Mods.ReimaginingAchievements.Conditions." + condition.Name;
		if (Language.Exists(key))
			return Language.GetTextValue(key);

		string named = NameFromCondition(condition);
		if (!string.IsNullOrEmpty(named))
			return named;

		var conditions = GetConditions(achievement);
		if (conditions.Count <= 1 && achievement != null)
			return achievement.Description.Value;

		return Prettify(condition.Name);
	}

	public static List<AchievementCondition> GetConditions(Achievement achievement)
	{
		if (achievement != null && ConditionsField?.GetValue(achievement) is Dictionary<string, AchievementCondition> dictionary)
			return new List<AchievementCondition>(dictionary.Values);

		return new List<AchievementCondition>();
	}

	public static bool TryGetProgress(AchievementCondition condition, out int value, out int max)
	{
		value = 0;
		max = 0;
		IAchievementTracker tracker = condition?.GetAchievementTracker();
		if (tracker == null)
			return false;

		PropertyInfo valueProperty = tracker.GetType().GetProperty("Value");
		PropertyInfo maxProperty = tracker.GetType().GetProperty("MaxValue");
		if (valueProperty == null || maxProperty == null)
			return false;

		object rawValue = valueProperty.GetValue(tracker);
		object rawMax = maxProperty.GetValue(tracker);
		if (rawValue is int intValue && rawMax is int intMax)
		{
			value = intValue;
			max = intMax;
			return max > 1;
		}

		if (rawValue is float floatValue && rawMax is float floatMax)
		{
			value = (int)floatValue;
			max = (int)floatMax;
			return max > 1;
		}

		return false;
	}

	private static List<RequirementLine> FromConditions(Achievement achievement)
	{
		List<AchievementCondition> conditions = GetConditions(achievement);
		var lines = new List<RequirementLine>();
		if (conditions.Count == 0)
		{
			lines.Add(new RequirementLine(achievement.Description.Value, achievement.IsCompleted));
			return lines;
		}

		if (conditions.Count == 1 && !TryGetProgress(conditions[0], out _, out _))
		{
			lines.Add(new RequirementLine(achievement.Description.Value, conditions[0].IsCompleted || achievement.IsCompleted));
			return lines;
		}

		foreach (AchievementCondition condition in conditions)
		{
			string text = Describe(achievement, condition);
			if (TryGetProgress(condition, out int value, out int max))
				text = text + " " + value + "/" + max;

			lines.Add(new RequirementLine(text, condition.IsCompleted || achievement.IsCompleted));
		}

		return lines;
	}

	private static string NameFromCondition(AchievementCondition condition)
	{
		if (condition is ItemPickupCondition or ItemCraftCondition)
		{
			short[] items = Shorts(condition, "_itemIds");
			if (items.Length > 0)
				return Lang.GetItemNameValue(items[0]);
		}

		if (condition is NPCKilledCondition)
		{
			short[] npcs = Shorts(condition, "_npcIds", "_npcIDs");
			if (npcs.Length > 0)
				return Lang.GetNPCNameValue(npcs[0]);
		}

		if (condition is TileDestroyedCondition)
		{
			short[] tiles = Shorts(condition, "_tileIds", "_tileIDs");
			if (tiles.Length > 0)
				return Lang.GetMapObjectName(tiles[0]);
		}

		return null;
	}

	private static string Prettify(string name)
	{
		if (string.IsNullOrEmpty(name))
			return "";

		Match item = Regex.Match(name, @"ITEM_(?:PICKUP|CRAFT)_(\d+)");
		if (item.Success && int.TryParse(item.Groups[1].Value, out int itemId))
			return Lang.GetItemNameValue(itemId);

		Match buff = Regex.Match(name, @"^BUFF_(\d+)$");
		if (buff.Success && int.TryParse(buff.Groups[1].Value, out int buffId))
			return Lang.GetBuffName(buffId);

		Match boss = Regex.Match(name, @"^BOSS_(\d+)$");
		if (boss.Success && int.TryParse(boss.Groups[1].Value, out int bossId))
			return Lang.GetNPCNameValue(bossId);

		Match npc = Regex.Match(name, @"NPC_KILLED_(\d+)");
		if (npc.Success && int.TryParse(npc.Groups[1].Value, out int npcId))
			return Lang.GetNPCNameValue(npcId);

		Match tile = Regex.Match(name, @"TILE_DESTROYED_(\d+)");
		if (tile.Success && int.TryParse(tile.Groups[1].Value, out int tileId))
		{
			string tileName = Lang.GetMapObjectName(tileId);
			if (!string.IsNullOrEmpty(tileName))
				return tileName;
		}

		return name.Replace('_', ' ');
	}

	private static short[] Shorts(object target, params string[] names)
	{
		Type type = target.GetType();
		foreach (string name in names)
		{
			FieldInfo field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (field == null)
				continue;

			if (field.GetValue(target) is short[] shorts)
				return shorts;

			if (field.GetValue(target) is int[] ints)
				return Array.ConvertAll(ints, value => (short)value);
		}

		return Array.Empty<short>();
	}
}
