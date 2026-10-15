using Overdrawn.Run;

namespace Overdrawn.Jokers;

/// A loss does not break the win streak, the first time each level.
public sealed class StreakShield : JokerRule
{
	public override string Id => "streak_shield";

	public bool Spent { get; private set; }

	public override bool ShieldLoss()
	{
		if (Spent)
		{
			return false;
		}

		Spent = true;
		return true;
	}

	public override void OnLevelStart(RunState run) => Spent = false;
}
