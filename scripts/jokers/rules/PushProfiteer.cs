namespace Overdrawn.Jokers;

/// Pushes score as if they were a win at your current total. A push already
/// scores on its own tier, so what a win would add is the streak: a push
/// extends it instead of leaving it alone.
public sealed class PushProfiteer : JokerRule
{
	public override string Id => "push_profiteer";

	public override bool PushExtendsStreak => true;
}
