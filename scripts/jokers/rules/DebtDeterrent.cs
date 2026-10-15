namespace Overdrawn.Jokers;

/// Debt stops compounding and grows by a flat $5 a level instead.
public sealed class DebtDeterrent : JokerRule
{
	public const int FlatGrowth = 5;

	public override string Id => "debt_deterrent";

	public override int GrowDebt(int debt, int standard) => debt <= 0 ? 0 : debt + FlatGrowth;
}
