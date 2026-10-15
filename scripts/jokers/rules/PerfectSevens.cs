using System.Linq;
using Overdrawn.Cards;
using Overdrawn.Scoring;

namespace Overdrawn.Jokers;

/// A hand of exactly 7-7-7 clears the level's Score Target outright.
public sealed class PerfectSevens : JokerRule
{
	public override string Id => "perfect_sevens";

	public override bool ClearsLevel(ScoringHand hand) =>
		hand.Hand.Cards.Count == 3 && hand.Hand.Cards.All(card => card.Rank == Rank.Seven);
}
