using Overdrawn.Scoring;

namespace Overdrawn.Jokers;

/// +1 Mult on each of the first 3 hands played per level. Both halves of a
/// split belong to the deal they came from.
public sealed class WarmUp : JokerRule
{
	public const int Mult = 1;
	public const int Hands = 3;

	public override string Id => "warm_up";

	public override JokerTrigger? OnScore(ScoringHand hand) => hand.HandNumber <= Hands ? JokerTrigger.AddMult(Mult) : null;
}
