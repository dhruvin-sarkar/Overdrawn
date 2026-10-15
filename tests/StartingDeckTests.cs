using System;
using Overdrawn.Blackjack;
using Overdrawn.Cards;
using Overdrawn.Jokers;
using Overdrawn.Run;
using Overdrawn.Scoring;
using Xunit;
using static Overdrawn.Tests.Deals;

namespace Overdrawn.Tests;

/// What each starting deck does to a run besides its cards, held for the whole run.
public class StartingDeckTests
{
	private static RunState Dealt(DeckTerms terms) => RunState.Begin("deck", Shoe.StandardDeck(), seed: 7, terms);

	[Fact]
	public void TheHighRollersDeckRaisesBothWagerLimitsByHalf()
	{
		RunState run = Dealt(DeckTerms.House with { MinimumWagerScale = 1.5, MaximumWagerScale = 1.5 });

		Assert.Equal((15, 150), run.TableLimits);
	}

	[Fact]
	public void TheDecksPullOnTheLimitsIsAppliedBeforeRounding()
	{
		// Level five asks $17.49 to $174.90 before rounding, so half again is $26.24 to $262.35.
		Assert.Equal((25, 260), RunEconomy.WagerBounds(5, 1.5, 1.5));
	}

	[Fact]
	public void TheSplitDeckOpensWithTwoMoreSlotsAndALowerCeiling()
	{
		RunState run = Dealt(DeckTerms.House with { JokerSlots = RunEconomy.BaseJokerSlots + 2, MaximumWagerScale = 0.75 });

		Assert.Equal(RunEconomy.BaseJokerSlots + 2, run.JokerSlots);
		Assert.Equal((10, 75), run.TableLimits);
	}

	[Fact]
	public void TheInsuranceDeckSeesTheHoleCardAllRun()
	{
		RunState run = Rigged(DeckTerms.House with { Peek = true }, Rank.Ten, Rank.Six, Rank.Nine, Rank.Seven);
		var round = new Round(run);

		round.Begin(Wager);

		Assert.False(round.HoleCardHidden);
	}

	[Fact]
	public void TheInsuranceDeckHalvesTheInterestCapBeforeJokersRaiseIt()
	{
		RunState run = Dealt(DeckTerms.House with { InterestCapScale = 0.5 });
		Assert.Equal(25, run.InterestCap);

		run.Acquire(new DeepPockets());

		Assert.Equal(125, run.InterestCap);
	}

	[Fact]
	public void TheSuperstitionDeckAddsItsMultAsABeatOfItsOwn()
	{
		RunState run = Rigged(DeckTerms.House with { Mult = 1 }, Rank.Ten, Rank.Six, Rank.Nine, Rank.Seven);

		Round round = Settled(run, Move.Stand);

		// 19 stands for 34 Chips at x2, and the deck makes it x3.
		Assert.Equal(new ScoreStep(ScoreStepKind.Deck, 0, 1, 1, 34, 3), round.Results[0].Score.Steps[1]);
	}

	[Fact]
	public void TheCountersDeckHintsAtTheCardTheDealTakesFirst()
	{
		RunState run = Rigged(DeckTerms.House with { NextCardHint = true }, Rank.Four, Rank.Six, Rank.Nine, Rank.Seven);
		PlayingCard next = run.Shoe.Next;
		var round = new Round(run);

		round.Begin(Wager);

		Assert.Equal(next, round.Hands[0].Cards[0]);
		Assert.True(next.IsLow);
	}

	[Fact]
	public void AHintReadOffTheCutCardIsStillTheCardDealt()
	{
		var shoe = new Shoe(Shoe.StandardDeck(), new Random(7));
		while (shoe.Remaining > Shoe.CutCard)
		{
			shoe.Draw();
		}

		shoe.BeginHand();
		PlayingCard next = shoe.Next;
		shoe.BeginHand();

		Assert.Equal(next, shoe.Draw());
	}

	[Theory]
	[InlineData(Rank.Two, true)]
	[InlineData(Rank.Six, true)]
	[InlineData(Rank.Seven, false)]
	[InlineData(Rank.King, false)]
	[InlineData(Rank.Ace, false)]
	public void LowCardsRunFromTwoToSix(Rank rank, bool low)
	{
		Assert.Equal(low, new PlayingCard(Suit.Clubs, rank).IsLow);
	}

	[Fact]
	public void ADeckCanScaleTheTableButNeverZeroIt()
	{
		Assert.Throws<ArgumentOutOfRangeException>(() => Dealt(DeckTerms.House with { MaximumWagerScale = 0 }));
	}
}
