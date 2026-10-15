using Overdrawn.Blackjack;

namespace Overdrawn.Jokers;

/// Gain $3 whenever you bust. The table still takes the wager; the policy pays
/// out beside it, once for every hand that busts.
public sealed class InsurancePolicy : JokerRule
{
	public const int Cash = 3;

	public override string Id => "insurance_policy";

	public override int Payout(Hand hand, HandSettlement settlement) => hand.IsBust ? Cash : 0;
}
