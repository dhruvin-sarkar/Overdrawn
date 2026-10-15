using Overdrawn.Blackjack;

namespace Overdrawn.Bosses;

/// No doubling down this level.
public sealed class TheVault : BossRule
{
	public override string Id => "the_vault";
	public override double TargetMultiplier => 1.5;

	public override HouseRules Bend(HouseRules rules) => rules with { DoubleAllowed = false };
}
