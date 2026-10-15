namespace Overdrawn.Jokers;

/// Every Joker positioned to the right of this one has its Mult effects
/// doubled. The number on the effect is what doubles, so +2 Mult becomes +4
/// and x1.5 Mult becomes x3. Chips are not a Mult effect.
public sealed class MultEngine : JokerRule
{
	public override string Id => "mult_engine";

	public override JokerTrigger Amplify(JokerTrigger trigger, int ownSlot, int firingSlot) => firingSlot > ownSlot
		? trigger with { Mult = trigger.Mult * 2, Times = trigger.Times == 1 ? 1 : trigger.Times * 2 }
		: trigger;
}
