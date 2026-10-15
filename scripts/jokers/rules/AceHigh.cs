using Overdrawn.Scoring;

namespace Overdrawn.Jokers;

/// Aces played as 11 give +8 Chips. A hand can only ever count one Ace as
/// eleven, so this fires once, on any soft total.
public sealed class AceHigh : JokerRule
{
	public const int Chips = 8;

	public override string Id => "ace_high";

	public override JokerTrigger? OnScore(ScoringHand hand) => hand.Hand.IsSoft ? JokerTrigger.AddChips(Chips) : null;
}
