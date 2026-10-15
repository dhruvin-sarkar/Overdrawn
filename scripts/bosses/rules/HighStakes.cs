namespace Overdrawn.Bosses;

/// Score Target doubled, but Base Chips also doubled. The doubled target is
/// the multiplier itself.
public sealed class HighStakes : BossRule
{
	public override string Id => "high_stakes";
	public override double TargetMultiplier => 2.0;
	public override int BaseChipsScale => 2;
}
