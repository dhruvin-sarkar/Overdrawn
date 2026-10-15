using System;
using System.Collections.Generic;
using Overdrawn.Blackjack;
using Overdrawn.Jokers;

namespace Overdrawn.Scoring;

/// The Chips and Mult a hand produced, their product, and the beats that got
/// there in the order they fired.
public readonly record struct ScoreBreakdown(
	HandOutcome Outcome, int Chips, double Mult, long Score, IReadOnlyList<ScoreStep> Steps);

/// <summary>
/// Turns a finished hand into Chips x Mult. It reads the hand and the money
/// settlement after the fact and changes neither: HandResolver has already
/// decided what actually happened at the table. Jokers fire strictly in slot
/// order, left to right, and the list handed in is that order.
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

	/// <param name="jokers">The rail in slot order. Each joker fires in turn, left to right.</param>
	public static ScoreBreakdown Score(ScoringHand hand, IReadOnlyList<JokerRule> jokers)
	{
		if (hand.WinStreak < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(hand), hand.WinStreak, "A streak cannot be negative.");
		}

		// A joker may change what the hand is before anything scores it.
		foreach (JokerRule joker in jokers)
		{
			hand = joker.Reframe(hand);
		}

		if (hand.Outcome == HandOutcome.Bust)
		{
			return new ScoreBreakdown(HandOutcome.Bust, 0, 0, 0, ScoreStep.None);
		}

		int sum = hand.Hand.Total;
		(int chips, double mult) = hand.Outcome switch
		{
			HandOutcome.StandUnder17 => (sum, 1d),
			HandOutcome.Stand17To20 => (sum + 15, 2d),
			HandOutcome.TwentyOne => (sum + 40, 4d),
			HandOutcome.Blackjack => (sum + 100, 8d),
			_ => throw new ArgumentOutOfRangeException(nameof(hand), hand.Outcome, "Unscored outcome."),
		};

		var steps = new List<ScoreStep> { new(ScoreStepKind.Base, chips, mult, 1, chips, mult) };

		int streak = Math.Min(hand.WinStreak, MaxStreakBonus);
		foreach (JokerRule joker in jokers)
		{
			streak = joker.StreakBonus(hand.WinStreak, streak);
		}

		if (streak > 0)
		{
			mult += streak;
			steps.Add(new ScoreStep(ScoreStepKind.Streak, 0, streak, 1, chips, mult));
		}

		if (hand.Settlement.DealerBusted)
		{
			mult += 1;
			steps.Add(new ScoreStep(ScoreStepKind.DealerBust, 0, 1, 1, chips, mult));
		}

		int plays = 1;
		foreach (JokerRule joker in jokers)
		{
			plays += joker.Retriggers(hand);
		}

		for (int slot = 0; slot < jokers.Count; slot++)
		{
			if (jokers[slot].OnScore(hand) is not { } trigger)
			{
				continue;
			}

			// Every other joker gets a say in this beat; where each one sits is
			// what decides whether it has one.
			for (int other = 0; other < jokers.Count; other++)
			{
				if (other != slot)
				{
					trigger = jokers[other].Amplify(trigger, other, slot);
				}
			}

			for (int play = 0; play < plays; play++)
			{
				chips += trigger.Chips;
				mult += trigger.Mult;
				mult *= trigger.Times;
				steps.Add(new ScoreStep(ScoreStepKind.Joker, trigger.Chips, trigger.Mult, trigger.Times, chips, mult, slot));
			}
		}

		return new ScoreBreakdown(hand.Outcome, chips, mult, (long)(chips * mult), steps);
	}
}
