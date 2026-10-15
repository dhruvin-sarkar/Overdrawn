namespace Overdrawn.Jokers;

/// What one joker does to the running score when it fires: Chips added, then
/// Mult added, then Mult multiplied, in that order.
public readonly record struct JokerTrigger(int Chips = 0, double Mult = 0, double Times = 1)
{
	public static JokerTrigger AddChips(int chips) => new(Chips: chips);
	public static JokerTrigger AddMult(double mult) => new(Mult: mult);
	public static JokerTrigger TimesMult(double times) => new(Times: times);
}
