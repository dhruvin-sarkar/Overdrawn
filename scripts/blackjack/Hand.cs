using System;
using System.Collections.Generic;
using System.Linq;
using Overdrawn.Cards;

namespace Overdrawn.Blackjack;

/// <summary>
/// One hand of blackjack and the money riding on it. A split produces a second
/// Hand rather than nesting, so every hand settles on its own.
/// </summary>
public sealed class Hand
{
	private readonly List<PlayingCard> _cards = new();

	public Hand(int wager, bool fromSplit = false)
	{
		if (wager < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(wager), wager, "A wager cannot be negative.");
		}

		Wager = wager;
		FromSplit = fromSplit;
	}

	public IReadOnlyList<PlayingCard> Cards => _cards;
	public int Wager { get; private set; }
	public bool Doubled { get; private set; }
	public bool Surrendered { get; private set; }
	public bool Stood { get; private set; }
	public bool FromSplit { get; }

	/// The best total that does not bust, or the hard total when every choice busts.
	public int Total
	{
		get
		{
			int total = _cards.Sum(card => card.Value);
			return SoftAce(total) ? total + 10 : total;
		}
	}

	/// True when an Ace is being counted as eleven, so the hand cannot bust on a hit.
	public bool IsSoft => SoftAce(_cards.Sum(card => card.Value));

	public bool IsBust => Total > 21;

	/// Two cards making 21. A split hand can reach 21 but never a natural.
	public bool IsBlackjack => _cards.Count == 2 && Total == 21 && !FromSplit;

	/// A hand is finished once it can take no more cards.
	public bool IsFinished => Stood || Surrendered || IsBust || Total == 21 || (Doubled && _cards.Count > 2);

	public bool IsPair => _cards.Count == 2 && _cards[0].Value == _cards[1].Value;

	public void Take(PlayingCard card)
	{
		if (IsFinished)
		{
			throw new InvalidOperationException("This hand has already finished.");
		}

		_cards.Add(card);
	}

	/// Deals the opening card of a hand, including the second half of a split.
	public void Deal(PlayingCard card) => _cards.Add(card);

	public void Stand() => Stood = true;

	public void DoubleDown()
	{
		if (_cards.Count != 2)
		{
			throw new InvalidOperationException("Doubling is only possible on the opening two cards.");
		}

		Wager *= 2;
		Doubled = true;
	}

	public void Surrender()
	{
		if (_cards.Count != 2)
		{
			throw new InvalidOperationException("Surrender is only possible on the opening two cards.");
		}

		Surrendered = true;
	}

	/// Takes the second card off a pair so it can start a hand of its own.
	public PlayingCard SplitOff()
	{
		if (!IsPair)
		{
			throw new InvalidOperationException("Only a pair can be split.");
		}

		PlayingCard card = _cards[1];
		_cards.RemoveAt(1);
		return card;
	}

	private bool SoftAce(int hardTotal) => _cards.Any(card => card.Rank == Rank.Ace) && hardTotal + 10 <= 21;
}
