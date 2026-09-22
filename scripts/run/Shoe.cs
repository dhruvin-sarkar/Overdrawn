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

	private int _cap;

	public Shoe(IEnumerable<PlayingCard> deck, Random random)
	{
		_deck = deck.ToList();
		_random = random;
		Shuffle();
	}

	/// The player's own cards. A level never touches this.
	public IReadOnlyList<PlayingCard> Deck => _deck;
	public IReadOnlyList<PlayingCard> Temporary => _temporary;
	public int Remaining => _drawPile.Count;

	/// <param name="cap">How many cards the shoe holds this level, or zero for all of them.</param>
	public void BeginLevel(IEnumerable<PlayingCard> temporary, int cap = 0)
	{
		if (cap < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(cap), cap, "A shoe cannot hold fewer than no cards.");
		}

		_temporary.Clear();
		_temporary.AddRange(temporary);
		_cap = cap;
		Shuffle();
	}

	public void EndLevel()
	{
		_temporary.Clear();
		_cap = 0;
		Shuffle();
	}

	/// Adds a card to the shoe for the rest of this level only, the way a table
	/// that duplicates dealt cards does.
	public void AddTemporary(PlayingCard card)
	{
		_temporary.Add(card);
		_drawPile.Insert(_random.Next(_drawPile.Count + 1), card);
	}

	public PlayingCard Draw()
	{
		if (_drawPile.Count == 0)
		{
			Shuffle();
		}

		PlayingCard card = _drawPile[^1];
		_drawPile.RemoveAt(_drawPile.Count - 1);
		return card;
	}

	/// Rebuilds the draw pile from the deck plus this level's cards.
	public void Shuffle()
	{
		_drawPile.Clear();
		_drawPile.AddRange(_deck);
		_drawPile.AddRange(_temporary);

		for (int i = _drawPile.Count - 1; i > 0; i--)
		{
			int j = _random.Next(i + 1);
			(_drawPile[i], _drawPile[j]) = (_drawPile[j], _drawPile[i]);
		}

		if (_cap > 0 && _drawPile.Count > _cap)
		{
			_drawPile.RemoveRange(_cap, _drawPile.Count - _cap);
		}
	}

	public static List<PlayingCard> StandardDeck() =>
		(from Suit suit in Enum.GetValues<Suit>()
			from Rank rank in Enum.GetValues<Rank>()
			select new PlayingCard(suit, rank)).ToList();
}
