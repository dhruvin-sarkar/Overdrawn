namespace Overdrawn.Jokers;

/// Interest cap raised by $100.
public sealed class DeepPockets : JokerRule
{
	public const int Bonus = 100;

	public override string Id => "deep_pockets";

	public override int InterestCapBonus => Bonus;
}
