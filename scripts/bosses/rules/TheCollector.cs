namespace Overdrawn.Bosses;

/// Doubles any outstanding debt the moment the player sits down; clearing the
/// level wipes all debt.
public sealed class TheCollector : BossRule
{
	public override string Id => "the_collector";
	public override double TargetMultiplier => 2.0;
	public override bool WipesDebtOnClear => true;

	public override int DebtOnArrival(int debt) => debt * 2;
}
