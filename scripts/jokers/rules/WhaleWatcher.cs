using Overdrawn.Scoring;

namespace Overdrawn.Jokers;

/// +1 Mult per $150 in the bankroll, counted again on every hand. Debt does
/// not come off the count; it only leaves less cash to count.
public sealed class WhaleWatcher : JokerRule
{
	public const int DollarsPerMult = 150;

	public override string Id => "whale_watcher";

	public override JokerTrigger? OnScore(ScoringHand hand)
	{
		int mult = hand.Bankroll / DollarsPerMult;
		return mult == 0 ? null : JokerTrigger.AddMult(mult);
	}
}
