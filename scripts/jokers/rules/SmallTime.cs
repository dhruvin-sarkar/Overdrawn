using Overdrawn.Scoring;

namespace Overdrawn.Jokers;

/// +20 Chips flat on every hand.
public sealed class SmallTime : JokerRule
{
	public const int Chips = 20;

	public override string Id => "small_time";

	public override JokerTrigger? OnScore(ScoringHand hand) => JokerTrigger.AddChips(Chips);
}
