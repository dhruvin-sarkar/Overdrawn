using System.Collections.Generic;

namespace Overdrawn.Scoring;

/// Which part of the pipeline a beat came from.
public enum ScoreStepKind
{
	Base,
	Deck,
	Streak,
	DealerBust,
	Joker,
}

/// <summary>
/// One beat of the scoring pipeline: what it added, and where the hand stands
/// once it has. The screen plays these in order so the player watches the score
/// being built rather than being handed a total.
/// </summary>
/// <param name="Slot">Which joker fired, for a joker beat; -1 for the rest of the pipeline.</param>
public readonly record struct ScoreStep(
	ScoreStepKind Kind, int AddChips, double AddMult, double TimesMult, int Chips, double Mult, int Slot = -1)
{
	public static readonly IReadOnlyList<ScoreStep> None = new List<ScoreStep>();
}
