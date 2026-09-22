using System;
using System.Collections.Generic;
using System.Linq;
using Overdrawn.Blackjack;
using Overdrawn.Cards;
using Xunit;

namespace Overdrawn.Tests;

public class HandResolverTests
{
	private static Hand Dealt(int wager, params Rank[] ranks) => Build(new Hand(wager), ranks);

	private static Hand SplitHand(int wager, params Rank[] ranks) => Build(new Hand(wager, fromSplit: true), ranks);

	private static Hand Build(Hand hand, Rank[] ranks)
	{
		foreach (Rank rank in ranks)
		{
			hand.Deal(new PlayingCard(Suit.Spades, rank));
		}

		return hand;
	}

	private static PlayingCard Card(Rank rank) => new(Suit.Spades, rank);

	[Theory]
	[InlineData(false, false)]
	[InlineData(true, true)]
	public void DealerOnSoftSeventeenFollowsTheTable(bool hitsSoft17, bool draws)
	{
		Hand dealer = Dealt(0, Rank.Ace, Rank.Six);

		Assert.Equal(17, dealer.Total);
		Assert.True(dealer.IsSoft);
		Assert.Equal(draws, HandResolver.DealerDraws(dealer, new HouseRules { DealerHitsSoft17 = hitsSoft17 }));
	}

	[Fact]
	public void DealerStandsOnHardSeventeenAtEveryTable()
	{
		Hand dealer = Dealt(0, Rank.Ten, Rank.Seven);

		Assert.False(HandResolver.DealerDraws(dealer, HouseRules.Standard));
		Assert.False(HandResolver.DealerDraws(dealer, new HouseRules { DealerHitsSoft17 = true }));
	}

	[Fact]
	public void DealerDrawsBelowSeventeen()
	{
		Assert.True(HandResolver.DealerDraws(Dealt(0, Rank.Ten, Rank.Six), HouseRules.Standard));
		Assert.True(HandResolver.DealerDraws(Dealt(0, Rank.Ace, Rank.Five), HouseRules.Standard));
	}

	[Fact]
	public void AcesCountHighUntilTheyWouldBust()
	{
		Assert.Equal(12, Dealt(0, Rank.Ace, Rank.Ace).Total);
		Assert.Equal(21, Dealt(0, Rank.Ace, Rank.Ace, Rank.Nine).Total);

		Hand hand = Dealt(0, Rank.Ace, Rank.Five);
		hand.Take(Card(Rank.Ten));

		Assert.Equal(16, hand.Total);
		Assert.False(hand.IsSoft);
		Assert.False(hand.IsBust);
	}

	[Fact]
	public void BustLosesEvenWhenTheDealerAlsoBusts()
	{
		Hand hand = Dealt(20, Rank.Ten, Rank.Six);
		hand.Take(Card(Rank.Ten));
		Hand dealer = Dealt(0, Rank.Ten, Rank.Six);
		dealer.Take(Card(Rank.Ten));

		HandSettlement settlement = HandResolver.Settle(hand, dealer, HouseRules.Standard);

		Assert.Equal(Settlement.PlayerBust, settlement.Result);
		Assert.Equal(-20, settlement.Payout);
	}

	[Fact]
	public void NaturalPaysThreeToTwoAndRoundsTowardsTheHouse()
	{
		HandSettlement settlement = HandResolver.Settle(
			Dealt(25, Rank.Ace, Rank.King), Dealt(0, Rank.Ten, Rank.Eight), HouseRules.Standard);

		Assert.Equal(Settlement.PlayerBlackjack, settlement.Result);
		Assert.Equal(37, settlement.Payout);
	}

	[Fact]
	public void EvenMoneyTablePaysANaturalLikeAnyOtherWin()
	{
		HandSettlement settlement = HandResolver.Settle(
			Dealt(20, Rank.Ace, Rank.King), Dealt(0, Rank.Ten, Rank.Eight), new HouseRules { BlackjackPayout = 1.0 });

		Assert.Equal(20, settlement.Payout);
	}

	[Fact]
	public void TwoNaturalsPush()
	{
		HandSettlement settlement = HandResolver.Settle(
			Dealt(20, Rank.Ace, Rank.King), Dealt(0, Rank.Ace, Rank.Queen), HouseRules.Standard);

		Assert.Equal(Settlement.Push, settlement.Result);
		Assert.Equal(0, settlement.Payout);
	}

	[Fact]
	public void TwentyOneFromASplitIsNotANaturalAndLosesToOne()
	{
		Hand hand = SplitHand(20, Rank.Ace, Rank.King);

		Assert.False(hand.IsBlackjack);
		Assert.Equal(21, hand.Total);

		HandSettlement settlement = HandResolver.Settle(hand, Dealt(0, Rank.Ace, Rank.King), HouseRules.Standard);

		Assert.Equal(Settlement.DealerBlackjack, settlement.Result);
		Assert.Equal(-20, settlement.Payout);
	}

	[Fact]
	public void ADoubledHandRisksAndWinsTheDoubledWager()
	{
		Hand hand = Dealt(10, Rank.Six, Rank.Five);
		hand.DoubleDown();
		hand.Take(Card(Rank.Ten));

		Assert.Equal(21, hand.Total);
		Assert.True(hand.IsFinished);

		HandSettlement settlement = HandResolver.Settle(hand, Dealt(0, Rank.Ten, Rank.Ten), HouseRules.Standard);

		Assert.Equal(Settlement.PlayerWin, settlement.Result);
		Assert.Equal(20, settlement.Payout);
	}

	[Fact]
	public void ADoubledTwentyOnePushesADealerTwentyOne()
	{
		Hand hand = Dealt(10, Rank.Six, Rank.Five);
		hand.DoubleDown();
		hand.Take(Card(Rank.Ten));

		Hand dealer = Dealt(0, Rank.Seven, Rank.Four);
		dealer.Take(Card(Rank.Ten));

		HandSettlement settlement = HandResolver.Settle(hand, dealer, HouseRules.Standard);

		Assert.Equal(Settlement.Push, settlement.Result);
		Assert.Equal(0, settlement.Payout);
	}

	[Fact]
	public void SurrenderGivesUpHalfTheWagerAndTheHouseKeepsTheOddDollar()
	{
		Hand hand = Dealt(25, Rank.Ten, Rank.Six);
		hand.Surrender();

		HandSettlement settlement = HandResolver.Settle(hand, Dealt(0, Rank.Ten, Rank.Nine), HouseRules.Standard);

		Assert.Equal(Settlement.PlayerSurrender, settlement.Result);
		Assert.Equal(-12, settlement.Payout);
	}

	[Fact]
	public void SurrenderIsNotOfferedOnceTheDealerShowsANatural()
	{
		IReadOnlyList<Move> moves = HandResolver.LegalMoves(
			Dealt(20, Rank.Ten, Rank.Six), Dealt(0, Rank.Ace, Rank.King), 200, HouseRules.Standard, splitsUsed: 0);

		Assert.DoesNotContain(Move.Surrender, moves);
	}

	[Fact]
	public void InsurancePaysTwoToOneOnlyAgainstANatural()
	{
		Assert.Equal(20, HandResolver.SettleInsurance(10, Dealt(0, Rank.Ace, Rank.King), HouseRules.Standard));
		Assert.Equal(-10, HandResolver.SettleInsurance(10, Dealt(0, Rank.Ace, Rank.Nine), HouseRules.Standard));
	}

	[Fact]
	public void ATableWithoutInsuranceRefusesTheBet()
	{
		Assert.Throws<InvalidOperationException>(() => HandResolver.SettleInsurance(
			10, Dealt(0, Rank.Ace, Rank.King), new HouseRules { InsuranceAllowed = false }));
	}

	[Fact]
	public void ADealerThatTakesTiesTurnsAPushIntoALoss()
	{
		HandSettlement settlement = HandResolver.Settle(
			Dealt(20, Rank.Ten, Rank.Nine), Dealt(0, Rank.Ten, Rank.Nine), new HouseRules { DealerWinsTies = true });

		Assert.Equal(Settlement.DealerWin, settlement.Result);
		Assert.Equal(-20, settlement.Payout);
	}

	[Fact]
	public void TenValueCardsOfDifferentRanksSplit()
	{
		var hand = new Hand(20);
		hand.Deal(new PlayingCard(Suit.Spades, Rank.King));
		hand.Deal(new PlayingCard(Suit.Hearts, Rank.Queen));

		Assert.Contains(Move.Split, HandResolver.LegalMoves(
			hand, Dealt(0, Rank.Ten, Rank.Six), 100, HouseRules.Standard, splitsUsed: 0));
	}

	[Fact]
	public void AThinBankrollCannotDoubleOrSplit()
	{
		IReadOnlyList<Move> moves = HandResolver.LegalMoves(
			Dealt(50, Rank.Eight, Rank.Eight), Dealt(0, Rank.Ten, Rank.Six), 10, HouseRules.Standard, splitsUsed: 0);

		Assert.Equal(new[] { Move.Hit, Move.Stand, Move.Surrender }, moves.ToArray());
	}

	[Fact]
	public void SplittingStopsAtTheTableLimit()
	{
		var rules = new HouseRules { MaxSplits = 1 };
		Hand dealer = Dealt(0, Rank.Ten, Rank.Six);

		Assert.Contains(Move.Split, HandResolver.LegalMoves(Dealt(20, Rank.Eight, Rank.Eight), dealer, 100, rules, 0));
		Assert.DoesNotContain(Move.Split, HandResolver.LegalMoves(Dealt(20, Rank.Eight, Rank.Eight), dealer, 100, rules, 1));
	}

	[Fact]
	public void ATableThatBansDoubleAfterSplitStillAllowsItOnTheFirstHand()
	{
		var rules = new HouseRules { DoubleAfterSplit = false };
		Hand dealer = Dealt(0, Rank.Ten, Rank.Six);

		Assert.Contains(Move.Double, HandResolver.LegalMoves(Dealt(20, Rank.Six, Rank.Five), dealer, 100, rules, 0));
		Assert.DoesNotContain(Move.Double, HandResolver.LegalMoves(SplitHand(20, Rank.Six, Rank.Five), dealer, 100, rules, 1));
	}

	[Fact]
	public void SplittingTakesTheSecondCardOffTheHand()
	{
		var hand = new Hand(20);
		hand.Deal(new PlayingCard(Suit.Spades, Rank.Eight));
		hand.Deal(new PlayingCard(Suit.Hearts, Rank.Eight));

		PlayingCard moved = hand.SplitOff();

		Assert.Equal(new PlayingCard(Suit.Hearts, Rank.Eight), moved);
		Assert.Single(hand.Cards);
		Assert.Equal(8, hand.Total);
	}
}
