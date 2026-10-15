using System;
using Overdrawn.Blackjack;

namespace Overdrawn.Bosses;

/// Dealer hits soft 17; Blackjack pays 1:1.
public sealed class TheHouse : BossRule
{
	public override string Id => "the_house";
	public override double TargetMultiplier => 1.6;

	public override HouseRules Bend(HouseRules rules) =>
		rules with { DealerHitsSoft17 = true, BlackjackPayout = Math.Min(rules.BlackjackPayout, 1.0) };
}
