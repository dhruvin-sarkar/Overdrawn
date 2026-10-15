using System;
using System.Linq;
using Overdrawn.Blackjack;
using Overdrawn.Bosses;
using Overdrawn.Cards;
using Overdrawn.Jokers;
using Overdrawn.Run;
using Overdrawn.Scoring;
using Xunit;
using static Overdrawn.Tests.Deals;

namespace Overdrawn.Tests;

/// A run from one table to the next: the cash-out, the next level, the boss
/// waiting at the end of each stretch, and the debt that follows the player.
public class LevelFlowTests
{
	/// Clears the level outright, takes the payout and moves on.
	private static void Advance(RunState run)
	{
		run.ClearLevel();
		run.CashOutLevel();
		run.BeginNextLevel();
	}

	private static void AdvanceTo(RunState run, int level)
	{
		while (run.Level < level)
		{
			Advance(run);
		}
	}

	/// A run sitting at the first boss table this boss can deal, found by seed
	/// so the boss is drawn the way a real run draws it.
	private static RunState Meeting(string bossId)
	{
		int bossLevel = Enumerable.Range(1, 6).Select(stretch => stretch * 3)
			.First(level => BossRules.DrawnAt(level).Contains(bossId));

		for (int seed = 0; seed < 10_000; seed++)
		{
			RunState run = RunState.Begin("house_deck", Shoe.StandardDeck(), seed);
			AdvanceTo(run, bossLevel - 2);
			if (run.UpcomingBoss.Id == bossId)
			{
				AdvanceTo(run, bossLevel);
				return run;
			}
		}

		throw new InvalidOperationException($"No seed sends {bossId}.");
	}

	/// Spends everything, then lets the house float the minimum, as many times over.
	private static void TakeLoans(RunState run, int count)
	{
		for (int loan = 0; loan < count; loan++)
		{
			run.Spend(run.Bankroll);
			run.CoverMinimumWager();
		}
	}

	[Fact]
	public void ARunSeesItsFirstBossComingFromTheFirstHand()
	{
		RunState run = NewRun();

		Assert.Null(run.Boss);
		Assert.Contains(run.UpcomingBoss.Id, BossRules.DrawnAt(3));
	}

	[Fact]
	public void AClearedLevelPaysForTheClearTheSpareHandsAndTheInterest()
	{
		RunState run = NewRun();
		run.SpendHand();
		run.SpendHand();
		run.ClearLevel();

		CashOut cashOut = run.CashOutLevel();

		Assert.Equal(new CashOut(Clear: 25, HandsLeft: 6, HandBonus: 30, BossBonus: 0, Interest: 10), cashOut);
		Assert.Equal(RunState.BaseStartingBankroll + 65, run.Bankroll);
	}

	[Fact]
	public void OnlyAClearedLevelPaysAndOnlyOnce()
	{
		RunState run = NewRun();
		Assert.Throws<InvalidOperationException>(() => run.CashOutLevel());

		run.ClearLevel();
		run.CashOutLevel();

		Assert.Throws<InvalidOperationException>(() => run.CashOutLevel());
	}

	[Fact]
	public void TheNextLevelAsksForMoreAndDealsAFreshSetOfHands()
	{
		RunState run = NewRun();
		run.RecordOutcome(Won);
		run.SpendHand();
		run.ClearLevel();
		Assert.Throws<InvalidOperationException>(() => run.BeginNextLevel());

		run.CashOutLevel();
		run.BeginNextLevel();

		Assert.Equal(2, run.Level);
		Assert.Equal(420, run.ScoreTarget);
		Assert.Equal(0, run.LevelScore);
		Assert.Equal(RunEconomy.HandsPerLevel, run.HandsRemaining);
		Assert.Equal(0, run.HandsPlayed);
		Assert.Equal(0, run.WinStreak);
		Assert.False(run.CashedOut);
	}

	[Fact]
	public void TheBossSeenComingIsTheOneThatSitsDown()
	{
		RunState run = NewRun();
		BossRule coming = run.UpcomingBoss;

		AdvanceTo(run, 3);

		Assert.Same(coming, run.Boss);
		Assert.True(run.IsBossLevel);
		Assert.Equal(RunEconomy.NextScoreTarget(420, coming.TargetMultiplier), run.ScoreTarget);
	}

	[Fact]
	public void EachStretchDrawsABossTheRunHasNotMet()
	{
		for (int seed = 0; seed < 50; seed++)
		{
			RunState run = RunState.Begin("house_deck", Shoe.StandardDeck(), seed);
			AdvanceTo(run, 3);
			string first = run.Boss!.Id;

			Advance(run);

			Assert.Null(run.Boss);
			Assert.NotEqual(first, run.UpcomingBoss.Id);
			Assert.Contains(run.UpcomingBoss.Id, BossRules.DrawnAt(6));
		}
	}

	[Fact]
	public void TheLastTableIsAlwaysTheFinalTableAndClearingItWinsTheRun()
	{
		RunState run = NewRun();
		AdvanceTo(run, RunEconomy.LevelsPerRun);

		Assert.Equal(BossRules.FinalTable, run.Boss!.Id);
		Assert.False(run.RunWon);

		run.ClearLevel();
		run.CashOutLevel();

		Assert.True(run.RunWon);
		Assert.Throws<InvalidOperationException>(() => run.BeginNextLevel());
	}

	[Fact]
	public void DebtGrowsAtTheStartOfEveryLevelItIsOwed()
	{
		RunState run = NewRun();
		TakeLoans(run, 1);

		Advance(run);

		Assert.Equal(33, run.Debt);
	}

	[Fact]
	public void DebtDeterrentGrowsItByAFlatFiveInstead()
	{
		RunState run = NewRun();
		run.Acquire(new DebtDeterrent());
		TakeLoans(run, 1);

		Advance(run);

		Assert.Equal(35, run.Debt);
	}

	[Fact]
	public void DebtPastThreeTimesTheLoanSendsForTheCollector()
	{
		RunState run = NewRun();
		TakeLoans(run, 4);
		Assert.Equal(120, run.Debt);
		Assert.Equal(BossRules.Collector, run.UpcomingBoss.Id);

		AdvanceTo(run, 3);

		// $120 grows to $132 and $146 as levels two and three begin, then doubles as The Collector sits down.
		Assert.Equal(BossRules.Collector, run.Boss!.Id);
		Assert.Equal(292, run.Debt);

		run.ClearLevel();
		run.CashOutLevel();

		Assert.Equal(0, run.Debt);
		Assert.Equal(0, run.OriginalLoan);
	}

	[Fact]
	public void TheCollectorNeverTakesTheFinalTable()
	{
		RunState run = NewRun();
		AdvanceTo(run, 16);

		TakeLoans(run, 4);

		Assert.True(RunEconomy.CollectorDue(run.Debt, run.OriginalLoan));
		Assert.Equal(BossRules.FinalTable, run.UpcomingBoss.Id);
	}

	[Fact]
	public void HouseAlwaysLosesSwitchesTheBossOffButNotTheTargetOrTheBonus()
	{
		RunState run = NewRun();
		run.Acquire(new HouseAlwaysLoses());
		BossRule boss = run.UpcomingBoss;

		AdvanceTo(run, 3);

		Assert.False(run.BossRulesInForce);
		Assert.Equal(RunState.BaseTable, run.Rules);
		Assert.Equal(RunEconomy.NextScoreTarget(420, boss.TargetMultiplier), run.ScoreTarget);
		run.ClearLevel();
		Assert.Equal(RunEconomy.BossClearBonus, run.CashOutLevel().BossBonus);
	}

	[Fact]
	public void VaultBreakerDoublesAtTheVaultAnyway()
	{
		RunState run = Meeting("the_vault");

		run.Acquire(new VaultBreaker());

		Assert.False(run.TableRules.DoubleAllowed);
		Assert.True(run.Rules.DoubleAllowed);
	}

	[Fact]
	public void TheWhaleTriplesTheTablesMinimum()
	{
		RunState run = Meeting("the_whale");
		(int minimum, int maximum) = RunEconomy.WagerBounds(run.Level);

		Assert.Equal((minimum * TheWhale.MinimumMultiple, maximum), run.TableLimits);
		Assert.Equal(minimum * TheWhale.MinimumMultiple, new Round(run).WagerBounds.Minimum);
	}

	[Fact]
	public void ColdShoulderKeepsTheAcesOutOfItsOwnShoeOnly()
	{
		RunState run = Meeting("cold_shoulder");

		run.SitDown();

		Assert.Equal(48, run.Shoe.Stock);
		Assert.Equal(52, run.Shoe.Deck.Count);
		Assert.DoesNotContain(Enumerable.Range(0, 48).Select(_ => run.Shoe.Draw()), card => card.Rank == Rank.Ace);

		Advance(run);
		run.SitDown();
		Assert.Equal(52, run.Shoe.Stock);
	}

	[Fact]
	public void HighStakesDoublesTheBaseChipsAtTheTable()
	{
		RunState run = Meeting("high_stakes");
		run.SitDown();
		var round = new Round(run);
		round.Begin(run.TableLimits.Minimum);
		if (round.Phase == RoundPhase.PlayerTurn)
		{
			round.Play(Move.Stand);
		}

		round.RevealHole();
		while (round.DealerStep())
		{
		}

		round.Finish();

		// Two cards stood on never bust, so the hand always has base Chips to double.
		HandResult result = round.Results[0];
		ScoreBreakdown ordinary = ScoreCalculator.Score(Played(result.Hand, result.Settlement), Array.Empty<JokerRule>());
		Assert.Equal(ordinary.Steps[0].Chips * 2, result.Score.Steps[0].Chips);
	}

	[Fact]
	public void OvertimeDealsAnExtraHandFromTheNextLevelOn()
	{
		RunState run = NewRun();
		run.Acquire(new Overtime());
		Assert.Equal(RunEconomy.HandsPerLevel, run.HandsThisLevel);

		Advance(run);

		Assert.Equal(RunEconomy.HandsPerLevel + 1, run.HandsThisLevel);
		Assert.Equal(RunEconomy.HandsPerLevel + 1, run.HandsRemaining);
	}

	[Fact]
	public void EveryJokerHasItsSayAsALevelBegins()
	{
		RunState run = NewRun();
		var bloodline = new BankrollToBloodline();
		run.Acquire(bloodline);

		Advance(run);

		// $100 and a $75 cash-out make $175, a tenth of which is $17.
		Assert.Equal(17, bloodline.Converted);
		Assert.Equal(158, run.Bankroll);
	}

	[Fact]
	public void ThePlayerSitsDownOncePerLevel()
	{
		RunState run = NewRun();
		run.SitDown();
		Assert.Throws<InvalidOperationException>(() => run.SitDown());

		Advance(run);

		run.SitDown();
	}

	[Fact]
	public void DebtIsPaidOffForExactlyWhatIsOwed()
	{
		RunState run = NewRun();
		Assert.Throws<InvalidOperationException>(() => run.PayOffDebt());
		TakeLoans(run, 1);

		run.PayOffDebt();

		Assert.Equal(0, run.Debt);
		Assert.Equal(0, run.OriginalLoan);
		Assert.Equal(0, run.Bankroll);
	}

	[Fact]
	public void DebtCannotBePaidWithMoneyThePlayerDoesNotHave()
	{
		RunState run = NewRun();
		TakeLoans(run, 1);
		run.Spend(10);

		Assert.Throws<InvalidOperationException>(() => run.PayOffDebt());
		Assert.Equal(30, run.Debt);
	}

	[Fact]
	public void DeepPocketsRaisesTheInterestTheCashOutPays()
	{
		RunState run = NewRun();
		run.Collect(900);
		run.Acquire(new DeepPockets());
		run.ClearLevel();

		Assert.Equal(100, run.CashOutLevel().Interest);
	}
}
