using Overdrawn.Blackjack;
using Overdrawn.Scoring;

namespace Overdrawn.Jokers;

/// A bust forfeits half the wager instead of all of it, rounded the way a
/// surrender is, and still scores as a Low Hand on the cards it busted with.
public sealed class NoSuchThingAsBust : JokerRule
{
	public override string Id => "no_such_thing_as_bust";

	public override HandSettlement Overrule(HandSettlement settlement, Hand hand) =>
		settlement.Result == Settlement.PlayerBust
			? settlement with { Payout = HandResolver.ForfeitHalf(hand.Wager) }
			: settlement;

	public override ScoringHand Reframe(ScoringHand hand) =>
		hand.Outcome == HandOutcome.Bust && hand.Hand.IsBust ? hand with { Outcome = HandOutcome.StandUnder17 } : hand;
}
