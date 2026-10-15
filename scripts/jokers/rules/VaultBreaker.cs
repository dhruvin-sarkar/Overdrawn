using Overdrawn.Blackjack;

namespace Overdrawn.Jokers;

/// A boss table that bans doubling or splitting does not ban them for you.
public sealed class VaultBreaker : JokerRule
{
	public override string Id => "vault_breaker";

	public override HouseRules Bend(HouseRules rules) => rules with { DoubleAllowed = true, SplitAllowed = true };
}
