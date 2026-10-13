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
}
