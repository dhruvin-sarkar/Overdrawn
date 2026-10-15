using Overdrawn.Scoring;

namespace Overdrawn.Jokers;

/// Double-down wins get x1.5 Mult on top of the doubled chips. The doubled
/// wager already pays double money; this multiplies the score as well.
public sealed class DoubleTrouble : JokerRule
{
	public const double Times = 1.5;

	public override string Id => "double_trouble";

	public override JokerTrigger? OnScore(ScoringHand hand) =>
		hand.Hand.Doubled && hand.Settlement.PlayerWon ? JokerTrigger.TimesMult(Times) : null;
}
