using System;
using Overdrawn.Blackjack;

namespace Overdrawn.Scoring;

/// The Chips and Mult a hand produced, and their product.
public readonly record struct ScoreBreakdown(HandOutcome Outcome, int Chips, double Mult, long Score);

/// <summary>
/// Turns a finished hand into Chips x Mult. It reads the hand and the money
/// settlement after the fact and changes neither: HandResolver has already
/// decided what actually happened at the table.
/// </summary>
public static class ScoreCalculator
{
	public const int MaxStreakBonus = 10;

	public static HandOutcome Classify(Hand hand)
	{
		if (hand.IsBust || hand.Surrendered)
		{
			return HandOutcome.Bust;
		}

		if (hand.IsBlackjack)
		{
			return HandOutcome.Blackjack;
		}

		if (hand.Total == 21)
		{
			return HandOutcome.TwentyOne;
		}

		return hand.Total >= 17 ? HandOutcome.Stand17To20 : HandOutcome.StandUnder17;
	}

	/// <param name="winStreak">Consecutive wins including this hand, zero if it lost.</param>
	public static ScoreBreakdown Score(Hand hand, HandSettlement settlement, int winStreak)
	{
		if (winStreak < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(winStreak), winStreak, "A streak cannot be negative.");
		}

		HandOutcome outcome = Classify(hand);
		if (outcome == HandOutcome.Bust)
		{
			return new ScoreBreakdown(outcome, 0, 0, 0);
		}

		int sum = hand.Total;
		(int chips, double mult) = outcome switch
		{
			HandOutcome.StandUnder17 => (sum, 1d),
			HandOutcome.Stand17To20 => (sum + 15, 2d),
			HandOutcome.TwentyOne => (sum + 40, 4d),
			HandOutcome.Blackjack => (sum + 100, 8d),
			_ => throw new ArgumentOutOfRangeException(nameof(hand), outcome, "Unscored outcome."),
		};

		mult += Math.Min(winStreak, MaxStreakBonus);
		if (settlement.DealerBusted)
		{
			mult += 1;
		}

		return new ScoreBreakdown(outcome, chips, mult, (long)(chips * mult));
	}
}
