using Overdrawn.Blackjack;

namespace Overdrawn.Jokers;

/// +$2 whenever a hand results in a push, however it came to be one.
public sealed class EvenMoney : JokerRule
{
	public const int Cash = 2;

	public override string Id => "even_money";

	public override int Payout(Hand hand, HandSettlement settlement) => settlement.Result == Settlement.Push ? Cash : 0;
}
