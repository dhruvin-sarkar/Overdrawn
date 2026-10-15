namespace Overdrawn.Jokers;

/// +1 extra hand every level.
public sealed class Overtime : JokerRule
{
	public override string Id => "overtime";

	public override int ExtraHands => 1;
}
