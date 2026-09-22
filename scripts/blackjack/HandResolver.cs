using System;
using System.Collections.Generic;

namespace Overdrawn.Blackjack;

public enum Move
{
	Hit,
	Stand,
	Double,
	Split,
	Surrender,
}

public enum Settlement
{
	PlayerBust,
	PlayerSurrender,
	DealerBlackjack,
	DealerBust,
	PlayerWin,
	PlayerBlackjack,
	DealerWin,
	Push,
}

/// How a hand ended and what it did to the bankroll. Payout is the net change:
/// the wager is already staked, so a loss is negative and a push is zero.
public readonly record struct HandSettlement(Settlement Result, int Payout)
{
	public bool PlayerWon => Result is Settlement.PlayerWin or Settlement.PlayerBlackjack or Settlement.DealerBust;
	public bool DealerBusted => Result == Settlement.DealerBust;
}

/// <summary>
/// Real blackjack: which moves are legal, when the dealer draws, who won, and
/// what that pays. It knows nothing about Chips, Mult or Jokers — scoring reads
/// its result afterwards.
/// </summary>
public static class HandResolver
{
	public static IReadOnlyList<Move> LegalMoves(Hand hand, Hand dealer, int bankroll, HouseRules rules, int splitsUsed)
	{
		var moves = new List<Move>();
		if (hand.IsFinished)
		{
			return moves;
		}

		moves.Add(Move.Hit);
		moves.Add(Move.Stand);

		bool opening = hand.Cards.Count == 2;
		if (opening && rules.DoubleAllowed && bankroll >= hand.Wager && (!hand.FromSplit || rules.DoubleAfterSplit))
		{
			moves.Add(Move.Double);
		}

		if (opening && hand.IsPair && rules.SplitAllowed && splitsUsed < rules.MaxSplits && bankroll >= hand.Wager)
		{
			moves.Add(Move.Split);
		}

		// Late surrender: offered once the dealer is known not to have a natural.
		if (opening && !hand.FromSplit && rules.SurrenderAllowed && !dealer.IsBlackjack)
		{
			moves.Add(Move.Surrender);
		}

		return moves;
	}

	/// The dealer's fixed strategy: draw to 17, and on soft 17 draw only at a
	/// table that says so.
	public static bool DealerDraws(Hand dealer, HouseRules rules)
	{
		int total = dealer.Total;
		if (total < 17)
		{
			return true;
		}

		return total == 17 && dealer.IsSoft && rules.DealerHitsSoft17;
	}

	public static HandSettlement Settle(Hand hand, Hand dealer, HouseRules rules)
	{
		if (hand.Surrendered)
		{
			// Half the wager back, and the house keeps the odd dollar.
			return new HandSettlement(Settlement.PlayerSurrender, -(hand.Wager / 2));
		}

		// A bust loses even when the dealer goes on to bust as well — the
		// player's money is already gone by then.
		if (hand.IsBust)
		{
			return new HandSettlement(Settlement.PlayerBust, -hand.Wager);
		}

		if (hand.IsBlackjack && dealer.IsBlackjack)
		{
			return Tie(rules, hand.Wager);
		}

		if (hand.IsBlackjack)
		{
			return new HandSettlement(Settlement.PlayerBlackjack, (int)Math.Floor(hand.Wager * rules.BlackjackPayout));
		}

		if (dealer.IsBlackjack)
		{
			return new HandSettlement(Settlement.DealerBlackjack, -hand.Wager);
		}

		if (dealer.IsBust)
		{
			return new HandSettlement(Settlement.DealerBust, hand.Wager);
		}

		if (hand.Total > dealer.Total)
		{
			return new HandSettlement(Settlement.PlayerWin, hand.Wager);
		}

		if (hand.Total < dealer.Total)
		{
			return new HandSettlement(Settlement.DealerWin, -hand.Wager);
		}

		return Tie(rules, hand.Wager);
	}

	/// Insurance is a side bet, settled before the hand itself and paying 2:1.
	public static int SettleInsurance(int bet, Hand dealer, HouseRules rules)
	{
		if (!rules.InsuranceAllowed && bet > 0)
		{
			throw new InvalidOperationException("This table does not offer insurance.");
		}

		return dealer.IsBlackjack ? bet * 2 : -bet;
	}

	private static HandSettlement Tie(HouseRules rules, int wager) => rules.DealerWinsTies
		? new HandSettlement(Settlement.DealerWin, -wager)
		: new HandSettlement(Settlement.Push, 0);
}
