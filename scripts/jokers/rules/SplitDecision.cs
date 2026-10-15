using Overdrawn.Scoring;

namespace Overdrawn.Jokers;

/// If you split this hand, retrigger every Joker's effects on the second half.
/// Every half after the first counts as a second half.
public sealed class SplitDecision : JokerRule
{
	public override string Id => "split_decision";

	public override int Retriggers(ScoringHand hand) => hand.Hand.FromSplit && hand.Seat > 0 ? 1 : 0;
}
