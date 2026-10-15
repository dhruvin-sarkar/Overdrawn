using Overdrawn.Scoring;

namespace Overdrawn.Jokers;

/// +3 Mult if you stood without ever hitting this hand: it ended on its
/// opening two cards, whether the player stood on them or they made 21. A
/// double takes a card, so a doubled hand is not a careful one.
public sealed class CarefulPlayer : JokerRule
{
	public const int Mult = 3;

	public override string Id => "careful_player";

	public override JokerTrigger? OnScore(ScoringHand hand) =>
		hand.Hand.Cards.Count == 2 && !hand.Hand.Surrendered ? JokerTrigger.AddMult(Mult) : null;
}
