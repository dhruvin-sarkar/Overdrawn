using System;
using System.Linq;
using Overdrawn.Blackjack;
using Overdrawn.Cards;
using Overdrawn.Jokers;
using Overdrawn.Scoring;
using Xunit;

namespace Overdrawn.Tests;

public class ScoreCalculatorTests
{
	private static readonly HandSettlement Won = new(Settlement.PlayerWin, 20);
	private static readonly HandSettlement DealerBusted = new(Settlement.DealerBust, 20);
	private static readonly JokerRule[] NoJokers = Array.Empty<JokerRule>();

	private static Hand Dealt(params Rank[] ranks)
	{
		var hand = new Hand(20);
		foreach (Rank rank in ranks)
		{
			hand.Deal(new PlayingCard(Suit.Spades, rank));
		}

		return hand;
	}

	private static ScoreBreakdown Score(Hand hand, HandSettlement settlement, int winStreak = 0) =>
		ScoreCalculator.Score(new ScoringHand(hand, settlement, 0, winStreak, 1, 100), NoJokers);

	[Fact]
	public void ABustScoresNothingAtAll()
	{
		Hand hand = Dealt(Rank.Ten, Rank.Six);
		hand.Take(new PlayingCard(Suit.Spades, Rank.Ten));

		ScoreBreakdown score = Score(hand, new HandSettlement(Settlement.PlayerBust, -20));

		Assert.Equal(HandOutcome.Bust, score.Outcome);
		Assert.Equal(0, score.Score);
	}

	[Fact]
	public void StandingUnderSeventeenScoresTheCardsAlone()
	{
		ScoreBreakdown score = Score(Dealt(Rank.Ten, Rank.Six), Won);

		Assert.Equal(HandOutcome.StandUnder17, score.Outcome);
		Assert.Equal(16, score.Chips);
		Assert.Equal(1, score.Mult);
		Assert.Equal(16, score.Score);
	}

	[Fact]
	public void StandingSeventeenToTwentyAddsFifteenChipsAndDoublesMult()
	{
		ScoreBreakdown score = Score(Dealt(Rank.Ten, Rank.Nine), Won);

		Assert.Equal(HandOutcome.Stand17To20, score.Outcome);
		Assert.Equal(34, score.Chips);
		Assert.Equal(2, score.Mult);
		Assert.Equal(68, score.Score);
	}

	[Fact]
	public void TwentyOneOnThreeCardsIsWorthFourTimesMult()
	{
		Hand hand = Dealt(Rank.Seven, Rank.Four);
		hand.Take(new PlayingCard(Suit.Spades, Rank.Ten));

		ScoreBreakdown score = Score(hand, Won);

		Assert.Equal(HandOutcome.TwentyOne, score.Outcome);
		Assert.Equal(61, score.Chips);
		Assert.Equal(4, score.Mult);
		Assert.Equal(244, score.Score);
	}

	[Fact]
	public void ANaturalIsWorthFarMoreThanTheSameTotalOnThreeCards()
	{
		ScoreBreakdown score = Score(Dealt(Rank.Ace, Rank.King), Won);

		Assert.Equal(HandOutcome.Blackjack, score.Outcome);
		Assert.Equal(121, score.Chips);
		Assert.Equal(8, score.Mult);
		Assert.Equal(968, score.Score);
	}

	[Fact]
	public void AWinStreakAddsOneMultPerWinAndStopsAtTen()
	{
		Assert.Equal(5, Score(Dealt(Rank.Ten, Rank.Nine), Won, 3).Mult);
		Assert.Equal(12, Score(Dealt(Rank.Ten, Rank.Nine), Won, 10).Mult);
		Assert.Equal(12, Score(Dealt(Rank.Ten, Rank.Nine), Won, 40).Mult);
	}

	[Fact]
	public void AStreakCannotBeNegative()
	{
		Assert.Throws<ArgumentOutOfRangeException>(() => Score(Dealt(Rank.Ten, Rank.Nine), Won, -1));
	}

	[Fact]
	public void WinningBecauseTheDealerBustedIsWorthAnExtraMult()
	{
		Assert.Equal(3, Score(Dealt(Rank.Ten, Rank.Nine), DealerBusted).Mult);
	}

	[Fact]
	public void ASurrenderedHandScoresLikeABust()
	{
		Hand hand = Dealt(Rank.Ten, Rank.Six);
		hand.Surrender();

		Assert.Equal(0, Score(hand, new HandSettlement(Settlement.PlayerSurrender, -10)).Score);
	}

	[Fact]
	public void TheBeatsArriveInTheOrderTheyFiredAndLandOnTheFinalTotal()
	{
		ScoreBreakdown score = Score(Dealt(Rank.Ten, Rank.Nine), DealerBusted, 3);

		Assert.Equal(
			new[] { ScoreStepKind.Base, ScoreStepKind.Streak, ScoreStepKind.DealerBust },
			score.Steps.Select(step => step.Kind));
		Assert.Equal(new double[] { 2, 5, 6 }, score.Steps.Select(step => step.Mult));
		Assert.All(score.Steps, step => Assert.Equal(34, step.Chips));

		ScoreStep last = score.Steps[^1];
		Assert.Equal(score.Chips, last.Chips);
		Assert.Equal(score.Mult, last.Mult);
	}

	[Fact]
	public void ABustHasNoBeatsToPlay()
	{
		Hand hand = Dealt(Rank.Ten, Rank.Six);
		hand.Take(new PlayingCard(Suit.Spades, Rank.Ten));

		Assert.Empty(Score(hand, new HandSettlement(Settlement.PlayerBust, -20)).Steps);
	}
}
