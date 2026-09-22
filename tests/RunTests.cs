using System;
using System.Collections.Generic;
using System.Linq;
using Overdrawn.Blackjack;
using Overdrawn.Cards;
using Overdrawn.Run;
using Xunit;

namespace Overdrawn.Tests;

public class ShoeTests
{
	private static readonly PlayingCard Joker = new(Suit.Hearts, Rank.Ace);

	private static Shoe Standard() => new(Shoe.StandardDeck(), new Random(7));

	[Fact]
	public void AStandardDeckHoldsEveryCardOnce()
	{
		List<PlayingCard> deck = Shoe.StandardDeck();

		Assert.Equal(52, deck.Count);
		Assert.Equal(52, deck.Distinct().Count());
	}

	[Fact]
	public void DrawingWorksThroughTheWholeShoeBeforeRepeating()
	{
		Shoe shoe = Standard();
		var drawn = new List<PlayingCard>();

		for (int i = 0; i < 52; i++)
		{
			drawn.Add(shoe.Draw());
		}

		Assert.Equal(52, drawn.Distinct().Count());
		Assert.Equal(0, shoe.Remaining);
	}

	[Fact]
	public void AnEmptyShoeReshufflesItselfOnTheNextDraw()
	{
		Shoe shoe = Standard();
		for (int i = 0; i < 52; i++)
		{
			shoe.Draw();
		}

		shoe.Draw();

		Assert.Equal(51, shoe.Remaining);
	}

	[Fact]
	public void ALevelVariantNeverJoinsThePlayersDeck()
	{
		Shoe shoe = Standard();

		shoe.BeginLevel(Enumerable.Repeat(Joker, 4));

		Assert.Equal(52, shoe.Deck.Count);
		Assert.Equal(56, shoe.Remaining);
	}

	[Fact]
	public void EndingALevelTakesTheTemporaryCardsBackOut()
	{
		Shoe shoe = Standard();
		shoe.BeginLevel(Enumerable.Repeat(Joker, 4));

		shoe.EndLevel();

		Assert.Empty(shoe.Temporary);
		Assert.Equal(52, shoe.Remaining);
	}

	[Fact]
	public void AThinShoeHoldsOnlyItsCap()
	{
		Shoe shoe = Standard();

		shoe.BeginLevel(Array.Empty<PlayingCard>(), cap: 26);

		Assert.Equal(26, shoe.Remaining);
	}

	[Fact]
	public void ACardAddedMidLevelIsTemporaryToo()
	{
		Shoe shoe = Standard();
		shoe.BeginLevel(Array.Empty<PlayingCard>());

		shoe.AddTemporary(Joker);

		Assert.Equal(53, shoe.Remaining);
		Assert.Equal(52, shoe.Deck.Count);

		shoe.EndLevel();

		Assert.Equal(52, shoe.Remaining);
	}
}
public class RunEconomyTests
{
	[Theory]
	[InlineData(1, false)]
	[InlineData(3, true)]
	[InlineData(6, true)]
	[InlineData(17, false)]
	[InlineData(18, true)]
	public void EveryThirdLevelIsABoss(int level, bool boss)
	{
		Assert.Equal(boss, RunEconomy.IsBossLevel(level));
	}

	[Fact]
	public void AStandardLevelAsksForFortyPercentMoreThanTheLast()
	{
		Assert.Equal(420, RunEconomy.NextScoreTarget(RunEconomy.FirstScoreTarget, RunEconomy.StandardTargetGrowth));
		Assert.Equal(588, RunEconomy.NextScoreTarget(420, RunEconomy.StandardTargetGrowth));
	}

	[Fact]
	public void ABossAsksForItsOwnMultiplierInstead()
	{
		Assert.Equal(941, RunEconomy.NextScoreTarget(588, 1.6));
	}

	[Fact]
	public void WagerBoundsStartAtTenAndAHundred()
	{
		Assert.Equal((10, 100), RunEconomy.WagerBounds(1));
	}

	[Fact]
	public void WagerBoundsGrowFifteenPercentALevelAndLandOnFives()
	{
		(int minimum, int maximum) = RunEconomy.WagerBounds(5);

		Assert.Equal(15, minimum);
		Assert.Equal(175, maximum);
		Assert.Equal(0, minimum % RunEconomy.WagerStep);
		Assert.Equal(0, maximum % RunEconomy.WagerStep);
	}

	[Fact]
	public void LevelsStartAtOne()
	{
		Assert.Throws<ArgumentOutOfRangeException>(() => RunEconomy.WagerBounds(0));
	}

	[Fact]
	public void InterestPaysADollarPerTenHeldAndStopsAtFifty()
	{
		Assert.Equal(0, RunEconomy.Interest(9));
		Assert.Equal(12, RunEconomy.Interest(125));
		Assert.Equal(50, RunEconomy.Interest(500));
		Assert.Equal(50, RunEconomy.Interest(9000));
		Assert.Equal(25, RunEconomy.Interest(9000, cap: 25));
	}

	[Fact]
	public void ClearingEarlyPaysForTheHandsYouDidNotNeed()
	{
		Assert.Equal(25, RunEconomy.LevelPayout(0, boss: false));
		Assert.Equal(40, RunEconomy.LevelPayout(3, boss: false));
		Assert.Equal(90, RunEconomy.LevelPayout(3, boss: true));
	}

	[Fact]
	public void ALoanIsThreeMinimumWagers()
	{
		Assert.Equal(30, RunEconomy.LoanFor(1));
		Assert.Equal(45, RunEconomy.LoanFor(5));
	}

	[Fact]
	public void DebtCompoundsAtTenPercentAndRoundsAgainstYou()
	{
		Assert.Equal(0, RunEconomy.GrowDebt(0));
		Assert.Equal(33, RunEconomy.GrowDebt(30));
		Assert.Equal(37, RunEconomy.GrowDebt(33));
	}

	[Fact]
	public void TheCollectorComesOnceDebtPassesThreeTimesTheLoan()
	{
		Assert.False(RunEconomy.CollectorDue(90, 30));
		Assert.True(RunEconomy.CollectorDue(91, 30));
	}
}
