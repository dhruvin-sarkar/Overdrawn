using System.Linq;
using Overdrawn.Scoring;

namespace Overdrawn.Jokers;

/// +2 Mult per card of the majority suit in the played hand: whichever suit
/// shows up most. A tie counts the same whichever suit is taken.
public sealed class FlushTable : JokerRule
{
	public const int MultPerCard = 2;

	public override string Id => "flush_table";

	public override JokerTrigger? OnScore(ScoringHand hand)
	{
		int majority = hand.Hand.Cards.GroupBy(card => card.Suit).Max(suit => suit.Count());
		return JokerTrigger.AddMult(MultPerCard * majority);
	}
}
