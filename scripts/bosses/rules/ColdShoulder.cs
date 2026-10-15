using Overdrawn.Blackjack;
using Overdrawn.Cards;

namespace Overdrawn.Bosses;

/// The shoe for this level has no Aces. They are back in the deck once the
/// level is over.
public sealed class ColdShoulder : BossRule
{
	public override string Id => "cold_shoulder";
	public override double TargetMultiplier => 1.7;

	public override bool Strikes(PlayingCard card) => card.Rank == Rank.Ace;
}
