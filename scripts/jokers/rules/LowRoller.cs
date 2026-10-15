using System.Linq;
using Overdrawn.Scoring;

namespace Overdrawn.Jokers;

/// +1 Mult per card valued 6 or under in the played hand. Every Ace is worth 1
/// except the one a soft hand plays as eleven.
public sealed class LowRoller : JokerRule
{
	public override string Id => "low_roller";

	public override JokerTrigger? OnScore(ScoringHand hand)
	{
		int lows = hand.Hand.Cards.Count(card => card.Value <= 6) - (hand.Hand.IsSoft ? 1 : 0);
		return lows == 0 ? null : JokerTrigger.AddMult(lows);
	}
}
