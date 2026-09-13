using System.Globalization;
using Godot;
using Overdrawn.Profiles;
using Overdrawn.Scoring;

namespace Overdrawn.UI;

/// <summary>
/// Personal bests and completion for the current profile.
/// </summary>
public partial class StatsPanel : MenuPanel
{
	[Export] public Color ScoreColour { get; set; } = new(0.914f, 0.306f, 0.247f);
	[Export] public Color LevelColour { get; set; } = new(0.953f, 0.584f, 0f);
	[Export] public Color MoneyColour { get; set; } = new(0.965f, 0.745f, 0.2f);

	public override void _Ready()
	{
		base._Ready();
		ProfileData profile = ProfileSlots.Instance.Current;
		ProfileStats stats = profile.Stats;

		GetNode<StatRow>("%BestHand").ShowValue(Number(stats.BestHand), ScoreColour);
		GetNode<StatRow>("%HighestLevel").ShowValue(Number(stats.HighestLevel), LevelColour);
		GetNode<StatRow>("%MostMoney").ShowValue($"${Number(stats.MostMoney)}", MoneyColour);
		GetNode<StatRow>("%BestStreak").ShowValue($"{stats.BestWinStreak} ({stats.CurrentWinStreak})", Colors.White);

		HandOutcome? mostPlayed = stats.MostCommonOutcome();
		GetNode<StatRow>("%MostPlayed").ShowValue(
			mostPlayed is { } outcome ? $"{outcome.Label()} ({stats.Count(outcome)})" : "None", Colors.White);

		GetNode<ProgressSummary>("%Progress").ShowProgress(profile.Progress);
	}

	private static string Number(long value) => value.ToString("N0", CultureInfo.InvariantCulture);
}
