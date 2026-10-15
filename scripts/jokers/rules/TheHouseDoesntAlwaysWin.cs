using Overdrawn.Blackjack;

namespace Overdrawn.Jokers;

/// Once per run, the first hand the house wins is a push instead. A surrender
/// is the player's own call, so it is not a hand the house won.
public sealed class TheHouseDoesntAlwaysWin : JokerRule
{
	public override string Id => "the_house_doesnt_always_win";

	public bool Spent { get; private set; }

	public override HandSettlement Overrule(HandSettlement settlement, Hand hand)
	{
		if (Spent || settlement.Result is not (Settlement.DealerWin or Settlement.DealerBlackjack or Settlement.PlayerBust))
		{
			return settlement;
		}

		Spent = true;
		return new HandSettlement(Settlement.Push, 0);
	}
}
