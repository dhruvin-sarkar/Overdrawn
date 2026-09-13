using Godot;

namespace Overdrawn.UI;

/// <summary>
/// The Options hub: a column of buttons leading to the other menu panels.
/// </summary>
public partial class OptionsPanel : MenuPanel
{
	[Export] public PackedScene SettingsScene { get; set; } = null!;
	[Export] public PackedScene StatsScene { get; set; } = null!;
	[Export] public PackedScene DeckScene { get; set; } = null!;
	[Export] public PackedScene CreditsScene { get; set; } = null!;

	public override void _Ready()
	{
		base._Ready();
		GetNode<Button>("%Settings").Pressed += () => RequestOpen(SettingsScene);
		GetNode<Button>("%Stats").Pressed += () => RequestOpen(StatsScene);
		GetNode<Button>("%Deck").Pressed += () => RequestOpen(DeckScene);
		GetNode<Button>("%Credits").Pressed += () => RequestOpen(CreditsScene);
	}
}
