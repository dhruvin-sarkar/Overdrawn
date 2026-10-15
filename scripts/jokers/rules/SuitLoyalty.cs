using System;
using System.Linq;
using Overdrawn.Cards;
using Overdrawn.Run;
using Overdrawn.Scoring;

namespace Overdrawn.Jokers;

/// The player swears to a suit on pickup; +3 Mult per card of that suit in the
/// played hand for the rest of the run.
public sealed class SuitLoyalty : JokerRule
{
	public const int MultPerCard = 3;

	public override string Id => "suit_loyalty";

	/// The suit sworn to, settled before the joker joins the rail.
	public Suit? Chosen { get; private set; }

	public void Choose(Suit suit)
	{
		if (Chosen is not null)
		{
			throw new InvalidOperationException($"Suit Loyalty is already sworn to {Chosen}.");
		}

		Chosen = suit;
	}

	public override void OnAcquired(RunState run)
	{
		if (Chosen is null)
		{
			throw new InvalidOperationException("Suit Loyalty joins the rail with its suit already chosen.");
		}
	}

	public override JokerTrigger? OnScore(ScoringHand hand)
	{
		int loyal = hand.Hand.Cards.Count(card => card.Suit == Chosen);
		return loyal == 0 ? null : JokerTrigger.AddMult(MultPerCard * loyal);
	}
}
