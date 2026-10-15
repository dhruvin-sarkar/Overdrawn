using Overdrawn.Scoring;

namespace Overdrawn.Jokers;

/// +3 Mult if you hit at least twice this hand. The hits are the cards past
/// the opening two; a double takes its one card without hitting.
public sealed class BoldPlay : JokerRule
{
	public const int Mult = 3;
	public const int HitsNeeded = 2;

	public override string Id => "bold_play";

	public override JokerTrigger? OnScore(ScoringHand hand)
	{
		int hits = hand.Hand.Cards.Count - 2;
		return !hand.Hand.Doubled && hits >= HitsNeeded ? JokerTrigger.AddMult(Mult) : null;
	}
}
