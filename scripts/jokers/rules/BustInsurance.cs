using System.Linq;
using Overdrawn.Blackjack;
using Overdrawn.Run;
using Overdrawn.Scoring;

namespace Overdrawn.Jokers;

/// The first bust each level scores as if you had stood right before busting:
/// the hand without the card that broke it. The money is still a bust's.
public sealed class BustInsurance : JokerRule
{
	public override string Id => "bust_insurance";

	public bool Spent { get; private set; }

	public override ScoringHand Reframe(ScoringHand hand)
	{
		if (Spent || hand.Outcome != HandOutcome.Bust || !hand.Hand.IsBust)
		{
			return hand;
		}

		var stood = new Hand(hand.Hand.Wager, hand.Hand.FromSplit);
		foreach (PlayingCard card in hand.Hand.Cards.SkipLast(1))
		{
			stood.Deal(card);
		}

		stood.Stand();
		Spent = true;
		return hand with { Hand = stood, Outcome = ScoreCalculator.Classify(stood) };
	}

	public override void OnLevelStart(RunState run) => Spent = false;
}
