using System.Linq;
using Overdrawn.Cards;
using Overdrawn.Scoring;

namespace Overdrawn.Jokers;

/// +4 Mult if the played hand has no Aces.
public sealed class SteadyHand : JokerRule
{
	public const int Mult = 4;

	public override string Id => "steady_hand";

	public override JokerTrigger? OnScore(ScoringHand hand) =>
		hand.Hand.Cards.Any(card => card.Rank == Rank.Ace) ? null : JokerTrigger.AddMult(Mult);
}
