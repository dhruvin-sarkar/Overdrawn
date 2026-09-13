using System;

namespace Overdrawn.Scoring;

/// The scoring tier a finished hand lands in, per the Content Reference.
public enum HandOutcome
{
	Bust,
	StandUnder17,
	Stand17To20,
	TwentyOne,
	Blackjack,
}

public static class HandOutcomeText
{
	public static string Label(this HandOutcome outcome) => outcome switch
	{
		HandOutcome.Bust => "Bust",
		HandOutcome.StandUnder17 => "Stand Under 17",
		HandOutcome.Stand17To20 => "Stand 17-20",
		HandOutcome.TwentyOne => "21",
		HandOutcome.Blackjack => "Blackjack",
		_ => throw new ArgumentOutOfRangeException(nameof(outcome)),
	};
}
