using System;
using System.Linq;
using Overdrawn.Blackjack;
using Overdrawn.Cards;
using Overdrawn.Run;
using Xunit;

namespace Overdrawn.Tests;

public class RunStateTests
{
	private static RunState Begin(float bankrollScale = 1f, params Rank[] without) =>
		RunState.Begin("house_deck", Shoe.StandardDeck(without), seed: 7, bankrollScale);

	[Fact]
	public void ARunOpensAtLevelOneWithEightHandsAndTheFirstTarget()
	{
		RunState run = Begin();

		Assert.Equal(1, run.Level);
		Assert.Equal(RunEconomy.FirstScoreTarget, run.ScoreTarget);
		Assert.Equal(RunEconomy.HandsPerLevel, run.HandsRemaining);
		Assert.Equal(0, run.LevelScore);
		Assert.Equal(0, run.WinStreak);
		Assert.Equal(0, run.Debt);
		Assert.False(run.IsBossLevel);
		Assert.False(run.LevelCleared);
	}

	[Fact]
	public void ARunOpensWithTheBaseBankroll()
	{
		Assert.Equal(RunState.BaseStartingBankroll, Begin().Bankroll);
	}

	[Fact]
	public void ADeckThatTradesMoneyForPowerOpensPoorer()
	{
		Assert.Equal(70, Begin(bankrollScale: 0.7f).Bankroll);
	}

	[Fact]
	public void ARunCannotOpenOnAWorthlessBankroll()
	{
		Assert.Throws<ArgumentOutOfRangeException>(() => Begin(bankrollScale: 0f));
	}

	[Fact]
	public void APulledRankLeavesTheDeckEntirely()
	{
		RunState run = Begin(1f, Rank.Four);

		Assert.Equal(48, run.Shoe.Deck.Count);
		Assert.DoesNotContain(run.Shoe.Deck, card => card.Rank == Rank.Four);
	}

	[Fact]
	public void PullingTheJacksTakesFourFaceCards()
	{
		Assert.Equal(48, Shoe.StandardDeck(new[] { Rank.Jack }).Count);
	}

	[Fact]
	public void TheSameSeedDealsTheSameCards()
	{
		var first = RunState.Begin("house_deck", Shoe.StandardDeck(), seed: 42);
		var second = RunState.Begin("house_deck", Shoe.StandardDeck(), seed: 42);

		Assert.Equal(
			Enumerable.Range(0, 10).Select(_ => first.Shoe.Draw()).ToArray(),
			Enumerable.Range(0, 10).Select(_ => second.Shoe.Draw()).ToArray());
	}

	[Fact]
	public void ALevelsOwnCardsNeverJoinTheRunsDeck()
	{
		RunState run = Begin();
		var variant = new[] { new PlayingCard(Suit.Spades, Rank.Ace) };

		run.Shoe.BeginLevel(variant);
		Assert.Equal(52, run.Shoe.Deck.Count);
		Assert.Equal(53, run.Shoe.Remaining);

		run.Shoe.EndLevel();
		Assert.Equal(52, run.Shoe.Deck.Count);
		Assert.Equal(52, run.Shoe.Remaining);
	}
}
