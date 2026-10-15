using System;
using Overdrawn.Blackjack;

namespace Overdrawn.Bosses;

/// Dealer hits soft 17, peeks and Blackjack pays 1:1, all at once: The House
/// and Sharp Eyes at one table, so this dealer takes every tie as well.
public sealed class TheFinalTable : BossRule
{
	public override string Id => "the_final_table";
	public override double TargetMultiplier => 2.5;

	public override HouseRules Bend(HouseRules rules) => rules with
	{
		DealerHitsSoft17 = true,
		BlackjackPayout = Math.Min(rules.BlackjackPayout, 1.0),
		DealerWinsTies = true,
	};
}
