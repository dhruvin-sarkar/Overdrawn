using Overdrawn.Scoring;

namespace Overdrawn.Jokers;

/// +15 Chips if your first two cards share the same rank. Rank, not value: a
/// King and a Queen count the same but are not two of a kind.
public sealed class TwoOfAKind : JokerRule
{
	public const int Chips = 15;

	public override string Id => "two_of_a_kind";

	public override JokerTrigger? OnScore(ScoringHand hand) =>
		hand.Hand.Cards[0].Rank == hand.Hand.Cards[1].Rank ? JokerTrigger.AddChips(Chips) : null;
}
