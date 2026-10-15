using System;
using Overdrawn.Blackjack;
using Overdrawn.Bosses;
using Overdrawn.Cards;
using Overdrawn.Jokers;
using Overdrawn.Run;
using Overdrawn.Scoring;
using Xunit;
using static Overdrawn.Tests.Deals;

namespace Overdrawn.Tests;

/// Every boss held to its line in the Content Reference, and the rules for who
/// the house sends when.
public class BossTests
{
	private static readonly HouseRules Table = RunState.BaseTable;

	[Fact]
	public void TheHouseHitsSoftSeventeenAndPaysANaturalEvenMoney()
	{
		HouseRules rules = new TheHouse().Bend(Table);

		Assert.True(rules.DealerHitsSoft17);
		Assert.Equal(1.0, rules.BlackjackPayout);
		Assert.Equal(1.6, new TheHouse().TargetMultiplier);
	}

	[Fact]
	public void OlSixtyFivePaysSixToFiveAndNeverBetterThanTheTableAlreadyDoes()
	{
		Assert.Equal(1.2, new OlSixtyFive().Bend(Table).BlackjackPayout);
		Assert.Equal(1.0, new OlSixtyFive().Bend(Table with { BlackjackPayout = 1.0 }).BlackjackPayout);
	}

	[Fact]
	public void TheVaultShutsDoublingAndTheTwinsShutSplitting()
	{
		Assert.False(new TheVault().Bend(Table).DoubleAllowed);
		Assert.True(new TheVault().Bend(Table).SplitAllowed);
		Assert.False(new TheTwins().Bend(Table).SplitAllowed);
		Assert.True(new TheTwins().Bend(Table).DoubleAllowed);
	}

	[Fact]
	public void SharpEyesTakesEveryTie()
	{
		Assert.True(new SharpEyes().Bend(Table).DealerWinsTies);
		Assert.Equal(1.8, new SharpEyes().TargetMultiplier);
	}

	[Fact]
	public void TheWhaleTriplesTheMinimum()
	{
		Assert.Equal(30, new TheWhale().MinimumWager(10));
	}

	[Fact]
	public void HighStakesDoublesTheBaseChips()
	{
		Hand hand = Opening(Rank.Ten, Rank.Nine);

		ScoreBreakdown standard = ScoreCalculator.Score(Played(hand), Array.Empty<JokerRule>());
		ScoreBreakdown doubled = ScoreCalculator.Score(Played(hand) with { BaseChipsScale = new HighStakes().BaseChipsScale },
			Array.Empty<JokerRule>());

		// 19 stands: 34 base Chips at x2, or 68 at this table.
		Assert.Equal(68, standard.Score);
		Assert.Equal(136, doubled.Score);
		Assert.Equal(2.0, new HighStakes().TargetMultiplier);
	}

	[Fact]
	public void ColdShoulderStrikesTheAces()
	{
		var boss = new ColdShoulder();

		Assert.True(boss.Strikes(new PlayingCard(Suit.Hearts, Rank.Ace)));
		Assert.False(boss.Strikes(new PlayingCard(Suit.Hearts, Rank.King)));
	}

	[Fact]
	public void TheCollectorDoublesTheDebtAndClearingWipesIt()
	{
		var boss = new TheCollector();

		Assert.Equal(60, boss.DebtOnArrival(30));
		Assert.True(boss.WipesDebtOnClear);
	}

	[Fact]
	public void TheFinalTableIsTheHouseAndSharpEyesAtOnce()
	{
		HouseRules rules = new TheFinalTable().Bend(Table);

		Assert.True(rules.DealerHitsSoft17);
		Assert.Equal(1.0, rules.BlackjackPayout);
		Assert.True(rules.DealerWinsTies);
		Assert.Equal(2.5, new TheFinalTable().TargetMultiplier);
	}

	[Fact]
	public void NoMercyDealsByBothOfItsRules()
	{
		var boss = new NoMercy(new TheVault(), new TheWhale());

		Assert.False(boss.Bend(Table).DoubleAllowed);
		Assert.Equal(30, boss.MinimumWager(10));
		Assert.Equal(2.2, boss.TargetMultiplier);
		Assert.Throws<ArgumentException>(() => new NoMercy(new TheVault(), new TheVault()));
	}

	[Fact]
	public void NoMercyAlwaysPairsTwoDifferentSingleRules()
	{
		for (int seed = 0; seed < 200; seed++)
		{
			NoMercy boss = NoMercy.Draw(new Random(seed));

			Assert.NotEqual(boss.Rules[0].Id, boss.Rules[1].Id);
			Assert.All(boss.Rules, rule => Assert.Contains(rule.Id, BossRules.SingleRules));
		}
	}

	[Fact]
	public void EveryBossIsRegisteredUnderItsOwnId()
	{
		Assert.All(BossRules.Ids, id => Assert.Equal(id, BossRules.Create(id, new Random(1)).Id));
		Assert.Throws<ArgumentException>(() => BossRules.Create("no_such_boss", new Random(1)));
	}

	[Fact]
	public void TheFirstBossTablesOnlyDrawTheGentlerBosses()
	{
		Assert.Equal(new[] { "the_vault", "the_twins", "the_house", "ol_sixty_five" }, BossRules.DrawnAt(3));
		Assert.Contains("no_mercy", BossRules.DrawnAt(12));
	}

	[Fact]
	public void DebtAndTheLastTableAreTheOnlyWaysToMeetTheirBosses()
	{
		Assert.DoesNotContain(BossRules.Collector, BossRules.DrawnAt(17));
		Assert.DoesNotContain(BossRules.FinalTable, BossRules.DrawnAt(17));
	}
}
