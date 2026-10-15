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
	[Export] public int ExtraJokerSlots { get; set; }
	/// Scale every table's wager limits for the whole run.
	[Export] public float MinimumWagerScale { get; set; } = 1f;
	[Export] public float MaximumWagerScale { get; set; } = 1f;
	/// The dealer's hole card is dealt face up all run.
	[Export] public bool Peek { get; set; }
	[Export] public float InterestCapScale { get; set; } = 1f;
	/// Mult added to every hand that scores.
	[Export] public int Mult { get; set; }
	/// Shows whether the next card is high or low before every deal.
	[Export] public bool NextCardHint { get; set; }

	public System.Collections.Generic.List<PlayingCard> BuildDeck() => Shoe.StandardDeck(RemovedRanks);

	public DeckTerms Terms() => new()
	{
		BankrollScale = StartingBankrollScale,
		JokerSlots = RunEconomy.BaseJokerSlots + ExtraJokerSlots,
		MinimumWagerScale = MinimumWagerScale,
		MaximumWagerScale = MaximumWagerScale,
		Peek = Peek,
		InterestCapScale = InterestCapScale,
		Mult = Mult,
		NextCardHint = NextCardHint,
	};
}
