namespace Overdrawn.Jokers;

/// The first Table Pack reroll each shop is free.
public sealed class QuickHands : JokerRule
{
	public override string Id => "quick_hands";

	public override bool FreeFirstPackReroll => true;
}
