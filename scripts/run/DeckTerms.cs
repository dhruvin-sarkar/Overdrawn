namespace Overdrawn.Run;

/// <summary>
/// What a starting deck does to a run besides its cards. A deck is chosen once
/// and holds for the whole run, so these never change once the run begins.
/// </summary>
public sealed record DeckTerms
{
	/// The House Deck: the standard table with nothing added.
	public static readonly DeckTerms House = new();

	/// Scales the bankroll the run opens with.
	public double BankrollScale { get; init; } = 1;

	/// How many jokers the rail holds to begin with.
	public int JokerSlots { get; init; } = RunEconomy.BaseJokerSlots;

	/// Scale every table's minimum and maximum wager, before rounding to the step.
	public double MinimumWagerScale { get; init; } = 1;
	public double MaximumWagerScale { get; init; } = 1;

	/// The dealer's hole card is dealt face up all run.
	public bool Peek { get; init; }

	/// Scales the most interest a level's end can pay, before jokers add to it.
	public double InterestCapScale { get; init; } = 1;

	/// Mult added to every hand that scores.
	public int Mult { get; init; }

	/// Whether the player sees if the next card off the shoe is high or low
	/// before every deal.
	public bool NextCardHint { get; init; }
}
