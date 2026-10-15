using System;
using System.Linq;
using Overdrawn.Cards;
using Overdrawn.Scoring;

namespace Overdrawn.Jokers;

/// Permanent +1 Chip scaling each time a 2-6 is played this run, capped at
/// +50. A hand's own low cards are counted before it scores, bust or not.
public sealed class CardCounter : JokerRule
{
	public const int Cap = 50;

	public override string Id => "card_counter";

	public int Counted { get; private set; }

	public override JokerTrigger? OnScore(ScoringHand hand) => Counted == 0 ? null : JokerTrigger.AddChips(Counted);

	public override void OnSettled(ScoringHand hand)
	{
		int lows = hand.Hand.Cards.Count(card => card.Rank is >= Rank.Two and <= Rank.Six);
		Counted = Math.Min(Cap, Counted + lows);
	}
}
