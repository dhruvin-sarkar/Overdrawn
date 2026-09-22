using Overdrawn.Blackjack;
using Overdrawn.Cards;
using Overdrawn.Scoring;
using Xunit;

namespace Overdrawn.Tests;

public class ScoreCalculatorTests
{
	private static readonly HandSettlement Won = new(Settlement.PlayerWin, 20);
	private static readonly HandSettlement DealerBusted = new(Settlement.DealerBust, 20);

	private static Hand Dealt(params Rank[] ranks)
	{
		var hand = new Hand(20);
		foreach (Rank rank in ranks)
		{
			hand.Deal(new PlayingCard(Suit.Spades, rank));
		}

		return hand;
	}

	[Fact]
	public void ABustScoresNothingAtAll()
	{
		Hand hand = Dealt(Rank.Ten, Rank.Six);
		hand.Take(new PlayingCard(Suit.Spades, Rank.Ten));

		ScoreBreakdown score = ScoreCalculator.Score(hand, new HandSettlement(Settlement.PlayerBust, -20), 0);

		Assert.Equal(HandOutcome.Bust, score.Outcome);
		Assert.Equal(0, score.Score);
	}

	[Fact]
	public void StandingUnderSeventeenScoresTheCardsAlone()
	{
		ScoreBreakdown score = ScoreCalculator.Score(Dealt(Rank.Ten, Rank.Six), Won, 0);

		Assert.Equal(HandOutcome.StandUnder17, score.Outcome);
		Assert.Equal(16, score.Chips);
		Assert.Equal(1, score.Mult);
		Assert.Equal(16, score.Score);
	}

	[Fact]
	public void StandingSeventeenToTwentyAddsFifteenChipsAndDoublesMult()
	{
		ScoreBreakdown score = ScoreCalculator.Score(Dealt(Rank.Ten, Rank.Nine), Won, 0);

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

		ScoreBreakdown score = ScoreCalculator.Score(hand, Won, 0);

		Assert.Equal(HandOutcome.TwentyOne, score.Outcome);
		Assert.Equal(61, score.Chips);
		Assert.Equal(4, score.Mult);
		Assert.Equal(244, score.Score);
	}

	[Fact]
	public void ANaturalIsWorthFarMoreThanTheSameTotalOnThreeCards()
	{
		ScoreBreakdown score = ScoreCalculator.Score(Dealt(Rank.Ace, Rank.King), Won, 0);

		Assert.Equal(HandOutcome.Blackjack, score.Outcome);
		Assert.Equal(121, score.Chips);
		Assert.Equal(8, score.Mult);
		Assert.Equal(968, score.Score);
	}

	[Fact]
	public void AWinStreakAddsOneMultPerWinAndStopsAtTen()
	{
		Assert.Equal(5, ScoreCalculator.Score(Dealt(Rank.Ten, Rank.Nine), Won, 3).Mult);
		Assert.Equal(12, ScoreCalculator.Score(Dealt(Rank.Ten, Rank.Nine), Won, 10).Mult);
		Assert.Equal(12, ScoreCalculator.Score(Dealt(Rank.Ten, Rank.Nine), Won, 40).Mult);
	}

	[Fact]
	public void WinningBecauseTheDealerBustedIsWorthAnExtraMult()
	{
		Assert.Equal(3, ScoreCalculator.Score(Dealt(Rank.Ten, Rank.Nine), DealerBusted, 0).Mult);
	}

	[Fact]
	public void ASurrenderedHandScoresLikeABust()
	{
		Hand hand = Dealt(Rank.Ten, Rank.Six);
		hand.Surrender();

		Assert.Equal(0, ScoreCalculator.Score(hand, new HandSettlement(Settlement.PlayerSurrender, -10), 0).Score);
	}
}
