using System;
using Overdrawn.Blackjack;
using Overdrawn.Cards;
using Overdrawn.Jokers;
using Overdrawn.Run;
using Overdrawn.Scoring;
using Xunit;
using static Overdrawn.Tests.Deals;

namespace Overdrawn.Tests;

/// The Legendary jokers, each held to its line in the Content Reference.
public class LegendaryJokerTests
{
	[Fact]
	public void SevenSevenSevenClearsTheLevelAndNothingElseDoes()
	{
		var joker = new PerfectSevens();

		Assert.True(joker.ClearsLevel(Played(Opening(Rank.Seven, Rank.Seven).Hit(Rank.Seven))));
		Assert.False(joker.ClearsLevel(Played(Opening(Rank.Seven, Rank.Four).Hit(Rank.Seven).Hit(Rank.Three))));
		Assert.False(joker.ClearsLevel(Played(Opening(Rank.Seven, Rank.Seven))));
	}

	[Fact]
	public void ThreeCardsThatAreNotAllSevensDoNotClear()
	{
		var joker = new PerfectSevens();

		Assert.False(joker.ClearsLevel(Played(Opening(Rank.Six, Rank.Seven).Hit(Rank.Eight))));
		Assert.False(joker.ClearsLevel(Played(Opening(Rank.Seven, Rank.Two).Hit(Rank.Three))));
	}

	[Fact]
	public void SevenSevenSevenAtTheTableClearsALevelItsScoreWouldNot()
	{
		RunState run = Rigged(Rank.Seven, Rank.Ten, Rank.Seven, Rank.Four, Rank.Seven, Rank.Seven);
		run.Acquire(new PerfectSevens());

		Round round = Settled(run, Move.Hit);

		// 21 pushes against the dealer's 21: 61 Chips at x4, short of the 300 target.
		Assert.Equal(244, round.Results[0].Score.Score);
		Assert.True(run.LevelCleared);
	}

	[Fact]
	public void TheFirstHandTheHouseWinsAllRunIsAPush()
	{
		var joker = new TheHouseDoesntAlwaysWin();
		Hand hand = Opening(Rank.Ten, Rank.Six);

		Assert.Equal(Pushed, joker.Overrule(Lost, hand));
		Assert.True(joker.Spent);
		Assert.Equal(Lost, joker.Overrule(Lost, hand));
	}

	[Fact]
	public void ADealerNaturalIsALossTheHouseGivesBack()
	{
		var joker = new TheHouseDoesntAlwaysWin();

		Assert.Equal(Pushed, joker.Overrule(new HandSettlement(Settlement.DealerBlackjack, -Wager), Opening(Rank.Ten, Rank.Nine)));
	}

	[Fact]
	public void WinsAndPushesLeaveTheSaveForTheFirstLoss()
	{
		var joker = new TheHouseDoesntAlwaysWin();
		Hand hand = Opening(Rank.Ten, Rank.Nine);

		Assert.Equal(Won, joker.Overrule(Won, hand));
		Assert.Equal(Pushed, joker.Overrule(Pushed, hand));
		Assert.False(joker.Spent);
		Assert.Equal(Pushed, joker.Overrule(Lost, hand));
	}

	[Fact]
	public void ABustIsAHandTheHouseWonAndASurrenderIsNot()
	{
		var onBust = new TheHouseDoesntAlwaysWin();
		Assert.Equal(Pushed, onBust.Overrule(Busted, Opening(Rank.Ten, Rank.Six).Hit(Rank.Ten)));

		var onSurrender = new TheHouseDoesntAlwaysWin();
		var surrendered = new HandSettlement(Settlement.PlayerSurrender, -10);
		Assert.Equal(surrendered, onSurrender.Overrule(surrendered, Opening(Rank.Ten, Rank.Six)));
		Assert.False(onSurrender.Spent);
	}

	[Fact]
	public void AtTheTableTheWagerComesBackAndTheStreakStands()
	{
		RunState run = Rigged(Rank.Ten, Rank.Ten, Rank.Six, Rank.Nine);
		run.Acquire(new TheHouseDoesntAlwaysWin());
		run.RecordOutcome(Won);
		int before = run.Bankroll;

		Round round = Settled(run, Move.Stand);

		Assert.Equal(Settlement.Push, round.Results[0].Settlement.Result);
		Assert.Equal(before, run.Bankroll);
		Assert.Equal(1, run.WinStreak);
	}

	[Fact]
	public void MultEngineDoublesAMultiplierToItsRight()
	{
		Hand doubled = Opening(Rank.Five, Rank.Six).Doubling(Rank.Nine);

		// 5, 6 doubled to 20: 35 Chips at x2, then x1.5, or x3 once the engine sits to its left.
		Assert.Equal(210, Score(doubled, new MultEngine(), new DoubleTrouble()).Score);
		Assert.Equal(105, Score(doubled, new DoubleTrouble(), new MultEngine()).Score);
	}

	[Fact]
	public void BankrollToBloodlineTurnsATenthOfTheBankrollIntoMultEachLevel()
	{
		RunState run = NewRun();
		var joker = new BankrollToBloodline();

		joker.OnLevelStart(run);
		Assert.Equal(90, run.Bankroll);
		Assert.Equal(1, joker.Mult);

		joker.OnLevelStart(run);
		joker.OnLevelStart(run);

		// $10, then $9, then $8: $27 is two whole Mult, with $7 carried.
		Assert.Equal(73, run.Bankroll);
		Assert.Equal(27, joker.Converted);
		Assert.Equal(JokerTrigger.AddMult(2), joker.OnScore(Played(Opening(Rank.Ten, Rank.Nine))));
	}

	[Fact]
	public void UnderTenDollarsConvertsNothing()
	{
		RunState run = NewRun();
		run.Spend(run.Bankroll - 9);
		var joker = new BankrollToBloodline();

		joker.OnLevelStart(run);

		Assert.Equal(9, run.Bankroll);
		Assert.Null(joker.OnScore(Played(Opening(Rank.Ten, Rank.Nine))));
	}

	[Fact]
	public void NoSuchThingAsBustForfeitsHalfTheWager()
	{
		HandSettlement settlement = new NoSuchThingAsBust().Overrule(Busted, Opening(Rank.Ten, Rank.Six).Hit(Rank.Ten));

		Assert.Equal(new HandSettlement(Settlement.PlayerBust, -Wager / 2), settlement);
	}

	[Fact]
	public void ABustStillScoresAsALowHandOnTheCardsItBustedWith()
	{
		ScoreBreakdown score = ScoreCalculator.Score(Played(Opening(Rank.Ten, Rank.Six).Hit(Rank.Ten), Busted),
			new JokerRule[] { new NoSuchThingAsBust() });

		Assert.Equal(HandOutcome.StandUnder17, score.Outcome);
		Assert.Equal(26, score.Score);
	}

	[Fact]
	public void NoSuchThingAsBustOnlyCatchesABust()
	{
		var joker = new NoSuchThingAsBust();
		var surrendered = new HandSettlement(Settlement.PlayerSurrender, HandResolver.ForfeitHalf(Wager));
		Hand stoodOnSixteen = Opening(Rank.Ten, Rank.Six).Standing();

		Assert.Equal(Lost, joker.Overrule(Lost, stoodOnSixteen));
		Assert.Equal(surrendered, joker.Overrule(surrendered, Opening(Rank.Ten, Rank.Six)));

		Hand gaveUp = Opening(Rank.Ten, Rank.Six);
		gaveUp.Surrender();
		Assert.Equal(0, ScoreCalculator.Score(Played(gaveUp, surrendered), new JokerRule[] { joker }).Score);
	}

	[Fact]
	public void AtTheTableABustCostsHalfAndStillBreaksTheStreak()
	{
		RunState run = Rigged(Rank.Ten, Rank.Ten, Rank.Six, Rank.Seven, Rank.Ten);
		run.Acquire(new NoSuchThingAsBust());
		run.RecordOutcome(Won);
		int before = run.Bankroll;

		HandResult result = Settled(run, Move.Hit).Results[0];

		Assert.Equal(Settlement.PlayerBust, result.Settlement.Result);
		Assert.Equal(before - (Wager / 2), run.Bankroll);
		Assert.Equal(0, run.WinStreak);
		Assert.Equal(26, result.Score.Score);
	}

	[Fact]
	public void WhicheverJokerCatchesTheBustFirstScoresIt()
	{
		Hand busted = Opening(Rank.Ten, Rank.Two).Hit(Rank.Six).Hit(Rank.Nine);

		ScoreBreakdown lowHand = ScoreCalculator.Score(Played(busted, Busted),
			new JokerRule[] { new NoSuchThingAsBust(), new BustInsurance() });
		ScoreBreakdown stood = ScoreCalculator.Score(Played(busted, Busted),
			new JokerRule[] { new BustInsurance(), new NoSuchThingAsBust() });

		// 27 as a Low Hand at x1, or the 18 it was before the 9 at x2.
		Assert.Equal(27, lowHand.Score);
		Assert.Equal(66, stood.Score);
	}

	[Fact]
	public void ABustScoredAsALowHandIsStillNoCharlie()
	{
		Hand busted = Opening(Rank.Two, Rank.Three).Hit(Rank.Four).Hit(Rank.Five).Hit(Rank.Ten);

		ScoreBreakdown score = ScoreCalculator.Score(Played(busted, Busted),
			new JokerRule[] { new NoSuchThingAsBust(), new FiveCardCharlieKing() });

		Assert.Equal(24, score.Score);
	}

	[Fact]
	public void TensCompanySplitsAnyTwoTenValueCards()
	{
		RunState run = NewRun();
		Hand kingQueen = Opening(Rank.King, Rank.Queen);
		Hand dealer = Opening(Rank.Ten, Rank.Six);
		Assert.DoesNotContain(Move.Split, HandResolver.LegalMoves(kingQueen, dealer, 100, run.Rules, 0));

		run.Acquire(new TensCompany());

		Assert.Contains(Move.Split, HandResolver.LegalMoves(kingQueen, dealer, 100, run.Rules, 0));
	}

	[Fact]
	public void AtTheTableAKingAndAQueenSplitIntoTwoHands()
	{
		RunState run = Rigged(Rank.King, Rank.Ten, Rank.Queen, Rank.Seven, Rank.Nine, Rank.Eight);
		run.Acquire(new TensCompany());
		int before = run.Bankroll;

		Round round = Settled(run, Move.Split, Move.Stand, Move.Stand);

		// King, 9 and Queen, 8 both beat 17: 34 Chips at x3, then 33 Chips at x4.
		Assert.Equal(2, round.Results.Count);
		Assert.Equal(102, round.Results[0].Score.Score);
		Assert.Equal(132, round.Results[1].Score.Score);
		Assert.Equal(before + (2 * Wager), run.Bankroll);
	}

	[Fact]
	public void AllInLeavesADollarWhenTheWholeBankrollIsLost()
	{
		var joker = new AllIn();
		joker.Arm();

		HandSettlement settlement = joker.Overrule(new HandSettlement(Settlement.DealerWin, -100), new Hand(100));

		Assert.Equal(new HandSettlement(Settlement.DealerWin, -99), settlement);
		Assert.False(joker.Armed);
	}

	[Fact]
	public void AllInIsOncePerLevel()
	{
		var joker = new AllIn();

		joker.Arm();
		Assert.False(joker.AllowsAllIn);
		Assert.Throws<InvalidOperationException>(() => joker.Arm());

		joker.OnLevelStart(NewRun());
		Assert.True(joker.AllowsAllIn);
		joker.Arm();
		Assert.True(joker.Armed);
	}

	[Fact]
	public void OnlyTheArmedHandIsCaught()
	{
		var joker = new AllIn();
		var hand = new Hand(100);
		var lost = new HandSettlement(Settlement.DealerWin, -100);

		Assert.Equal(lost, joker.Overrule(lost, hand));

		joker.Arm();
		Assert.Equal(Won, joker.Overrule(Won, Opening(Rank.Ten, Rank.Nine)));
		Assert.Equal(lost, joker.Overrule(lost, hand));
	}

	[Fact]
	public void HouseAlwaysLosesSwitchesTheBossRuleOff()
	{
		Assert.True(new HouseAlwaysLoses().DisablesBossRule);
	}
}
