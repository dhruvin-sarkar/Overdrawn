using System.Linq;
using Overdrawn.Blackjack;
using Overdrawn.Cards;
using Overdrawn.Jokers;
using Overdrawn.Run;
using Overdrawn.Scoring;
using Xunit;
using static Overdrawn.Tests.Deals;

namespace Overdrawn.Tests;

/// The Uncommon jokers, each held to its line in the Content Reference.
public class UncommonJokerTests
{
	[Fact]
	public void CardCounterCountsAHandBeforeItScoresIt()
	{
		var counter = new CardCounter();
		ScoringHand hand = Played(Opening(Rank.King, Rank.Six).Hit(Rank.Five));
		Assert.Null(counter.OnScore(hand));

		counter.OnSettled(hand);

		Assert.Equal(2, counter.Counted);
		Assert.Equal(JokerTrigger.AddChips(2), counter.OnScore(hand));
	}

	[Fact]
	public void CardCounterCountsTheLowCardsOfABustToo()
	{
		var counter = new CardCounter();

		counter.OnSettled(Played(Opening(Rank.Six, Rank.Ten).Hit(Rank.Six), Busted));

		Assert.Equal(2, counter.Counted);
	}

	[Fact]
	public void CardCounterStopsAtFifty()
	{
		var counter = new CardCounter();
		ScoringHand hand = Played(Opening(Rank.Two, Rank.Three, Rank.Four, Rank.Five, Rank.Six));

		for (int i = 0; i < 20; i++)
		{
			counter.OnSettled(hand);
		}

		Assert.Equal(CardCounter.Cap, counter.Counted);
	}

	[Fact]
	public void AtTheTableTheHandBeingCountedAlreadyScoresItsCount()
	{
		RunState run = Rigged(Rank.Five, Rank.Ten, Rank.Six, Rank.Seven, Rank.Nine);
		run.Acquire(new CardCounter());

		HandResult result = Settled(run, Move.Hit, Move.Stand).Results[0];

		// 5, 6, 9 stands on 20 against 17: 35 Chips +2 counted, at x2 +1 for the streak.
		Assert.Equal(111, result.Score.Score);
	}

	[Fact]
	public void FlushTableAddsTwoMultPerCardOfTheMostCommonSuit()
	{
		Hand hand = Opening(new PlayingCard(Suit.Hearts, Rank.Ten), new PlayingCard(Suit.Hearts, Rank.Two))
			.Hit(Rank.Five, Suit.Clubs);

		// 17 stands: 32 Chips at x2, +4 for the two hearts.
		Assert.Equal(32 * 6, Score(hand, new FlushTable()).Score);
	}

	[Fact]
	public void EverySuitDifferentStillMakesAMajorityOfOne()
	{
		Hand hand = Opening(new PlayingCard(Suit.Spades, Rank.Ten), new PlayingCard(Suit.Hearts, Rank.Nine));

		Assert.Equal(JokerTrigger.AddMult(FlushTable.MultPerCard), new FlushTable().OnScore(Played(hand)));
	}

	[Fact]
	public void SplitDecisionPlaysEveryBeatTwiceOnTheSecondHalfOnly()
	{
		var jokers = new JokerRule[] { new SplitDecision(), new SmallTime(), new LowRoller() };

		ScoreBreakdown kept = ScoreCalculator.Score(Played(SplitHalf(Rank.Six, Rank.Ten), seat: 0), jokers);
		ScoreBreakdown dealt = ScoreCalculator.Score(Played(SplitHalf(Rank.Six, Rank.Ten), seat: 1), jokers);

		Assert.Equal((16 + 20) * 2, kept.Score);
		Assert.Equal((16 + 40) * 3, dealt.Score);
		Assert.Equal(2, kept.Steps.Count(step => step.Kind == ScoreStepKind.Joker));
		Assert.Equal(4, dealt.Steps.Count(step => step.Kind == ScoreStepKind.Joker));
	}

	[Fact]
	public void AnUnsplitHandIsNotRetriggered()
	{
		ScoreBreakdown score = ScoreCalculator.Score(Played(Opening(Rank.Ten, Rank.Six), seat: 1),
			new JokerRule[] { new SplitDecision(), new SmallTime() });

		Assert.Equal(36, score.Score);
	}

	[Fact]
	public void DoubleTroubleMultipliesADoubledWinByOneAndAHalf()
	{
		// 5, 6 doubled to 20: 35 Chips at x2, then x1.5.
		Assert.Equal(105, Score(Opening(Rank.Five, Rank.Six).Doubling(Rank.Nine), new DoubleTrouble()).Score);
	}

	[Fact]
	public void ADoubledLossOrAPlainWinIsNoTrouble()
	{
		var joker = new DoubleTrouble();

		Assert.Null(joker.OnScore(Played(Opening(Rank.Five, Rank.Six).Doubling(Rank.Nine), Lost)));
		Assert.Null(joker.OnScore(Played(Opening(Rank.Ten, Rank.Nine))));
	}

	[Fact]
	public void AtTheTableTheStreakIsAddedBeforeTheDoubleIsMultiplied()
	{
		RunState run = Rigged(Rank.Five, Rank.Ten, Rank.Six, Rank.Seven, Rank.Nine);
		run.Acquire(new DoubleTrouble());
		int before = run.Bankroll;

		HandResult result = Settled(run, Move.Double).Results[0];

		// 20 against 17: 35 Chips at (2 + 1) x 1.5, and the doubled wager pays double.
		Assert.Equal(4.5, result.Score.Mult);
		Assert.Equal(157, result.Score.Score);
		Assert.Equal(before + (2 * Wager), run.Bankroll);
	}

	[Fact]
	public void InsurancePolicyPaysThreeDollarsOnABustAndOnlyABust()
	{
		var policy = new InsurancePolicy();

		Assert.Equal(InsurancePolicy.Cash, policy.Payout(Opening(Rank.Ten, Rank.Six).Hit(Rank.Ten), Busted));
		Assert.Equal(0, policy.Payout(Opening(Rank.Ten, Rank.Nine), Won));
		Assert.Equal(0, policy.Payout(Opening(Rank.Ten, Rank.Six), Lost));

		Hand surrendered = Opening(Rank.Ten, Rank.Six);
		surrendered.Surrender();
		Assert.Equal(0, policy.Payout(surrendered, new HandSettlement(Settlement.PlayerSurrender, -10)));
	}

	[Fact]
	public void TheTableTakesTheWagerAndThePolicyPaysBesideIt()
	{
		RunState run = Rigged(Rank.Ten, Rank.Ten, Rank.Six, Rank.Seven, Rank.Ten);
		run.Acquire(new InsurancePolicy());
		int before = run.Bankroll;

		Round round = Settled(run, Move.Hit);

		Assert.Equal(Settlement.PlayerBust, round.Results[0].Settlement.Result);
		Assert.Equal(before - Wager + InsurancePolicy.Cash, run.Bankroll);
	}

	[Fact]
	public void EveryBustedHalfOfASplitIsPaid()
	{
		RunState run = Rigged(Rank.Eight, Rank.Ten, Rank.Eight, Rank.Seven, Rank.Ten, Rank.Ten, Rank.Ten, Rank.Ten);
		run.Acquire(new InsurancePolicy());
		int before = run.Bankroll;

		Round round = Settled(run, Move.Split, Move.Hit, Move.Hit);

		Assert.Equal(2, round.Results.Count);
		Assert.All(round.Results, result => Assert.Equal(Settlement.PlayerBust, result.Settlement.Result));
		Assert.Equal(before - (2 * Wager) + (2 * InsurancePolicy.Cash), run.Bankroll);
	}

	[Fact]
	public void HotStreakGrowsOnTheWinThatCompletesEachThree()
	{
		var joker = new HotStreak();
		Hand hand = Opening(Rank.Ten, Rank.Nine);

		for (int streak = 1; streak <= 6; streak++)
		{
			joker.OnSettled(Played(hand, winStreak: streak));
		}

		Assert.Equal(2, joker.Stacks);
		Assert.Equal(JokerTrigger.AddMult(2), joker.OnScore(Played(hand)));
	}

	[Fact]
	public void AStreakHeldStillIsNotCountedTwice()
	{
		var joker = new HotStreak();
		Hand hand = Opening(Rank.Ten, Rank.Nine);
		joker.OnSettled(Played(hand, winStreak: 1));
		joker.OnSettled(Played(hand, winStreak: 2));
		joker.OnSettled(Played(hand, winStreak: 3));

		joker.OnSettled(Played(hand, Lost, winStreak: 3));

		Assert.Equal(1, joker.Stacks);
	}

	[Fact]
	public void WinsFromBeforeHotStreakJoinedDoNotGrowIt()
	{
		RunState run = NewRun();
		for (int win = 0; win < HotStreak.WinsPerStack; win++)
		{
			run.RecordOutcome(Won);
		}

		var joker = new HotStreak();
		run.Acquire(joker);
		joker.OnSettled(Played(Opening(Rank.King, Rank.Nine), Pushed, winStreak: run.WinStreak));

		Assert.Equal(0, joker.Stacks);
	}

	[Fact]
	public void HotStreakKeepsItsMultOnceTheStreakBreaks()
	{
		var joker = new HotStreak();
		Hand hand = Opening(Rank.Ten, Rank.Nine);
		joker.OnSettled(Played(hand, winStreak: 1));
		joker.OnSettled(Played(hand, winStreak: 2));
		joker.OnSettled(Played(hand, winStreak: 3));

		joker.OnSettled(Played(hand, Lost, winStreak: 0));
		joker.OnSettled(Played(hand, winStreak: 1));

		Assert.Equal(1, joker.Stacks);
		Assert.Equal(JokerTrigger.AddMult(1), joker.OnScore(Played(hand, winStreak: 1)));
	}

	[Fact]
	public void HotStreakStopsAtTen()
	{
		var joker = new HotStreak();
		Hand hand = Opening(Rank.Ten, Rank.Nine);

		for (int streak = 1; streak <= 45; streak++)
		{
			joker.OnSettled(Played(hand, winStreak: streak));
		}

		Assert.Equal(HotStreak.Cap, joker.Stacks);
	}

	[Fact]
	public void TheThirdStraightWinAtTheTableScoresTheStackItEarned()
	{
		RunState run = Rigged(
			Rank.King, Rank.Ten, Rank.Nine, Rank.Seven,
			Rank.King, Rank.Ten, Rank.Nine, Rank.Seven,
			Rank.King, Rank.Ten, Rank.Nine, Rank.Seven);
		var joker = new HotStreak();
		run.Acquire(joker);

		Settled(run, Move.Stand);
		Settled(run, Move.Stand);
		HandResult third = Settled(run, Move.Stand).Results[0];

		// 19 against 17 on a three-win streak: 34 Chips at x2, +3 for the streak, +1 stack.
		Assert.Equal(1, joker.Stacks);
		Assert.Equal(34 * 6, third.Score.Score);
	}

	[Fact]
	public void PushProfiteerCountsAPushTowardTheStreak()
	{
		RunState run = NewRun();
		run.Acquire(new PushProfiteer());

		run.RecordOutcome(Won);
		run.RecordOutcome(Pushed);
		Assert.Equal(2, run.WinStreak);

		run.RecordOutcome(Lost);
		Assert.Equal(0, run.WinStreak);
	}

	[Fact]
	public void APushAtTheTableScoresWithTheStreakAWinWouldHaveEarned()
	{
		RunState run = Rigged(Rank.Ten, Rank.Ten, Rank.Nine, Rank.Nine);
		run.Acquire(new PushProfiteer());
		run.RecordOutcome(Won);
		int before = run.Bankroll;

		HandResult result = Settled(run, Move.Stand).Results[0];

		// 19 pushes against 19: 34 Chips at x2, +2 for the streak the push carried on.
		Assert.Equal(Settlement.Push, result.Settlement.Result);
		Assert.Equal(136, result.Score.Score);
		Assert.Equal(new[] { ScoreStepKind.Base, ScoreStepKind.Streak }, result.Score.Steps.Select(step => step.Kind));
		Assert.Equal(before, run.Bankroll);
	}

	[Fact]
	public void DeepPocketsRaisesTheInterestCapByAHundred()
	{
		int cap = RunEconomy.InterestCap + new DeepPockets().InterestCapBonus;

		Assert.Equal(50, RunEconomy.Interest(1000));
		Assert.Equal(100, RunEconomy.Interest(1000, cap));
		Assert.Equal(150, RunEconomy.Interest(9000, cap));
	}

	[Fact]
	public void QuickHandsFreesTheFirstPackReroll()
	{
		Assert.True(new QuickHands().FreeFirstPackReroll);
	}

	[Fact]
	public void StreakShieldCoversOneLossALevel()
	{
		RunState run = NewRun();
		run.Acquire(new StreakShield());
		run.RecordOutcome(Won);
		run.RecordOutcome(Won);

		run.RecordOutcome(Lost);
		Assert.Equal(2, run.WinStreak);

		run.RecordOutcome(Lost);
		Assert.Equal(0, run.WinStreak);
	}

	[Fact]
	public void ANewLevelRaisesTheShieldAgain()
	{
		RunState run = NewRun();
		var shield = new StreakShield();
		run.Acquire(shield);
		run.RecordOutcome(Won);
		run.RecordOutcome(Lost);
		Assert.True(shield.Spent);

		shield.OnLevelStart(run);
		run.RecordOutcome(Lost);

		Assert.Equal(1, run.WinStreak);
	}
}
