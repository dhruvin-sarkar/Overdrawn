namespace Overdrawn.Jokers;

/// The dealer's hole card is dealt face up, every hand, for the rest of the run.
public sealed class MarkedDeck : JokerRule
{
	public override string Id => "marked_deck";

	public override bool Peek => true;
}
