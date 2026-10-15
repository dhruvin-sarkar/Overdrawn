using System;
using System.Collections.Generic;
using System.Linq;
using Overdrawn.Blackjack;
using Overdrawn.Cards;

namespace Overdrawn.Run;

/// <summary>
/// The cards a level is dealt from. The player owns the permanent deck; a level
/// layers its own temporary cards on top and takes them away again when it
/// ends, so a level variant can never change what the player actually owns.
/// </summary>
public sealed class Shoe
{
	private readonly List<PlayingCard> _deck;
	private readonly List<PlayingCard> _temporary = new();
	private readonly List<PlayingCard> _drawPile = new();
	private readonly Random _random;

	/// How few cards may be left when a hand starts. A real table sets its cut
	/// card so a shuffle never interrupts a hand, because the cards already on
	/// the felt must not come back and be dealt a second time.
	public const int CutCard = 24;

	private int _cap;
	private Func<PlayingCard, bool>? _strike;

	public Shoe(IEnumerable<PlayingCard> deck, Random random)
	{
		_deck = deck.ToList();
		_random = random;
		Fill();
	}

	/// The player's own cards. A level never touches this.
	public IReadOnlyList<PlayingCard> Deck => _deck;
	public IReadOnlyList<PlayingCard> Temporary => _temporary;
	public int Remaining => _drawPile.Count;

	/// How many cards a full shoe holds this level, once struck cards are out
	/// and the cap is in.
	public int Stock => Math.Min(_cap > 0 ? _cap : int.MaxValue, Dealable().Count());

	/// <param name="cap">How many cards the shoe holds this level, or zero for all of them.</param>
	/// <param name="strike">Cards kept out of the shoe for this level only, such as a boss's.</param>
	public void BeginLevel(IEnumerable<PlayingCard> temporary, int cap = 0, Func<PlayingCard, bool>? strike = null)
	{
		if (cap < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(cap), cap, "A shoe cannot hold fewer than no cards.");
		}

		_temporary.Clear();
		_temporary.AddRange(temporary);
		_cap = cap;
		_strike = strike;
		Shuffle();
	}

	public void EndLevel()
	{
		_temporary.Clear();
		_cap = 0;
		_strike = null;
		Shuffle();
	}

	/// Strikes cards out of the permanent deck for good. The cards already in
	/// the draw pile stay until the next shuffle, the way a real shoe only
	/// changes between hands. Returns how many were struck.
	public int RemoveFromDeck(Func<PlayingCard, bool> match) => _deck.RemoveAll(card => match(card));

	/// Adds a card to the shoe for the rest of this level only, the way a table
	/// that duplicates dealt cards does.
	public void AddTemporary(PlayingCard card)
	{
		_temporary.Add(card);
		_drawPile.Insert(_random.Next(_drawPile.Count + 1), card);
	}

	/// Reaches for the cut card before a hand is dealt. A shoe is reshuffled
	/// here and nowhere else, so a hand always finishes on the cards it began on.
	public void BeginHand()
	{
		if (_drawPile.Count <= CutCard)
		{
			Shuffle();
		}
	}

	/// The card the next draw takes. Read it after reaching for the cut card,
	/// because a shuffle there would change it.
	public PlayingCard Next => _drawPile.Count > 0
		? _drawPile[^1]
		: throw new InvalidOperationException("The shoe is empty; the cut card shuffles it between hands.");

	public PlayingCard Draw()
	{
		if (_drawPile.Count == 0)
		{
			throw new InvalidOperationException("The shoe ran dry inside a hand; the cut card shuffles it between hands.");
		}

		PlayingCard card = _drawPile[^1];
		_drawPile.RemoveAt(_drawPile.Count - 1);
		return card;
	}

	/// Rebuilds the draw pile from the deck plus this level's cards, then puts
	/// it in a random order. A level is always started by shuffling.
	public void Shuffle()
	{
		Fill();

		for (int i = _drawPile.Count - 1; i > 0; i--)
		{
			int j = _random.Next(i + 1);
			(_drawPile[i], _drawPile[j]) = (_drawPile[j], _drawPile[i]);
		}

		// A thin shoe keeps a random handful rather than the same cards each time.
		if (_cap > 0 && _drawPile.Count > _cap)
		{
			_drawPile.RemoveRange(_cap, _drawPile.Count - _cap);
		}
	}

	private void Fill()
	{
		_drawPile.Clear();
		_drawPile.AddRange(Dealable());
	}

	private IEnumerable<PlayingCard> Dealable() =>
		_deck.Concat(_temporary).Where(card => _strike is null || !_strike(card));

	/// <param name="without">Ranks the deck is built without, such as a deck that has had its fours pulled.</param>
	public static List<PlayingCard> StandardDeck(IEnumerable<Rank>? without = null)
	{
		var removed = without?.ToHashSet() ?? new HashSet<Rank>();
		return (from Suit suit in Enum.GetValues<Suit>()
			from Rank rank in Enum.GetValues<Rank>()
			where !removed.Contains(rank)
			select new PlayingCard(suit, rank)).ToList();
	}
}
