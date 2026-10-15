using System;
using System.Linq;
using Overdrawn.Blackjack;
using Overdrawn.Cards;
using Overdrawn.Jokers;
using Overdrawn.Run;
using Overdrawn.Scoring;
using Xunit;
using static Overdrawn.Tests.Deals;

namespace Overdrawn.Tests;

/// The pipeline the jokers plug into: slot order, amplification, retriggers,
/// the run-level hooks, and the roster itself. Each joker's own numbers are
/// tested with the rest of its rarity.
public class JokerTests
{
	/// The worked example from the Systems Deep Dive: King, 6, 5 on a four-hand streak.
	private static ScoringHand KingSixFive() => Played(Opening(Rank.King, Rank.Six).Hit(Rank.Five), winStreak: 4);

	[Fact]
	public void TheDeepDiveExampleScoresSevenHundredAndTen()
	{
		ScoreBreakdown score = ScoreCalculator.Score(KingSixFive(), new JokerRule[] { new LowRoller(), new FaceValue() });

		Assert.Equal(71, score.Chips);
		Assert.Equal(10, score.Mult);
		Assert.Equal(710, score.Score);
	}

	[Fact]
	public void MultEngineAheadOfLowRollerDoublesItsMultAndNothingElse()
	{
		ScoreBreakdown ahead = ScoreCalculator.Score(KingSixFive(),
			new JokerRule[] { new MultEngine(), new LowRoller(), new FaceValue() });
		ScoreBreakdown behind = ScoreCalculator.Score(KingSixFive(),
			new JokerRule[] { new LowRoller(), new FaceValue(), new MultEngine() });

		Assert.Equal(852, ahead.Score);
		Assert.Equal(710, behind.Score);
	}

	[Fact]
	public void EveryJokerBeatNamesTheSlotItCameFrom()
	{
		ScoreBreakdown score = ScoreCalculator.Score(KingSixFive(),
			new JokerRule[] { new MultEngine(), new LowRoller(), new FaceValue() });

		Assert.Equal(
			new[] { ScoreStepKind.Base, ScoreStepKind.Streak, ScoreStepKind.Joker, ScoreStepKind.Joker },
			score.Steps.Select(step => step.Kind));
		Assert.Equal(new[] { -1, -1, 1, 2 }, score.Steps.Select(step => step.Slot));

		ScoreStep lowRoller = score.Steps[2];
		Assert.Equal(4, lowRoller.AddMult);
		Assert.Equal(12, lowRoller.Mult);
		ScoreStep faceValue = score.Steps[3];
		Assert.Equal(10, faceValue.AddChips);
		Assert.Equal(71, faceValue.Chips);
	}

	[Fact]
	public void AJokerWithNothingToSayAddsNoBeat()
	{
		ScoreBreakdown score = Score(Opening(Rank.Ten, Rank.Nine), new FaceValue());

		Assert.Single(score.Steps);
		Assert.Equal(68, score.Score);
	}

	[Fact]
	public void ABeatAddsChipsThenAddsMultThenMultipliesIt()
	{
		// 19 stands: 34 Chips at x2. The beat makes it 44 Chips at (2 + 2) x 3.
		ScoreBreakdown score = Score(Opening(Rank.Ten, Rank.Nine), new Fixed(new JokerTrigger(10, 2, 3)));

		Assert.Equal(44, score.Chips);
		Assert.Equal(12, score.Mult);
		Assert.Equal(528, score.Score);
		Assert.Equal(3, score.Steps[^1].TimesMult);
	}

	[Fact]
	public void AMultiplierMultipliesWhateverTheJokersToItsLeftHaveAdded()
	{
		var adds = new Fixed(JokerTrigger.AddMult(2));
		var times = new Fixed(JokerTrigger.TimesMult(2));

		// 19 stands at x2: adding 2 then doubling is x8, doubling then adding 2 is x6.
		Assert.Equal(34 * 8, Score(Opening(Rank.Ten, Rank.Nine), adds, times).Score);
		Assert.Equal(34 * 6, Score(Opening(Rank.Ten, Rank.Nine), times, adds).Score);
	}

	[Fact]
	public void RetriggersFromAcrossTheRailAddUp()
	{
		// Two echoes play Small Time three times over: 16 + 60 Chips at x1.
		ScoreBreakdown score = Score(Opening(Rank.Ten, Rank.Six), new Echo(), new Echo(), new SmallTime());

		Assert.Equal(76, score.Score);
		Assert.Equal(3, score.Steps.Count(step => step.Kind == ScoreStepKind.Joker));
	}

	[Fact]
	public void ABustScoresNothingWhateverTheRailSays()
	{
		ScoreBreakdown score = ScoreCalculator.Score(Played(Opening(Rank.Ten, Rank.Six).Hit(Rank.Ten), Busted),
			new JokerRule[] { new SmallTime() });

		Assert.Equal(0, score.Score);
		Assert.Empty(score.Steps);
	}

	// The roster.

	[Fact]
	public void EveryRuleIsRegisteredUnderItsOwnId()
	{
		Assert.All(JokerRules.Ids, id => Assert.Equal(id, JokerRules.Create(id).Id));
	}

	[Fact]
	public void EveryJokerOwnedIsItsOwnInstance()
	{
		Assert.NotSame(JokerRules.Create("card_counter"), JokerRules.Create("card_counter"));
	}

	[Fact]
	public void AnUnknownJokerIsRefused()
	{
		Assert.Throws<ArgumentException>(() => JokerRules.Create("no_such_joker"));
	}

	[Fact]
	public void EveryJokerCardOnDiskHasARuleAndEveryRuleHasACard()
	{
		Assert.Equal(JokerRules.Ids.Order(), Roster.IdsIn("jokers").Order());
	}

	// The rail.

	[Fact]
	public void TheRailFillsLeftToRightAndStopsAtItsSlots()
	{
		RunState run = NewRun(jokerSlots: 2);
		var first = new FaceValue();
		var second = new LowRoller();

		run.Acquire(first);
		run.Acquire(second);

		Assert.Equal(new JokerRule[] { first, second }, run.Jokers);
		Assert.Throws<InvalidOperationException>(() => run.Acquire(new SmallTime()));
	}

	[Fact]
	public void MovingAJokerReordersThePipeline()
	{
		RunState run = NewRun();
		var engine = new MultEngine();
		var lowRoller = new LowRoller();
		run.Acquire(lowRoller);
		run.Acquire(engine);

		run.MoveJoker(1, 0);

		Assert.Equal(new JokerRule[] { engine, lowRoller }, run.Jokers);
	}

	[Fact]
	public void ReleasingAJokerClosesTheGap()
	{
		RunState run = NewRun();
		var kept = new SmallTime();
		run.Acquire(new FaceValue());
		run.Acquire(kept);

		Assert.IsType<FaceValue>(run.Release(0));
		Assert.Equal(new JokerRule[] { kept }, run.Jokers);
	}

	[Fact]
	public void ARunNeedsAtLeastOneSlot()
	{
		Assert.Throws<ArgumentOutOfRangeException>(() => NewRun(jokerSlots: 0));
	}

	[Fact]
	public void AJokerSeesTheRunTheMomentItArrives()
	{
		RunState run = NewRun();
		var striker = new FaceStriker();

		run.Acquire(striker);

		Assert.Equal(12, striker.Struck);
		Assert.Equal(40, run.Shoe.Deck.Count);
	}

	[Fact]
	public void AJokerBendsTheTableRulesInSlotOrder()
	{
		RunState run = NewRun();
		Assert.False(run.Rules.SurrenderAllowed);

		run.Acquire(new SurrenderBender(allowed: true));
		Assert.True(run.Rules.SurrenderAllowed);
		Assert.False(run.TableRules.SurrenderAllowed);

		// The rightmost bender has the last word.
		run.Acquire(new SurrenderBender(allowed: false));
		Assert.False(run.Rules.SurrenderAllowed);
		run.MoveJoker(1, 0);
		Assert.True(run.Rules.SurrenderAllowed);
	}

	[Fact]
	public void APushLeavesTheStreakAloneUnlessAJokerSaysOtherwise()
	{
		RunState run = NewRun();
		run.RecordOutcome(Won);
		run.RecordOutcome(Pushed);
		Assert.Equal(1, run.WinStreak);

		run.Acquire(new PushCounts());
		run.RecordOutcome(Pushed);
		Assert.Equal(2, run.WinStreak);
	}

	[Fact]
	public void AShieldedLossKeepsTheStreakOnce()
	{
		RunState run = NewRun();
		run.Acquire(new OneShield());
		run.RecordOutcome(Won);
		run.RecordOutcome(Won);

		run.RecordOutcome(Lost);
		Assert.Equal(2, run.WinStreak);

		run.RecordOutcome(Lost);
		Assert.Equal(0, run.WinStreak);
	}

	[Fact]
	public void AShieldIsNotSpentOnALossWithNoStreakToKeep()
	{
		RunState run = NewRun();
		var shield = new OneShield();
		run.Acquire(shield);

		run.RecordOutcome(Lost);
		Assert.False(shield.Spent);

		run.RecordOutcome(Won);
		run.RecordOutcome(Lost);
		Assert.True(shield.Spent);
		Assert.Equal(1, run.WinStreak);
	}

	[Fact]
	public void DealingCountsTheHandsPlayed()
	{
		RunState run = NewRun();

		run.SpendHand();
		run.SpendHand();

		Assert.Equal(2, run.HandsPlayed);
		Assert.Equal(RunEconomy.HandsPerLevel - 2, run.HandsRemaining);
	}

	[Fact]
	public void ClearingTheLevelOutrightLiftsTheScoreToTheTarget()
	{
		RunState run = NewRun();
		run.AddScore(10);

		run.ClearLevel();

		Assert.True(run.LevelCleared);
		Assert.Equal(run.ScoreTarget, run.LevelScore);
	}

	[Fact]
	public void ThePeekIsWhateverTheRailGrants()
	{
		RunState run = NewRun();
		Assert.False(run.Peek);

		run.Acquire(new Peeker());

		Assert.True(run.Peek);
	}

	[Fact]
	public void SpendingTakesMoneyForGoodAndNeverMoreThanIsThere()
	{
		RunState run = NewRun();

		run.Spend(30);

		Assert.Equal(RunState.BaseStartingBankroll - 30, run.Bankroll);
		Assert.Throws<ArgumentOutOfRangeException>(() => run.Spend(0));
		Assert.Throws<InvalidOperationException>(() => run.Spend(run.Bankroll + 1));
	}

	// The round.

	[Fact]
	public void TheTableFiresTheRailLeftToRight()
	{
		long Scored(params JokerRule[] rail)
		{
			RunState run = Rigged(Rank.King, Rank.Ten, Rank.Nine, Rank.Seven);
			foreach (JokerRule joker in rail)
			{
				run.Acquire(joker);
			}

			return Settled(run, Move.Stand).Results[0].Score.Score;
		}

		// 19 against 17 is 34 Chips at x3 with the streak: +2 then x3 is x15, but x3 then +2 is x11.
		Assert.Equal(34 * 15, Scored(new Fixed(JokerTrigger.AddMult(2)), new Fixed(JokerTrigger.TimesMult(3))));
		Assert.Equal(34 * 11, Scored(new Fixed(JokerTrigger.TimesMult(3)), new Fixed(JokerTrigger.AddMult(2))));
	}

	[Fact]
	public void TheTableScoresAHandWithTheRail()
	{
		RunState run = Rigged(Rank.King, Rank.Ten, Rank.Nine, Rank.Seven);
		run.Acquire(new FaceValue());

		HandResult result = Settled(run, Move.Stand).Results[0];

		// 19 stands against 17: 34 Chips, +10 for the King, x2 Mult, +1 for the
		// one-hand streak the win has just started.
		Assert.Equal(Settlement.PlayerWin, result.Settlement.Result);
		Assert.Equal(44, result.Score.Chips);
		Assert.Equal(132, result.Score.Score);
		Assert.Equal(0, result.Score.Steps.Single(step => step.Kind == ScoreStepKind.Joker).Slot);
	}

	[Fact]
	public void TheRailSeesEveryHandBeforeItScoresBustOrNot()
	{
		RunState run = Rigged(Rank.King, Rank.Ten, Rank.Nine, Rank.Seven, Rank.Ten, Rank.Ten, Rank.Six, Rank.Seven, Rank.Ten);
		var tally = new Tally();
		run.Acquire(tally);

		// 19 against 17, and the hand it has just seen is worth a hundred Chips:
		// 134 Chips at x2, +1 for the streak.
		HandResult won = Settled(run, Move.Stand).Results[0];
		Assert.Equal(1, tally.Seen);
		Assert.Equal(402, won.Score.Score);

		HandResult bust = Settled(run, Move.Hit).Results[0];
		Assert.Equal(Settlement.PlayerBust, bust.Settlement.Result);
		Assert.Equal(2, tally.Seen);
		Assert.Equal(0, bust.Score.Score);
	}

	[Fact]
	public void AJokerThatPaysCashPaysItWhenTheTableDoes()
	{
		RunState run = Rigged(Rank.Ten, Rank.Ten, Rank.Nine, Rank.Nine);
		run.Acquire(new PushPays());
		int before = run.Bankroll;

		Settled(run, Move.Stand);

		Assert.Equal(before + 3, run.Bankroll);
	}

	[Fact]
	public void AJokerCanOverruleTheTableAndTheMoneyFollows()
	{
		RunState run = Rigged(Rank.Ten, Rank.Ten, Rank.Six, Rank.Nine);
		run.Acquire(new LossIsAPush());
		int before = run.Bankroll;

		Round round = Settled(run, Move.Stand);

		Assert.Equal(Settlement.Push, round.Results[0].Settlement.Result);
		Assert.Equal(before, run.Bankroll);
		Assert.Equal(0, run.WinStreak);
	}

	[Fact]
	public void AJokerCanClearTheLevelOutright()
	{
		RunState run = Rigged(Rank.Ten, Rank.Ten, Rank.Nine, Rank.Seven);
		run.Acquire(new AlwaysClears());

		Settled(run, Move.Stand);

		Assert.True(run.LevelCleared);
	}

	/// Plays the same beat on every hand.
	private sealed class Fixed(JokerTrigger trigger) : JokerRule
	{
		public override string Id => "test_fixed";
		public override JokerTrigger? OnScore(ScoringHand hand) => trigger;
	}

	/// Plays every beat once more.
	private sealed class Echo : JokerRule
	{
		public override string Id => "test_echo";
		public override int Retriggers(ScoringHand hand) => 1;
	}

	/// Counts the hands it has seen and scores a hundred Chips for each.
	private sealed class Tally : JokerRule
	{
		public override string Id => "test_tally";
		public int Seen { get; private set; }
		public override void OnSettled(ScoringHand hand) => Seen++;
		public override JokerTrigger? OnScore(ScoringHand hand) => JokerTrigger.AddChips(Seen * 100);
	}

	private sealed class FaceStriker : JokerRule
	{
		public override string Id => "test_striker";
		public int Struck { get; private set; }

		public override void OnAcquired(RunState run) =>
			Struck = run.Shoe.RemoveFromDeck(card => card.Rank is Rank.Jack or Rank.Queen or Rank.King);
	}

	private sealed class SurrenderBender(bool allowed) : JokerRule
	{
		public override string Id => "test_bender";
		public override HouseRules Bend(HouseRules rules) => rules with { SurrenderAllowed = allowed };
	}

	private sealed class PushCounts : JokerRule
	{
		public override string Id => "test_push_counts";
		public override bool PushExtendsStreak => true;
	}

	private sealed class OneShield : JokerRule
	{
		public override string Id => "test_shield";
		public bool Spent { get; private set; }

		public override bool ShieldLoss()
		{
			if (Spent)
			{
				return false;
			}

			Spent = true;
			return true;
		}
	}

	private sealed class Peeker : JokerRule
	{
		public override string Id => "test_peeker";
		public override bool Peek => true;
	}

	private sealed class PushPays : JokerRule
	{
		public override string Id => "test_push_pays";
		public override int Payout(Hand hand, HandSettlement settlement) => settlement.Result == Settlement.Push ? 3 : 0;
	}

	private sealed class LossIsAPush : JokerRule
	{
		public override string Id => "test_loss_is_push";

		public override HandSettlement Overrule(HandSettlement settlement, Hand hand) =>
			settlement.Result == Settlement.DealerWin ? new HandSettlement(Settlement.Push, 0) : settlement;
	}

	private sealed class AlwaysClears : JokerRule
	{
		public override string Id => "test_clears";
		public override bool ClearsLevel(ScoringHand hand) => true;
	}
}
