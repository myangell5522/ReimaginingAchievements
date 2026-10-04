using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Achievements;
using Terraria.GameContent.Achievements;
using Terraria.ID;
using Terraria.ModLoader;

namespace ReimaginingAchievements.Content.Achievements;

public static class VanillaCatalog
{
	public const int Melee = 1;
	public const int Ranged = 2;
	public const int Magic = 3;
	public const int Summon = 4;

	private static bool _built;
	private static readonly Dictionary<int, List<int>> Weapons = new Dictionary<int, List<int>>();
	private static readonly List<int> Armor = new List<int>();
	private static readonly SortedSet<int> PotionBuffs = new SortedSet<int>();

	public static List<int> WeaponsOf(int kind)
	{
		Build();
		return Weapons.TryGetValue(kind, out List<int> list) ? list : new List<int>();
	}

	public static List<int> ArmorPieces()
	{
		Build();
		return Armor;
	}

	public static IEnumerable<int> Potions()
	{
		Build();
		return PotionBuffs;
	}

	private static void Build()
	{
		if (_built)
			return;

		_built = true;
		for (int kind = Melee; kind <= Summon; kind++)
			Weapons[kind] = new List<int>();

		for (int type = 1; type < ItemID.Count; type++)
		{
			if (ItemID.Sets.Deprecated[type])
				continue;

			Item item = new Item();
			try
			{
				item.SetDefaults(type);
			}
			catch (Exception)
			{
				continue;
			}

			if (item.type != type)
				continue;

			int kind = WeaponKind(item);
			if (kind != 0)
				Weapons[kind].Add(type);

			if ((item.headSlot > 0 || item.bodySlot > 0 || item.legSlot > 0) && item.defense > 0 && !item.vanity)
				Armor.Add(type);

			if (IsPotion(item))
				PotionBuffs.Add(item.buffType);
		}
	}

	private static int WeaponKind(Item item)
	{
		if (item.damage <= 0 || item.accessory || item.ammo != AmmoID.None || item.consumable || item.useStyle == ItemUseStyleID.None)
			return 0;

		if (item.pick > 0 || item.axe > 0 || item.hammer > 0 || item.fishingPole > 0 || item.createTile != -1 || item.createWall != -1)
			return 0;

		if (item.CountsAsClass(DamageClass.Summon) || item.CountsAsClass(DamageClass.SummonMeleeSpeed))
			return Summon;

		if (item.CountsAsClass(DamageClass.Melee))
			return Melee;

		if (item.CountsAsClass(DamageClass.Ranged))
			return Ranged;

		if (item.CountsAsClass(DamageClass.Magic))
			return Magic;

		return 0;
	}

	private static bool IsPotion(Item item)
	{
		if (!item.consumable || item.buffType <= 0 || item.buffTime <= 0 || ItemID.Sets.IsFood[item.type])
			return false;

		int buff = item.buffType;
		if (buff >= BuffID.Count || Main.debuff[buff])
			return false;

		return buff != BuffID.WellFed && buff != BuffID.WellFed2 && buff != BuffID.WellFed3 && buff != BuffID.Tipsy;
	}
}

public abstract class PickupCollection : ModAchievement
{
	protected abstract IEnumerable<int> Items { get; }

	public override void SetStaticDefaults()
	{
		Achievement.SetCategory(AchievementCategory.Collector);
		foreach (int type in Items)
			AddItemPickupCondition(type);
	}

	public override Position GetDefaultPosition() => new After("CHAMPION_OF_TERRARIA");
}

public class MeleeMaster : PickupCollection
{
	protected override IEnumerable<int> Items => VanillaCatalog.WeaponsOf(VanillaCatalog.Melee);
}

public class RangedMaster : PickupCollection
{
	protected override IEnumerable<int> Items => VanillaCatalog.WeaponsOf(VanillaCatalog.Ranged);
}

public class MagicMaster : PickupCollection
{
	protected override IEnumerable<int> Items => VanillaCatalog.WeaponsOf(VanillaCatalog.Magic);
}

public class SummonMaster : PickupCollection
{
	protected override IEnumerable<int> Items => VanillaCatalog.WeaponsOf(VanillaCatalog.Summon);
}

public class FullArmory : PickupCollection
{
	protected override IEnumerable<int> Items => VanillaCatalog.ArmorPieces();
}

public class Alchemist : ModAchievement
{
	private readonly Dictionary<int, CustomFlagCondition> _buffs = new Dictionary<int, CustomFlagCondition>();

	public override void SetStaticDefaults()
	{
		Achievement.SetCategory(AchievementCategory.Collector);
		foreach (int buff in VanillaCatalog.Potions())
			_buffs[buff] = AddCondition("BUFF_" + buff);
	}

	public override Position GetDefaultPosition() => new After("CHAMPION_OF_TERRARIA");

	public void Check(Player player)
	{
		if (Achievement.IsCompleted)
			return;

		for (int i = 0; i < player.buffType.Length; i++)
		{
			int type = player.buffType[i];
			if (type > 0 && player.buffTime[i] > 0 && _buffs.TryGetValue(type, out CustomFlagCondition condition) && !condition.IsCompleted)
				condition.Complete();
		}
	}
}
