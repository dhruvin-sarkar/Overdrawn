using Overdrawn.Cards;

namespace Overdrawn.Blackjack;

/// <summary>
/// One card as it sits in the shoe or in a hand.
/// </summary>
public readonly record struct PlayingCard(Suit Suit, Rank Rank)
{
	/// Blackjack value with an Ace counted low. A hand decides when an Ace is
	/// worth eleven instead, because that only makes sense across a whole hand.
	public int Value => Rank switch
	{
		Rank.Ace => 1,
		Rank.Jack or Rank.Queen or Rank.King => 10,
		_ => (int)Rank,
	};
}
