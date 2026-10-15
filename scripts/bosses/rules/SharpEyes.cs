using Overdrawn.Blackjack;

namespace Overdrawn.Bosses;

/// Dealer peeks, auto-wins all pushes. Every table here already checks for a
/// dealer natural before anyone acts, so what this dealer brings is the ties.
public sealed class SharpEyes : BossRule
{
	public override string Id => "sharp_eyes";
	public override double TargetMultiplier => 1.8;

	public override HouseRules Bend(HouseRules rules) => rules with { DealerWinsTies = true };
}
