using System;
using Overdrawn.Blackjack;
using Overdrawn.Run;

namespace Overdrawn.Jokers;

/// Once per level, the whole bankroll may ride on one hand, and losing it
/// leaves $1 rather than nothing.
public sealed class AllIn : JokerRule
{
	public override string Id => "all_in";

	public bool UsedThisLevel { get; private set; }

	/// Whether the hand being dealt is the one riding it all.
	public bool Armed { get; private set; }

	public override bool AllowsAllIn => !UsedThisLevel;

	public void Arm()
	{
		if (UsedThisLevel)
		{
			throw new InvalidOperationException("All In has already been played this level.");
		}

		UsedThisLevel = true;
		Armed = true;
	}

	public override HandSettlement Overrule(HandSettlement settlement, Hand hand)
	{
		if (!Armed)
		{
			return settlement;
		}

		Armed = false;
		return hand.Wager + settlement.Payout < 1 ? settlement with { Payout = 1 - hand.Wager } : settlement;
	}

	public override void OnLevelStart(RunState run)
	{
		UsedThisLevel = false;
		Armed = false;
	}
}
