using Godot;
using Godot.Collections;
using Overdrawn.Blackjack;
using Overdrawn.Cards;
using Overdrawn.Run;

namespace Overdrawn.Content;

/// <summary>
/// The deck a run is started with. This is a run-long identity, unlike the
/// temporary variant a single level layers onto the shoe.
/// </summary>
[GlobalClass]
public partial class StartingDeck : Resource
{
	[Export] public string Id { get; set; } = "";
	[Export] public string DisplayName { get; set; } = "";
	/// The effect line shown on the run start screen.
	[Export(PropertyHint.MultilineText)] public string Effect { get; set; } = "";
	/// Ranks struck out of the deck entirely, such as the Superstition Deck's fours.
	[Export] public Array<Rank> RemovedRanks { get; set; } = new();
	/// Scales the bankroll a run opens with, for a deck that trades money for power.
	[Export] public float StartingBankrollScale { get; set; } = 1f;

	public System.Collections.Generic.List<PlayingCard> BuildDeck() => Shoe.StandardDeck(RemovedRanks);
}
