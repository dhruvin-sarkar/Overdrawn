using System;
using System.Collections.Generic;
using Overdrawn.Blackjack;

namespace Overdrawn.Run;

/// <summary>
/// One run in progress: the money on the table, how far through the eighteen
/// levels it is, and the shoe it is dealt from. Deliberately free of Godot so
/// the rules it enforces can be tested without the engine.
/// </summary>
public sealed class RunState
{
	/// What a run opens with. The content reference never states this, so it is
	/// set here: ten minimum wagers at level one.
	public const int BaseStartingBankroll = 100;

	/// The table a run sits down to. Insurance and surrender are both unlocked
	/// by Vouchers, so the opening table offers neither.
	public static readonly HouseRules BaseTable = new() { SurrenderAllowed = false, InsuranceAllowed = false };

	private RunState(string startingDeckId, Shoe shoe, Random random, int bankroll)
	{
		StartingDeckId = startingDeckId;
		Shoe = shoe;
		Random = random;
		Bankroll = bankroll;
	}

	public string StartingDeckId { get; }
	public Shoe Shoe { get; }
	public Random Random { get; }
	public HouseRules Rules { get; } = BaseTable;

	public int Level { get; private set; } = 1;
	public long ScoreTarget { get; private set; } = RunEconomy.FirstScoreTarget;
	public long LevelScore { get; private set; }
	public int HandsRemaining { get; private set; } = RunEconomy.HandsPerLevel;
	public int Bankroll { get; private set; }
	public int Debt { get; private set; }
	/// What the first loan of the current debt was, which is what The Collector watches.
	public int OriginalLoan { get; private set; }
	public int WinStreak { get; private set; }

	public bool IsBossLevel => RunEconomy.IsBossLevel(Level);
	public bool LevelCleared => LevelScore >= ScoreTarget;
	public bool LevelFailed => HandsRemaining == 0 && !LevelCleared;

	/// <param name="deck">The player's permanent deck, already built to the starting deck's composition.</param>
	/// <param name="bankrollScale">The starting deck's pull on the opening bankroll.</param>
	public static RunState Begin(string startingDeckId, IEnumerable<PlayingCard> deck, int seed, float bankrollScale = 1f)
	{
		if (bankrollScale <= 0f)
		{
			throw new ArgumentOutOfRangeException(nameof(bankrollScale), bankrollScale, "A run cannot open broke.");
		}

		var random = new Random(seed);
		return new RunState(startingDeckId, new Shoe(deck, random), random,
			(int)Math.Round(BaseStartingBankroll * bankrollScale));
	}

	/// Money leaves the bankroll and sits on the table.
	public void Stake(int amount)
	{
		if (amount <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(amount), amount, "A stake has to be money.");
		}

		if (amount > Bankroll)
		{
			throw new InvalidOperationException($"${amount} is more than the ${Bankroll} on hand.");
		}

		Bankroll -= amount;
	}

	/// Money comes back off the table: the stake plus whatever it paid.
	public void Collect(int amount)
	{
		if (amount < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(amount), amount, "A loss is taken by staking, not collecting.");
		}

		Bankroll += amount;
	}

	/// The house floats a loan the moment the bankroll cannot cover the table's
	/// minimum. It triggers on its own; it is not an offer.
	public bool CoverMinimumWager()
	{
		if (Bankroll >= RunEconomy.WagerBounds(Level).Minimum)
		{
			return false;
		}

		int loan = RunEconomy.LoanFor(Level);
		Bankroll += loan;
		Debt += loan;
		if (OriginalLoan == 0)
		{
			OriginalLoan = loan;
		}

		return true;
	}

	/// Spends one of the level's eight hands. Dealing is what uses one up.
	public void SpendHand()
	{
		if (HandsRemaining == 0)
		{
			throw new InvalidOperationException("This level has no hands left to play.");
		}

		HandsRemaining--;
	}

	/// A win extends the streak, a loss ends it, and a push leaves it alone.
	public void RecordOutcome(HandSettlement settlement)
	{
		if (settlement.PlayerWon)
		{
			WinStreak++;
		}
		else if (settlement.Result != Settlement.Push)
		{
			WinStreak = 0;
		}
	}

	public void AddScore(long score)
	{
		if (score < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(score), score, "A hand cannot score backwards.");
		}

		LevelScore += score;
	}
}
