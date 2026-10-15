using System.Collections.Generic;
using System.Linq;
using Overdrawn.Blackjack;
using Overdrawn.Cards;
using Overdrawn.Jokers;
using Overdrawn.Run;
using Overdrawn.Scoring;

namespace Overdrawn.Tests;

/// The hands, settlements and rigged runs the joker tests are dealt from.
internal static class Deals
{
	public const int Wager = 20;

	public static readonly HandSettlement Won = new(Settlement.PlayerWin, Wager);
	public static readonly HandSettlement Pushed = new(Settlement.Push, 0);
	public static readonly HandSettlement Lost = new(Settlement.DealerWin, -Wager);
	public static readonly HandSettlement Busted = new(Settlement.PlayerBust, -Wager);

	/// An opening hand, all spades.
	public static Hand Opening(params Rank[] ranks) =>
		Opening(ranks.Select(rank => new PlayingCard(Suit.Spades, rank)).ToArray());

	public static Hand Opening(params PlayingCard[] cards)
	{
		var hand = new Hand(Wager);
		foreach (PlayingCard card in cards)
		{
			hand.Deal(card);
		}

		return hand;
	}

	/// One half of a split, both of its cards already dealt.
	public static Hand SplitHalf(params Rank[] ranks)
	{
		var hand = new Hand(Wager, fromSplit: true);
		foreach (Rank rank in ranks)
		{
			hand.Deal(new PlayingCard(Suit.Spades, rank));
		}

		return hand;
	}

	public static Hand Hit(this Hand hand, Rank rank, Suit suit = Suit.Spades)
	{
		hand.Take(new PlayingCard(suit, rank));
		return hand;
	}

	public static Hand Standing(this Hand hand)
	{
		hand.Stand();
		return hand;
	}

	public static Hand Doubling(this Hand hand, Rank rank)
	{
		hand.DoubleDown();
		hand.Take(new PlayingCard(Suit.Spades, rank));
		return hand;
	}

	/// A finished hand as the scoring pipeline sees it. A win unless told otherwise.
	public static ScoringHand Played(Hand hand, HandSettlement? settlement = null, int winStreak = 0, int seat = 0,
		int handNumber = 1, int bankroll = 100) =>
		new(hand, settlement ?? Won, seat, winStreak, handNumber, bankroll);

	public static ScoreBreakdown Score(Hand hand, params JokerRule[] jokers) =>
		ScoreCalculator.Score(Played(hand), jokers);

	public static RunState NewRun(int jokerSlots = RunEconomy.BaseJokerSlots) =>
		RunState.Begin("house_deck", Shoe.StandardDeck(), seed: 7, DeckTerms.House with { JokerSlots = jokerSlots });

	/// A run whose shoe deals exactly these cards, in this order: player,
	/// dealer, player, dealer, then the rest from the top.
	public static RunState Rigged(params Rank[] order) => Rigged(DeckTerms.House, order);

	public static RunState Rigged(DeckTerms terms, params Rank[] order)
	{
		List<PlayingCard> deck = Shoe.StandardDeck();
		deck.AddRange(order.Select(rank => new PlayingCard(Suit.Spades, rank)).Reverse());
		return RunState.Begin("test", deck, seed: 1, terms);
	}

	/// Deals a hand at the standard wager, plays the moves, then lets the dealer
	/// finish and the table settle, the way the table screen does.
	public static Round Settled(RunState run, params Move[] moves)
	{
		var round = new Round(run);
		round.Begin(Wager);
		foreach (Move move in moves)
		{
			round.Play(move);
		}

		round.RevealHole();
		while (round.DealerStep())
		{
		}

		round.Finish();
		return round;
	}
}
