using System;
using Overdrawn.Blackjack;

namespace Overdrawn.Bosses;

/// Blackjack pays 6:5. Never better than a table that already pays worse.
public sealed class OlSixtyFive : BossRule
{
	public const double SixToFive = 1.2;

	public override string Id => "ol_sixty_five";
	public override double TargetMultiplier => 1.6;

	public override HouseRules Bend(HouseRules rules) =>
		rules with { BlackjackPayout = Math.Min(rules.BlackjackPayout, SixToFive) };
}
