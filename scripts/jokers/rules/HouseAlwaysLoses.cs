namespace Overdrawn.Jokers;

/// The boss's special rule is switched off entirely. The level still counts as
/// a boss for its target and its payout.
public sealed class HouseAlwaysLoses : JokerRule
{
	public override string Id => "house_always_loses";

	public override bool DisablesBossRule => true;
}
