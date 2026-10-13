using Godot;
using Overdrawn.Content;

namespace Overdrawn.UI;

/// <summary>
/// Where a run is set up: pick the deck you are sitting down with, see what it
/// does to the run, and deal in. Continue and Challenges are listed because the
/// run screen has always had them, and say plainly that they hold nothing yet.
/// </summary>
public partial class RunStartPanel : MenuPanel
{
	[Export] public StartingDeckLibrary StartingDecks { get; set; } = null!;

	private OptionCycler _deck = null!;
	private Label _effect = null!;
	private Control _newRun = null!;
	private Label _empty = null!;

	public override void _Ready()
	{
		base._Ready();
		_deck = GetNode<OptionCycler>("%Deck");
		_effect = GetNode<Label>("%Effect");
		_newRun = GetNode<Control>("%NewRun");
		_empty = GetNode<Label>("%Empty");

		string[] names = new string[StartingDecks.Decks.Count];
		for (int i = 0; i < names.Length; i++)
		{
			names[i] = StartingDecks.Decks[i].DisplayName;
		}

		_deck.Setup(names, 0);
		_deck.IndexChanged += _ => ShowDeck();
		ShowDeck();

		var tabs = GetNode<TabStrip>("%Tabs");
		tabs.TabSelected += ShowTab;
		tabs.Select(0);

		GetNode<Button>("%Play").Pressed += () => RequestRun(Chosen.Id);
	}

	private StartingDeck Chosen => StartingDecks.Decks[_deck.Index];

	private void ShowDeck() => _effect.Text = Chosen.Effect;

	private void ShowTab(int index)
	{
		_newRun.Visible = index == 0;
		_empty.Visible = !_newRun.Visible;
		_empty.Text = index == 1
			? "No run in progress."
			: "No challenges yet.";
	}
}
