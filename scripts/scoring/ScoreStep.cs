using System.Collections.Generic;

namespace Overdrawn.Scoring;

/// Which part of the pipeline a beat came from.
public enum ScoreStepKind
{
	Base,
	Streak,
	DealerBust,
}

/// <summary>
/// One beat of the scoring pipeline: what it added, and where the hand stands
/// once it has. The screen plays these in order so the player watches the score
/// being built rather than being handed a total.
/// </summary>
public readonly record struct ScoreStep(ScoreStepKind Kind, int AddChips, double AddMult, int Chips, double Mult)
{
	public static readonly IReadOnlyList<ScoreStep> None = new List<ScoreStep>();
}
