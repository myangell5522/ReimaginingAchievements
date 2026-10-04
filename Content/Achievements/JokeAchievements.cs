using ReimaginingAchievements.Content.Tracking;

namespace ReimaginingAchievements.Content.Achievements;

public class MoneyIsSafe : OwnedAchievement
{
}

public class OnlyWayIsDown : OwnedAchievement
{
}

public class MrGrinch : OwnedAchievement
{
}

public class LetMeFinish : OwnedAchievement
{
}

public class ItsHarder : OwnedAchievement
{
}

public class ItsHardest : OwnedAchievement
{
}

public class WeakAreMighty : OwnedAchievement
{
}

public class AintACharity : OwnedAchievement
{
}

public class YouExist : OwnedAchievement
{
}

public class Noooo : OwnedAchievement
{
}

public class SufferingStronger : CounterAchievement
{
	protected override string StatusKey => "Nurse";

	protected override int Goal => 10;

	protected override int Current => LocalCount(player => player.NurseHeals);
}

public class RichGetRicher : CounterAchievement
{
	protected override string StatusKey => "Platinum";

	protected override int Goal => 5;

	protected override int Current => LocalCount(player => player.PlatinumToday);
}

public class BulletStorm : OwnedAchievement
{
}

public class BoomBoomBoom : CounterAchievement
{
	protected override string StatusKey => "Dynamite";

	protected override int Goal => 500;

	protected override int Current => LocalCount(player => player.DynamiteBought);
}
