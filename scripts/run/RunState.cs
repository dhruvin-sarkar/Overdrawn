using System;
using System.Collections.Generic;
using System.Linq;
using Overdrawn.Blackjack;
using Overdrawn.Bosses;
using Overdrawn.Jokers;

namespace Overdrawn.Run;

/// <summary>
/// One run in progress: the money on the table, how far through the eighteen
/// levels it is, the shoe it is dealt from, the boss waiting at the end of each
/// stretch of three, and the jokers on the rail in the order they fire.
/// Deliberately free of Godot so the rules it enforces can be tested without
/// the engine.
/// </summary>
public sealed class RunState
{
	/// What a run opens with. The content reference never states this, so it is
	/// set here: ten minimum wagers at level one.
	public const int BaseStartingBankroll = 100;

	/// The table a run sits down to. Insurance and surrender are both unlocked
	/// by Vouchers, so the opening table offers neither.
	public static readonly HouseRules BaseTable = new() { SurrenderAllowed = false, InsuranceAllowed = false };

	private readonly List<JokerRule> _jokers = new();
	private readonly HashSet<string> _bossesMet = new();
	private bool _seated;

	private RunState(string startingDeckId, Shoe shoe, Random random, int bankroll, int jokerSlots)
	{
		StartingDeckId = startingDeckId;
		Shoe = shoe;
		Random = random;
		Bankroll = bankroll;
		JokerSlots = jokerSlots;
		UpcomingBoss = DrawBoss(RunEconomy.NextBossLevel(Level));
	}

	public string StartingDeckId { get; }
	public Shoe Shoe { get; }
	public Random Random { get; }

	/// The boss dealing this level, or null at an ordinary table.
	public BossRule? Boss { get; private set; }

	/// The boss at the end of this stretch of three levels. It is drawn as the
	/// stretch begins so the player can see it coming.
	public BossRule UpcomingBoss { get; private set; }

	/// Whether the boss's rules are in force. A joker can switch them off, and
	/// the level still asks for a boss's target and pays a boss's bonus.
	public bool BossRulesInForce => Boss is not null && !_jokers.Any(joker => joker.DisablesBossRule);

	/// The rules this level's table is dealt under, before any joker bends them.
	public HouseRules TableRules => BossRulesInForce ? Boss!.Bend(BaseTable) : BaseTable;

	/// The rules the hand is actually played by: the table's, bent by whichever
	/// jokers bend rules, in slot order.
	public HouseRules Rules
	{
		get
		{
			HouseRules rules = TableRules;
			foreach (JokerRule joker in _jokers)
			{
				rules = joker.Bend(rules);
			}

			return rules;
		}
	}

	/// What this level's table takes: the house limits for the level, with the
	/// boss's minimum where it sets one.
	public (int Minimum, int Maximum) TableLimits
	{
		get
		{
			(int minimum, int maximum) = RunEconomy.WagerBounds(Level);
			return BossRulesInForce ? (Boss!.MinimumWager(minimum), maximum) : (minimum, maximum);
		}
	}

	/// How many times over the base Chips count at this level's table.
	public int BaseChipsScale => BossRulesInForce ? Boss!.BaseChipsScale : 1;

	/// The most interest a level's end can pay.
	public int InterestCap => RunEconomy.InterestCap + _jokers.Sum(joker => joker.InterestCapBonus);

	/// The rail, in slot order. The order is the order the jokers fire in.
	public IReadOnlyList<JokerRule> Jokers => _jokers;
	public int JokerSlots { get; private set; }

	public int Level { get; private set; } = 1;
	public long ScoreTarget { get; private set; } = RunEconomy.FirstScoreTarget;
	public long LevelScore { get; private set; }
	/// How many hands this level deals in all, extra hands included.
	public int HandsThisLevel { get; private set; } = RunEconomy.HandsPerLevel;
	public int HandsRemaining { get; private set; } = RunEconomy.HandsPerLevel;
	/// How many hands this level has dealt so far.
	public int HandsPlayed { get; private set; }
	public int Bankroll { get; private set; }
	public int Debt { get; private set; }
	/// What the first loan of the current debt was, which is what The Collector watches.
	public int OriginalLoan { get; private set; }
	public int WinStreak { get; private set; }

	public bool IsBossLevel => RunEconomy.IsBossLevel(Level);
	public bool LevelCleared => LevelScore >= ScoreTarget;
	public bool LevelFailed => HandsRemaining == 0 && !LevelCleared;
	/// Whether this level has paid out, which is what lets the player leave it.
	public bool CashedOut { get; private set; }
	/// Cleared the final table.
	public bool RunWon => Level == RunEconomy.LevelsPerRun && LevelCleared;

	/// Whether the dealer's hole card is dealt face up at this table.
	public bool Peek => _jokers.Any(joker => joker.Peek);

	/// <param name="deck">The player's permanent deck, already built to the starting deck's composition.</param>
	/// <param name="bankrollScale">The starting deck's pull on the opening bankroll.</param>
	/// <param name="jokerSlots">How many jokers the rail holds to begin with.</param>
	public static RunState Begin(string startingDeckId, IEnumerable<PlayingCard> deck, int seed,
		float bankrollScale = 1f, int jokerSlots = RunEconomy.BaseJokerSlots)
	{
		if (bankrollScale <= 0f)
		{
			throw new ArgumentOutOfRangeException(nameof(bankrollScale), bankrollScale, "A run cannot open broke.");
		}

		if (jokerSlots < 1)
		{
			throw new ArgumentOutOfRangeException(nameof(jokerSlots), jokerSlots, "A rail needs at least one slot.");
		}

		var random = new Random(seed);
		return new RunState(startingDeckId, new Shoe(deck, random), random,
			(int)Math.Round(BaseStartingBankroll * bankrollScale), jokerSlots);
	}

	/// Sits the player down at this level's table, which is when the dealer
	/// shuffles a fresh shoe, without whatever cards the boss keeps out of it.
	public void SitDown()
	{
		if (_seated)
		{
			throw new InvalidOperationException("The player is already sitting at this level's table.");
		}

		_seated = true;
		Shoe.BeginLevel(Array.Empty<PlayingCard>(), strike: BossRulesInForce ? Boss!.Strikes : null);
	}

	/// Puts a joker in the rightmost free slot. The joker sees the run the
	/// moment it arrives, which is when a deck-editing one edits the deck, and
	/// a joker that refuses the run never takes a slot.
	public void Acquire(JokerRule joker)
	{
		if (_jokers.Count >= JokerSlots)
		{
			throw new InvalidOperationException($"All {JokerSlots} joker slots are taken.");
		}

		joker.OnAcquired(this);
		_jokers.Add(joker);
	}

	/// Takes a joker off the rail. Whatever it was owed is the shop's business.
	public JokerRule Release(int slot)
	{
		JokerRule joker = _jokers[slot];
		_jokers.RemoveAt(slot);
		return joker;
	}

	/// Reorders the rail, which reorders the pipeline.
	public void MoveJoker(int from, int to)
	{
		JokerRule joker = _jokers[from];
		_jokers.RemoveAt(from);
		_jokers.Insert(to, joker);
	}

	/// Money leaves the bankroll and sits on the table.
	public void Stake(int amount) => Withdraw(amount);

	/// Money leaves the bankroll for good: a purchase, a debt paid down, a
	/// joker turning cash into something else.
	public void Spend(int amount) => Withdraw(amount);

	/// Money comes back off the table: the stake plus whatever it paid, and
	/// whatever a joker pays on the side.
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
		int minimum = TableLimits.Minimum;
		if (Bankroll >= minimum)
		{
			return false;
		}

		int loan = RunEconomy.LoanFor(minimum);
		Bankroll += loan;
		Debt += loan;
		if (OriginalLoan == 0)
		{
			OriginalLoan = loan;
		}

		CallTheCollectorIfDue();
		return true;
	}

	/// Pays the debt off in the shop, for exactly what is owed.
	public void PayOffDebt()
	{
		if (Debt == 0)
		{
			throw new InvalidOperationException("There is no debt to pay off.");
		}

		Withdraw(Debt);
		Debt = 0;
		OriginalLoan = 0;
	}

	/// Spends one of the level's hands. Dealing is what uses one up.
	public void SpendHand()
	{
		if (HandsRemaining == 0)
		{
			throw new InvalidOperationException("This level has no hands left to play.");
		}

		HandsRemaining--;
		HandsPlayed++;
	}

	/// A win extends the streak, a loss ends it, and a push leaves it alone,
	/// unless a joker on the rail says otherwise.
	public void RecordOutcome(HandSettlement settlement)
	{
		if (settlement.PlayerWon)
		{
			WinStreak++;
			return;
		}

		if (settlement.Result == Settlement.Push)
		{
			if (_jokers.Any(joker => joker.PushExtendsStreak))
			{
				WinStreak++;
			}
			else if (_jokers.Any(joker => joker.PushBreaksStreak))
			{
				WinStreak = 0;
			}

			return;
		}

		// A loss, unless a joker is willing to cover it. A streak of nothing has
		// nothing to cover, so a shield is only asked when there is one.
		if (WinStreak > 0 && _jokers.Any(joker => joker.ShieldLoss()))
		{
			return;
		}

		WinStreak = 0;
	}

	public void AddScore(long score)
	{
		if (score < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(score), score, "A hand cannot score backwards.");
		}

		LevelScore += score;
	}

	/// Clears the level outright, whatever has been scored so far.
	public void ClearLevel()
	{
		LevelScore = Math.Max(LevelScore, ScoreTarget);
	}

	/// Pays out a cleared level: the clear itself, every hand left unplayed, a
	/// boss's bonus, and interest on what the player held as it ended. A boss
	/// that wipes the debt wipes it here.
	public CashOut CashOutLevel()
	{
		if (!LevelCleared)
		{
			throw new InvalidOperationException("Only a cleared level pays out.");
		}

		if (CashedOut)
		{
			throw new InvalidOperationException("This level has already paid out.");
		}

		CashOut cashOut = RunEconomy.CashOutFor(HandsRemaining, IsBossLevel, Bankroll, InterestCap);
		Bankroll += cashOut.Total;
		if (BossRulesInForce && Boss!.WipesDebtOnClear)
		{
			Debt = 0;
			OriginalLoan = 0;
		}

		CashedOut = true;
		return cashOut;
	}

	/// Moves on to the next level: a bigger target, a fresh set of hands, a
	/// streak that starts again, debt a little heavier, the boss if this is a
	/// boss level, and every joker's level-start say, in slot order.
	public void BeginNextLevel()
	{
		if (!CashedOut)
		{
			throw new InvalidOperationException("A level is only left once it has paid out.");
		}

		if (Level == RunEconomy.LevelsPerRun)
		{
			throw new InvalidOperationException("There is no table after the final one.");
		}

		Level++;
		Boss = IsBossLevel ? UpcomingBoss : null;
		ScoreTarget = RunEconomy.NextScoreTarget(ScoreTarget, Boss?.TargetMultiplier ?? RunEconomy.StandardTargetGrowth);
		LevelScore = 0;
		HandsThisLevel = RunEconomy.HandsPerLevel + _jokers.Sum(joker => joker.ExtraHands);
		HandsRemaining = HandsThisLevel;
		HandsPlayed = 0;
		WinStreak = 0;
		CashedOut = false;
		_seated = false;
		Shoe.EndLevel();

		GrowDebt();
		if (Boss is not null)
		{
			_bossesMet.Add(Boss.Id);
			if (BossRulesInForce)
			{
				Debt = Boss.DebtOnArrival(Debt);
			}
		}
		else if (RunEconomy.IsBossLevel(Level - 1))
		{
			UpcomingBoss = DrawBoss(RunEconomy.NextBossLevel(Level));
		}

		CallTheCollectorIfDue();

		foreach (JokerRule joker in _jokers)
		{
			joker.OnLevelStart(this);
		}
	}

	/// Interest on debt, at whatever rate the rail allows, in slot order.
	private void GrowDebt()
	{
		if (Debt == 0)
		{
			return;
		}

		int grown = RunEconomy.GrowDebt(Debt);
		foreach (JokerRule joker in _jokers)
		{
			grown = joker.GrowDebt(Debt, grown);
		}

		Debt = grown;
	}

	/// Debt past three times its first loan sends The Collector to the boss
	/// table the player has not reached yet. The final table is never given up.
	private void CallTheCollectorIfDue()
	{
		if (IsBossLevel || UpcomingBoss.Id == BossRules.Collector
			|| RunEconomy.NextBossLevel(Level) == RunEconomy.LevelsPerRun
			|| !RunEconomy.CollectorDue(Debt, OriginalLoan))
		{
			return;
		}

		UpcomingBoss = BossRules.Create(BossRules.Collector, Random);
	}

	/// A boss for a boss level, from those fierce enough for it that this run
	/// has not met yet. The last level always gets The Final Table.
	private BossRule DrawBoss(int level)
	{
		if (level == RunEconomy.LevelsPerRun)
		{
			return BossRules.Create(BossRules.FinalTable, Random);
		}

		List<string> fierceEnough = BossRules.DrawnAt(level).ToList();
		List<string> unmet = fierceEnough.Where(id => !_bossesMet.Contains(id)).ToList();
		List<string> pool = unmet.Count > 0 ? unmet : fierceEnough;
		return BossRules.Create(pool[Random.Next(pool.Count)], Random);
	}

	private void Withdraw(int amount)
	{
		if (amount <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(amount), amount, "Only money can leave the bankroll.");
		}

		if (amount > Bankroll)
		{
			throw new InvalidOperationException($"${amount} is more than the ${Bankroll} on hand.");
		}

		Bankroll -= amount;
	}
}
