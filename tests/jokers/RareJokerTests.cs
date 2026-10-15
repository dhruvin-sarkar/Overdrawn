using System;
using Overdrawn.Blackjack;
using Overdrawn.Cards;
using Overdrawn.Jokers;
using Overdrawn.Run;
using Overdrawn.Scoring;
using Xunit;
using static Overdrawn.Tests.Deals;

namespace Overdrawn.Tests;

/// The Rare jokers, each held to its line in the Content Reference.
public class RareJokerTests
{
	/// Ten, 2, 6 makes 18, and the 9 that followed broke it.
	private static Hand BustOnNine() => Opening(Rank.Ten, Rank.Two).Hit(Rank.Six).Hit(Rank.Nine);

	[Fact]
	public void BustInsuranceScoresTheFirstBustAsTheHandBeforeIt()
	{
		var insurance = new BustInsurance();

		ScoreBreakdown score = ScoreCalculator.Score(Played(BustOnNine(), Busted), new JokerRule[] { insurance });

		// Stood on 18: 33 Chips at x2.
		Assert.Equal(HandOutcome.Stand17To20, score.Outcome);
		Assert.Equal(66, score.Score);
		Assert.True(insurance.Spent);
	}

	[Fact]
	public void TheHandOnTheFeltKeepsTheCardThatBrokeIt()
	{
		Hand busted = BustOnNine();

		ScoreCalculator.Score(Played(busted, Busted), new JokerRule[] { new BustInsurance() });

		Assert.Equal(4, busted.Cards.Count);
		Assert.True(busted.IsBust);
	}

	[Fact]
	public void OnlyTheFirstBustEachLevelIsInsured()
	{
		var insurance = new BustInsurance();
		var rail = new JokerRule[] { insurance };
		ScoreCalculator.Score(Played(BustOnNine(), Busted), rail);

		Assert.Equal(0, ScoreCalculator.Score(Played(BustOnNine(), Busted), rail).Score);

		insurance.OnLevelStart(NewRun());
		Assert.Equal(66, ScoreCalculator.Score(Played(BustOnNine(), Busted), rail).Score);
	}

	[Fact]
	public void ASurrenderIsNotABustToInsure()
	{
		var insurance = new BustInsurance();
		Hand surrendered = Opening(Rank.Ten, Rank.Six);
		surrendered.Surrender();

		ScoreBreakdown score = ScoreCalculator.Score(
			Played(surrendered, new HandSettlement(Settlement.PlayerSurrender, -10)), new JokerRule[] { insurance });

		Assert.Equal(0, score.Score);
		Assert.False(insurance.Spent);
	}

	[Fact]
	public void ColdDeckStrikesTheFaceCardsTheMomentItArrives()
	{
		RunState run = NewRun();

		run.Acquire(new ColdDeck());

		Assert.Equal(40, run.Shoe.Deck.Count);
		Assert.DoesNotContain(run.Shoe.Deck, card => card.Rank is Rank.Jack or Rank.Queen or Rank.King);
	}

	[Fact]
	public void ColdDeckAddsFiveMultToEveryHand()
	{
		// 19 stands: 34 Chips at x2, +5.
		Assert.Equal(34 * 7, Score(Opening(Rank.Ten, Rank.Nine), new ColdDeck()).Score);
	}

	[Theory]
	[InlineData(0, 0)]
	[InlineData(1, 1)]
	[InlineData(2, 2)]
	[InlineData(3, 4)]
	[InlineData(4, 8)]
	[InlineData(10, 512)]
	[InlineData(15, 512)]
	public void PerfectStreakDoublesTheBonusWithEveryWin(int streak, int bonus)
	{
		int standard = Math.Min(streak, ScoreCalculator.MaxStreakBonus);

		Assert.Equal(bonus, new PerfectStreak().StreakBonus(streak, standard));
	}

	[Fact]
	public void APerfectStreakOfFourAddsEightMult()
	{
		ScoreBreakdown score = ScoreCalculator.Score(Played(Opening(Rank.Ten, Rank.Nine), winStreak: 4),
			new JokerRule[] { new PerfectStreak() });

		Assert.Equal(34 * 10, score.Score);
	}

	[Fact]
	public void APushBreaksAPerfectStreak()
	{
		RunState run = NewRun();
		run.Acquire(new PerfectStreak());
		run.RecordOutcome(Won);
		run.RecordOutcome(Won);

		run.RecordOutcome(Pushed);

		Assert.Equal(0, run.WinStreak);
	}

	[Fact]
	public void FiveCardsWithoutBustingGetEightyChipsAndFourMult()
	{
		Hand charlie = Opening(Rank.Two, Rank.Three).Hit(Rank.Two).Hit(Rank.Four).Hit(Rank.Three);

		// 14 stands: 14 Chips +80, at x1 +4.
		Assert.Equal(94 * 5, Score(charlie, new FiveCardCharlieKing()).Score);
	}

	[Fact]
	public void FourCardsAreNotACharlie()
	{
		Hand four = Opening(Rank.Two, Rank.Three).Hit(Rank.Two).Hit(Rank.Four);

		Assert.Null(new FiveCardCharlieKing().OnScore(Played(four)));
	}

	[Fact]
	public void VaultBreakerPutsDoublingAndSplittingBackOnABossTable()
	{
		var boss = new HouseRules { DoubleAllowed = false, SplitAllowed = false };

		HouseRules bent = new VaultBreaker().Bend(boss);

		Assert.True(bent.DoubleAllowed);
		Assert.True(bent.SplitAllowed);
	}

	[Fact]
	public void WhaleWatcherAddsAMultPerHundredAndFiftyHeld()
	{
		var joker = new WhaleWatcher();
		Hand hand = Opening(Rank.Ten, Rank.Nine);

		Assert.Null(joker.OnScore(Played(hand, bankroll: 149)));
		Assert.Equal(JokerTrigger.AddMult(1), joker.OnScore(Played(hand, bankroll: 150)));
		Assert.Equal(JokerTrigger.AddMult(2), joker.OnScore(Played(hand, bankroll: 449)));
	}

	[Fact]
	public void DebtDeterrentGrowsDebtByAFlatFiveInsteadOfCompounding()
	{
		var joker = new DebtDeterrent();

		Assert.Equal(220, RunEconomy.GrowDebt(200));
		Assert.Equal(205, joker.GrowDebt(200, RunEconomy.GrowDebt(200)));
		Assert.Equal(0, joker.GrowDebt(0, RunEconomy.GrowDebt(0)));
	}

	[Fact]
	public void WhaleWatcherCountsTheBankrollOnceTheTableHasPaid()
	{
		RunState run = Rigged(Rank.Ten, Rank.Ten, Rank.Nine, Rank.Seven);
		run.Collect(40);
		run.Acquire(new WhaleWatcher());

		HandResult result = Settled(run, Move.Stand).Results[0];

		// $140 less the stake is $120, short of $150. Paid, it is $160: 34 Chips at x2, +1 streak, +1 whale.
		Assert.Equal(160, run.Bankroll);
		Assert.Equal(34 * 4, result.Score.Score);
	}

	[Fact]
	public void MarkedDeckDealsTheHoleCardFaceUp()
	{
		RunState run = Rigged(Rank.Ten, Rank.Ten, Rank.Nine, Rank.Seven);
		run.Acquire(new MarkedDeck());
		var round = new Round(run);

		round.Begin(Wager);

		Assert.False(round.HoleCardHidden);
	}

	[Fact]
	public void MarkedDeckShowsADealerNaturalFromTheDeal()
	{
		RunState run = Rigged(Rank.Ten, Rank.Ace, Rank.Nine, Rank.King);
		run.Acquire(new MarkedDeck());
		var round = new Round(run);

		round.Begin(Wager);

		Assert.Equal(RoundPhase.DealerTurn, round.Phase);
		Assert.False(round.HoleCardHidden);
	}

	[Fact]
	public void SuitLoyaltyAddsThreeMultPerCardOfTheSwornSuit()
	{
		var loyalty = new SuitLoyalty();
		loyalty.Choose(Suit.Hearts);
		Hand hand = Opening(new PlayingCard(Suit.Hearts, Rank.Ten), new PlayingCard(Suit.Hearts, Rank.Two))
			.Hit(Rank.Five, Suit.Clubs);

		Assert.Equal(JokerTrigger.AddMult(6), loyalty.OnScore(Played(hand)));
		Assert.Null(loyalty.OnScore(Played(Opening(Rank.Ten, Rank.Nine))));
	}

	[Fact]
	public void SuitLoyaltyIsSwornOnceAndBeforeItJoinsTheRail()
	{
		RunState run = NewRun();
		Assert.Throws<InvalidOperationException>(() => run.Acquire(new SuitLoyalty()));
		Assert.Empty(run.Jokers);

		var loyalty = new SuitLoyalty();
		loyalty.Choose(Suit.Clubs);
		Assert.Throws<InvalidOperationException>(() => loyalty.Choose(Suit.Hearts));
	}

	[Fact]
	public void OvertimeAddsAHandToEveryLevel()
	{
		Assert.Equal(1, new Overtime().ExtraHands);
	}
}
