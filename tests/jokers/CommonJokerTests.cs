using System.Linq;
using Overdrawn.Blackjack;
using Overdrawn.Cards;
using Overdrawn.Jokers;
using Overdrawn.Run;
using Overdrawn.Scoring;
using Xunit;
using static Overdrawn.Tests.Deals;

namespace Overdrawn.Tests;

/// The Common jokers, each held to its line in the Content Reference.
public class CommonJokerTests
{
	[Fact]
	public void SteadyHandAddsFourMultWhenNoAceIsPlayed()
	{
		// 19 stands: 34 Chips at x2, +4.
		Assert.Equal(34 * 6, Score(Opening(Rank.Ten, Rank.Nine), new SteadyHand()).Score);
	}

	[Fact]
	public void SteadyHandWantsNoAceAtAllWhateverItCounts()
	{
		var joker = new SteadyHand();

		Assert.Null(joker.OnScore(Played(Opening(Rank.Ace, Rank.Seven))));
		Assert.Null(joker.OnScore(Played(Opening(Rank.Ten, Rank.Six).Hit(Rank.Ace))));
	}

	[Fact]
	public void AceHighAddsEightChipsForAnAcePlayedAsEleven()
	{
		// Soft 18 stands: 33 Chips +8 at x2.
		Assert.Equal(41 * 2, Score(Opening(Rank.Ace, Rank.Seven), new AceHigh()).Score);
	}

	[Fact]
	public void OnlyOneAceCanEverBeEleven()
	{
		Assert.Equal(JokerTrigger.AddChips(AceHigh.Chips), new AceHigh().OnScore(Played(Opening(Rank.Ace, Rank.Ace))));
	}

	[Fact]
	public void AnAceForcedDownToOneIsNotAceHigh()
	{
		Assert.Null(new AceHigh().OnScore(Played(Opening(Rank.Ten, Rank.Six).Hit(Rank.Ace))));
	}

	[Fact]
	public void LowRollerAddsAMultPerCardValuedSixOrUnder()
	{
		var joker = new LowRoller();

		Assert.Equal(JokerTrigger.AddMult(2), joker.OnScore(Played(Opening(Rank.Two, Rank.Six))));
		Assert.Null(joker.OnScore(Played(Opening(Rank.Ten, Rank.Seven))));
	}

	[Fact]
	public void LowRollerCountsAnAceOnlyWhenItIsPlayedAsOne()
	{
		var joker = new LowRoller();

		Assert.Equal(JokerTrigger.AddMult(1), joker.OnScore(Played(Opening(Rank.Ace, Rank.Five))));
		Assert.Equal(JokerTrigger.AddMult(2), joker.OnScore(Played(Opening(Rank.Ten, Rank.Six).Hit(Rank.Ace))));
		Assert.Equal(JokerTrigger.AddMult(1), joker.OnScore(Played(Opening(Rank.Ace, Rank.Ace).Hit(Rank.Nine))));
	}

	[Fact]
	public void FaceValueAddsTenChipsPerFaceCard()
	{
		var joker = new FaceValue();

		Assert.Equal(JokerTrigger.AddChips(20), joker.OnScore(Played(Opening(Rank.King, Rank.Queen))));
		Assert.Null(joker.OnScore(Played(Opening(Rank.Ten, Rank.Nine))));
	}

	[Fact]
	public void SmallTimeAddsTwentyChipsToEveryHand()
	{
		// 16 stands: 16 Chips +20 at x1.
		Assert.Equal(36, Score(Opening(Rank.Ten, Rank.Six), new SmallTime()).Score);
	}

	[Fact]
	public void CarefulPlayerAddsThreeMultForStandingOnTheOpeningTwo()
	{
		// 19 stands: 34 Chips at x2, +3.
		Assert.Equal(34 * 5, Score(Opening(Rank.Ten, Rank.Nine).Standing(), new CarefulPlayer()).Score);
	}

	[Fact]
	public void ANaturalWasNeverHitSoItIsCareful()
	{
		Assert.Equal(JokerTrigger.AddMult(CarefulPlayer.Mult),
			new CarefulPlayer().OnScore(Played(Opening(Rank.Ace, Rank.King))));
	}

	[Fact]
	public void AHitOrADoubleIsNotCareful()
	{
		var joker = new CarefulPlayer();

		Assert.Null(joker.OnScore(Played(Opening(Rank.Ten, Rank.Two).Hit(Rank.Five).Standing())));
		Assert.Null(joker.OnScore(Played(Opening(Rank.Five, Rank.Six).Doubling(Rank.Nine))));
	}

	[Fact]
	public void BoldPlayAddsThreeMultForTwoHitsOrMore()
	{
		ScoreBreakdown score = Score(Opening(Rank.Ten, Rank.Two).Hit(Rank.Three).Hit(Rank.Four).Standing(), new BoldPlay());

		// 19 stands: 34 Chips at x2, +3 for the hits.
		Assert.Equal(170, score.Score);
		Assert.Equal(BoldPlay.Mult, score.Steps.Single(step => step.Kind == ScoreStepKind.Joker).AddMult);

		Hand thrice = Opening(Rank.Two, Rank.Three).Hit(Rank.Four).Hit(Rank.Five).Hit(Rank.Six);
		Assert.Equal(JokerTrigger.AddMult(BoldPlay.Mult), new BoldPlay().OnScore(Played(thrice)));
	}

	[Fact]
	public void OneHitOrADoubleIsNotBold()
	{
		var joker = new BoldPlay();

		Assert.Null(joker.OnScore(Played(Opening(Rank.Ten, Rank.Six).Hit(Rank.Two))));
		Assert.Null(joker.OnScore(Played(Opening(Rank.Ten, Rank.Nine))));
		Assert.Null(joker.OnScore(Played(Opening(Rank.Five, Rank.Six).Doubling(Rank.Ten))));
	}

	[Fact]
	public void TheTableCountsTheHitsThePlayerTook()
	{
		RunState run = Rigged(Rank.Two, Rank.Ten, Rank.Three, Rank.Seven, Rank.Nine, Rank.Six);
		run.Acquire(new BoldPlay());

		HandResult result = Settled(run, Move.Hit, Move.Hit, Move.Stand).Results[0];

		// Two hits to 20 against 17: 35 Chips at x2, +1 for the streak, +3 for the hits.
		Assert.Equal(Settlement.PlayerWin, result.Settlement.Result);
		Assert.Equal(210, result.Score.Score);
	}

	[Fact]
	public void EvenMoneyPaysTwoDollarsOnAPushAndOnlyAPush()
	{
		var joker = new EvenMoney();
		Hand hand = Opening(Rank.Ten, Rank.Nine);

		Assert.Equal(EvenMoney.Cash, joker.Payout(hand, Pushed));
		Assert.Equal(0, joker.Payout(hand, Won));
		Assert.Equal(0, joker.Payout(hand, Lost));
	}

	[Fact]
	public void APushAtTheTablePaysEvenMoneyBesideTheReturnedWager()
	{
		RunState run = Rigged(Rank.Ten, Rank.Ten, Rank.Nine, Rank.Nine);
		run.Acquire(new EvenMoney());
		int before = run.Bankroll;

		Settled(run, Move.Stand);

		Assert.Equal(before + EvenMoney.Cash, run.Bankroll);
	}

	[Fact]
	public void WarmUpAddsOneMultOnTheFirstThreeHandsOfALevel()
	{
		var joker = new WarmUp();
		Hand hand = Opening(Rank.Ten, Rank.Nine);

		Assert.Equal(JokerTrigger.AddMult(WarmUp.Mult), joker.OnScore(Played(hand, handNumber: 1)));
		Assert.Equal(JokerTrigger.AddMult(WarmUp.Mult), joker.OnScore(Played(hand, handNumber: 3)));
		Assert.Null(joker.OnScore(Played(hand, handNumber: 4)));
	}

	[Fact]
	public void TwoOfAKindAddsFifteenChipsForAMatchedOpeningPair()
	{
		// 16 stands: 16 Chips +15 at x1.
		Assert.Equal(31, Score(Opening(Rank.Eight, Rank.Eight), new TwoOfAKind()).Score);
	}

	[Fact]
	public void TwoOfAKindIsRankNotValueAndOnlyTheFirstTwoCards()
	{
		var joker = new TwoOfAKind();

		Assert.Null(joker.OnScore(Played(Opening(Rank.King, Rank.Queen))));
		Assert.Null(joker.OnScore(Played(Opening(Rank.Two, Rank.Three).Hit(Rank.Two))));
	}
}
