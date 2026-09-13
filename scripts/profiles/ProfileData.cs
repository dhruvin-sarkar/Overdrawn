using System.Collections.Generic;
using System.Linq;
using Overdrawn.Scoring;

namespace Overdrawn.Profiles;

/// <summary>
/// Everything a save slot remembers between sessions. Serialised as JSON.
/// </summary>
public sealed class ProfileData
{
	public string Name { get; set; } = "";
	public int Wins { get; set; }
	public ProfileStats Stats { get; set; } = new();
	public ProfileProgress Progress { get; set; } = new();
}

public sealed class ProfileStats
{
	public long BestHand { get; set; }
	public int HighestLevel { get; set; }
	public int MostMoney { get; set; }
	public int BestWinStreak { get; set; }
	public int CurrentWinStreak { get; set; }
	public Dictionary<HandOutcome, int> Outcomes { get; set; } = new();

	public int Count(HandOutcome outcome) => Outcomes.GetValueOrDefault(outcome);

	/// The outcome played most often, or null before any hand has been played.
	public HandOutcome? MostCommonOutcome() =>
		Outcomes.Where(pair => pair.Value > 0)
			.OrderByDescending(pair => pair.Value)
			.Select(pair => (HandOutcome?)pair.Key)
			.FirstOrDefault();
}

public sealed class ProfileProgress
{
	public HashSet<string> Collection { get; set; } = new();
	public HashSet<string> Achievements { get; set; } = new();
	public HashSet<string> Challenges { get; set; } = new();
	public HashSet<string> DecksCleared { get; set; } = new();
}
