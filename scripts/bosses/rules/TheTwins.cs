using Overdrawn.Blackjack;

namespace Overdrawn.Bosses;

/// No splitting this level.
public sealed class TheTwins : BossRule
{
	public override string Id => "the_twins";
	public override double TargetMultiplier => 1.5;

	public override HouseRules Bend(HouseRules rules) => rules with { SplitAllowed = false };
}
