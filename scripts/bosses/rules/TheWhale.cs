namespace Overdrawn.Bosses;

/// Minimum wager tripled this level.
public sealed class TheWhale : BossRule
{
	public const int MinimumMultiple = 3;

	public override string Id => "the_whale";
	public override double TargetMultiplier => 1.7;

	public override int MinimumWager(int standard) => standard * MinimumMultiple;
}
