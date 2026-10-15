using Overdrawn.Blackjack;

namespace Overdrawn.Jokers;

/// Tens and face cards always split together as a pair, not just two of one rank.
public sealed class TensCompany : JokerRule
{
	public override string Id => "tens_company";

	public override HouseRules Bend(HouseRules rules) => rules with { SplitAnyTens = true };
}
