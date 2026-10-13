using System;
using System.Collections.Generic;
using System.Linq;
using Overdrawn.Blackjack;
using Overdrawn.Cards;
using Overdrawn.Run;
using Overdrawn.Scoring;
using Xunit;

namespace Overdrawn.Tests;

public class RoundTests
{
	private const int Wager = 25;

	/// A run whose shoe deals exactly these cards, in this order, off the top of
	/// an otherwise full shoe. The table deals player, dealer, player, dealer,
	/// and takes the rest from the top. The deck underneath keeps the shoe clear
	/// of the cut card, the way a real one is when a hand begins.
	private static RunState Run(params Rank[] order)
	{
		List<PlayingCard> deck = Shoe.StandardDeck();
		deck.AddRange(order.Select(rank => new PlayingCard(Suit.Spades, rank)).Reverse());
		return RunState.Begin("test", deck, seed: 1);
	}

	private static Round Dealt(RunState run, int wager = Wager)
	{
		var round = new Round(run);
		round.Begin(wager);
		return round;
	}

	/// Plays the dealer out and settles, the way the table screen does.
	private static void Resolve(Round round)
	{
		round.RevealHole();
		while (round.DealerStep())
		{
		}

		round.Finish();
	}

	[Fact]
	public void TheTableTakesWhatTheLevelAsksForUpToWhatThePlayerHolds()
	{
		RunState run = Run(Rank.Ten, Rank.Nine, Rank.Eight, Rank.Seven);
		var round = new Round(run);

		Assert.Equal(RunEconomy.WagerBounds(1), round.WagerBounds);

		// Once the bankroll is thinner than the table's maximum, it is the ceiling.
		run.Stake(run.Bankroll - 40);

		Assert.Equal((10, 40), round.WagerBounds);
		Assert.Throws<ArgumentOutOfRangeException>(() => round.Begin(45));
	}

	[Theory]
	[InlineData(5)]
	[InlineData(105)]
	public void TheTableRefusesAWagerOutsideItsBounds(int wager)
	{
		var round = new Round(Run(Rank.Ten, Rank.Nine, Rank.Eight, Rank.Seven));

		Assert.Throws<ArgumentOutOfRangeException>(() => round.Begin(wager));
	}

	[Fact]
	public void DealingTakesTheWagerAndOneOfTheLevelsHands()
	{
		RunState run = Run(Rank.Ten, Rank.Nine, Rank.Eight, Rank.Seven);

		Round round = Dealt(run);

		Assert.Equal(RoundPhase.PlayerTurn, round.Phase);
		Assert.Equal(RunState.BaseStartingBankroll - Wager, run.Bankroll);
		Assert.Equal(RunEconomy.HandsPerLevel - 1, run.HandsRemaining);
		Assert.Equal(18, round.Active.Total);
		Assert.Equal(16, round.Dealer.Total);
		Assert.True(round.HoleCardHidden);
	}

	[Fact]
	public void SurrenderIsNotOnOfferAtTheOpeningTable()
	{
		Round round = Dealt(Run(Rank.Ten, Rank.Nine, Rank.Six, Rank.Seven));

		Assert.Equal(new[] { Move.Hit, Move.Stand, Move.Double }, round.LegalMoves.ToArray());
	}

	[Fact]
	public void ADealerNaturalEndsTheHandBeforeThePlayerActs()
	{
		RunState run = Run(Rank.Ten, Rank.Ace, Rank.Nine, Rank.King);

		Round round = Dealt(run);

		Assert.Equal(RoundPhase.DealerTurn, round.Phase);
		Assert.Empty(round.LegalMoves);
		// The deal never exposes the hole card, not even when it ends the hand.
		Assert.True(round.HoleCardHidden);

		Resolve(round);

		Assert.Equal(Settlement.DealerBlackjack, round.Results[0].Settlement.Result);
		Assert.Equal(RunState.BaseStartingBankroll - Wager, run.Bankroll);
		// The hand still scores: only a bust scores nothing.
		Assert.Equal(HandOutcome.Stand17To20, round.Results[0].Score.Outcome);
		Assert.Equal(68, run.LevelScore);
	}

	[Fact]
	public void APlayerNaturalPaysThreeToTwoAndTheDealerNeverDraws()
	{
		RunState run = Run(Rank.Ace, Rank.Nine, Rank.King, Rank.Six);

		Round round = Dealt(run);

		Assert.Equal(RoundPhase.DealerTurn, round.Phase);
		Assert.False(round.DealerStep());

		round.Finish();

		Assert.Equal(Settlement.PlayerBlackjack, round.Results[0].Settlement.Result);
		Assert.Equal(RunState.BaseStartingBankroll + 37, run.Bankroll);
		Assert.Equal(15, round.Dealer.Total);
		Assert.Equal(1, run.WinStreak);
	}

	[Fact]
	public void BustingEndsTheHandAndTheDealerStaysPut()
	{
		RunState run = Run(Rank.Ten, Rank.Nine, Rank.Six, Rank.Seven, Rank.Ten);

		Round round = Dealt(run);
		round.Play(Move.Hit);

		Assert.Equal(RoundPhase.DealerTurn, round.Phase);
		Assert.True(round.Active.IsBust);
		Assert.False(round.DealerStep());

		round.Finish();

		Assert.Equal(Settlement.PlayerBust, round.Results[0].Settlement.Result);
		Assert.Equal(16, round.Dealer.Total);
		Assert.Equal(0, run.LevelScore);
		Assert.Equal(RunState.BaseStartingBankroll - Wager, run.Bankroll);
	}

	[Fact]
	public void StandingHandsOverToTheDealerWhoDrawsToSeventeen()
	{
		RunState run = Run(Rank.Ten, Rank.Nine, Rank.Nine, Rank.Six, Rank.Two);

		Round round = Dealt(run);
		round.Play(Move.Stand);

		Assert.Equal(RoundPhase.DealerTurn, round.Phase);
		Assert.True(round.HoleCardHidden);

		round.RevealHole();

		Assert.False(round.HoleCardHidden);
		Assert.True(round.DealerStep());
		Assert.False(round.DealerStep());

		round.Finish();

		Assert.Equal(17, round.Dealer.Total);
		Assert.Equal(Settlement.PlayerWin, round.Results[0].Settlement.Result);
		Assert.Equal(RunState.BaseStartingBankroll + Wager, run.Bankroll);
	}

	[Fact]
	public void DoublingStakesASecondWagerAndEndsTheHandOnOneCard()
	{
		RunState run = Run(Rank.Six, Rank.Ten, Rank.Five, Rank.Seven, Rank.Ten);

		Round round = Dealt(run);
		round.Play(Move.Double);

		Assert.Equal(RoundPhase.DealerTurn, round.Phase);
		Assert.Equal(Wager * 2, round.Hands[0].Wager);
		Assert.Equal(3, round.Hands[0].Cards.Count);

		Resolve(round);

		Assert.Equal(Settlement.PlayerWin, round.Results[0].Settlement.Result);
		Assert.Equal(RunState.BaseStartingBankroll + (Wager * 2), run.Bankroll);
		Assert.Equal(HandOutcome.TwentyOne, round.Results[0].Score.Outcome);
	}

	[Fact]
	public void SplittingMakesTwoHandsThatScoreOnTheirOwnAndNeitherIsANatural()
	{
		RunState run = Run(Rank.Ten, Rank.Nine, Rank.Ten, Rank.Seven, Rank.Ace, Rank.Ace, Rank.Ten);

		Round round = Dealt(run);
		round.Play(Move.Split);

		Assert.Equal(2, round.Hands.Count);
		Assert.All(round.Hands, hand =>
		{
			Assert.True(hand.FromSplit);
			Assert.Equal(21, hand.Total);
			Assert.False(hand.IsBlackjack);
			Assert.Equal(Wager, hand.Wager);
		});
		Assert.Equal(RunState.BaseStartingBankroll - (Wager * 2), run.Bankroll);

		Resolve(round);

		Assert.True(round.Dealer.IsBust);
		Assert.All(round.Results, result => Assert.Equal(Settlement.DealerBust, result.Settlement.Result));
		Assert.Equal(2, run.WinStreak);
		// Both hands are the same 21, so the second is worth more only because
		// the streak grew between them.
		Assert.True(round.Results[1].Score.Score > round.Results[0].Score.Score);
		Assert.Equal(RunState.BaseStartingBankroll + (Wager * 2), run.Bankroll);
	}

	[Fact]
	public void SplitAcesTakeOneCardEachAndTheTurnMovesOnByItself()
	{
		RunState run = Run(Rank.Ace, Rank.Ten, Rank.Ace, Rank.Seven, Rank.Nine, Rank.Eight);

		Round round = Dealt(run);
		round.Play(Move.Split);

		Assert.Equal(RoundPhase.DealerTurn, round.Phase);
		Assert.Equal(new[] { 20, 19 }, round.Hands.Select(hand => hand.Total).ToArray());

		Resolve(round);

		Assert.Equal(17, round.Dealer.Total);
		Assert.All(round.Results, result => Assert.Equal(Settlement.PlayerWin, result.Settlement.Result));
	}

	[Fact]
	public void ABustedSplitHandDoesNotStopTheDealerPlayingForTheOther()
	{
		RunState run = Run(
			Rank.Eight, Rank.Nine, Rank.Eight, Rank.Six, Rank.Ten, Rank.Ten, Rank.Ten, Rank.Two);

		Round round = Dealt(run);
		round.Play(Move.Split);
		round.Play(Move.Hit);

		Assert.True(round.Hands[0].IsBust);
		Assert.Equal(1, round.ActiveIndex);

		round.Play(Move.Stand);

		Assert.True(round.DealerStep());
		Resolve(round);

		Assert.Equal(17, round.Dealer.Total);
		Assert.Equal(Settlement.PlayerBust, round.Results[0].Settlement.Result);
		Assert.Equal(Settlement.PlayerWin, round.Results[1].Settlement.Result);
	}

	[Fact]
	public void APushLeavesTheWinStreakAloneAndALossEndsIt()
	{
		RunState run = Run(
			Rank.Ten, Rank.Nine, Rank.Nine, Rank.Six, Rank.Two,
			Rank.Ten, Rank.Ten, Rank.Nine, Rank.Nine,
			Rank.Ten, Rank.Ten, Rank.Six, Rank.Nine);

		Round won = Dealt(run);
		won.Play(Move.Stand);
		Resolve(won);
		Assert.Equal(1, run.WinStreak);

		Round pushed = Dealt(run);
		pushed.Play(Move.Stand);
		Resolve(pushed);

		Assert.Equal(Settlement.Push, pushed.Results[0].Settlement.Result);
		Assert.Equal(1, run.WinStreak);

		Round lost = Dealt(run);
		lost.Play(Move.Stand);
		Resolve(lost);

		Assert.Equal(Settlement.DealerWin, lost.Results[0].Settlement.Result);
		Assert.Equal(0, run.WinStreak);
	}

	[Fact]
	public void TheDealerWillNotShowTheHoleCardOnThePlayersTurn()
	{
		Round round = Dealt(Run(Rank.Ten, Rank.Nine, Rank.Six, Rank.Seven));

		Assert.Throws<InvalidOperationException>(round.RevealHole);
		Assert.True(round.HoleCardHidden);
	}

	[Fact]
	public void NothingSettlesWhileTheDealerStillHasCardsToDraw()
	{
		Round round = Dealt(Run(Rank.Ten, Rank.Nine, Rank.Nine, Rank.Six, Rank.Two));
		round.Play(Move.Stand);

		Assert.Throws<InvalidOperationException>(round.Finish);
	}

	[Fact]
	public void ThePlayerCannotActOutOfTurn()
	{
		var round = new Round(Run(Rank.Ten, Rank.Nine, Rank.Nine, Rank.Six));

		Assert.Throws<InvalidOperationException>(() => round.Play(Move.Hit));
		Assert.Throws<InvalidOperationException>(() => round.DealerStep());
	}
}
