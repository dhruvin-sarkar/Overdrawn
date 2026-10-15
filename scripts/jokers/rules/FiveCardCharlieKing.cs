using Overdrawn.Scoring;

namespace Overdrawn.Jokers;

/// A hand of five or more cards that has not bust gets +80 Chips and +4 Mult.
public sealed class FiveCardCharlieKing : JokerRule
{
	public const int CardsNeeded = 5;
	public const int Chips = 80;
	public const int Mult = 4;

	public override string Id => "five_card_charlie_king";

	public override JokerTrigger? OnScore(ScoringHand hand) =>
		hand.Hand.Cards.Count >= CardsNeeded && !hand.Hand.IsBust ? new JokerTrigger(Chips, Mult) : null;
}
