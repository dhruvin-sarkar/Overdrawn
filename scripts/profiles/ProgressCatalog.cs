using System.Collections.Generic;
using System.Linq;

namespace Overdrawn.Profiles;

/// <summary>
/// How much there is to complete, per the Content Reference. Replace these
/// counts with the content Resources once jokers, decks and achievements exist.
/// </summary>
public static class ProgressCatalog
{
	public const int Jokers = 40;
	public const int StartingDecks = 6;
	public const int Achievements = 21;
	public const int Challenges = 0;

	public static IReadOnlyList<ProgressRow> Rows(ProfileProgress progress) => new[]
	{
		new ProgressRow("Collection", progress.Collection.Count, Jokers + StartingDecks),
		new ProgressRow("Achievements", progress.Achievements.Count, Achievements),
		new ProgressRow("Challenges", progress.Challenges.Count, Challenges),
		new ProgressRow("Decks Cleared", progress.DecksCleared.Count, StartingDecks),
	};

	/// Overall completion: the average of every category that has something to complete.
	public static float Overall(ProfileProgress progress)
	{
		ProgressRow[] counted = Rows(progress).Where(row => row.Total > 0).ToArray();
		return counted.Length == 0 ? 0f : counted.Average(row => row.Fraction);
	}
}

public readonly record struct ProgressRow(string Label, int Done, int Total)
{
	public float Fraction => Total == 0 ? 0f : (float)Done / Total;
}
