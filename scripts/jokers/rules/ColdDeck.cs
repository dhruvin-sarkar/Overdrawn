using Overdrawn.Cards;
using Overdrawn.Run;
using Overdrawn.Scoring;

namespace Overdrawn.Jokers;

/// Strikes every face card from the deck for good the moment it joins the
/// rail, and gives +5 Mult on every hand for the rest of the run.
public sealed class ColdDeck : JokerRule
{
	public const int Mult = 5;

	public override string Id => "cold_deck";

	public override void OnAcquired(RunState run) =>
		run.Shoe.RemoveFromDeck(card => card.Rank is Rank.Jack or Rank.Queen or Rank.King);

	public override JokerTrigger? OnScore(ScoringHand hand) => JokerTrigger.AddMult(Mult);
}
