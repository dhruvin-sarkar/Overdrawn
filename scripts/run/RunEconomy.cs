using System;

namespace Overdrawn.Run;

/// What a cleared level pays, line by line, in the order the cash-out reads it.
/// <param name="HandsLeft">How many of the level's hands went unplayed.</param>
/// <param name="HandBonus">What those hands are worth.</param>
public readonly record struct CashOut(int Clear, int HandsLeft, int HandBonus, int BossBonus, int Interest)
{
	public int Total => Clear + HandBonus + BossBonus + Interest;
}

/// <summary>
/// Every number a run runs on, straight from the Content Reference: what a
/// level asks for, what a table lets you wager, and what the house pays.
/// </summary>
public static class RunEconomy
{
	public const int LevelsPerRun = 18;
	public const int HandsPerLevel = 8;

	/// The rail a run opens with, before a deck or a voucher widens it.
	public const int BaseJokerSlots = 5;
	public const int BaseConsumableSlots = 2;

	public const long FirstScoreTarget = 300;
	public const double StandardTargetGrowth = 1.4;

	public const int FirstMinimumWager = 10;
	public const int FirstMaximumWager = 100;
	public const double WagerGrowth = 1.15;
	public const int WagerStep = 5;

	public const int ClearPayout = 25;
	public const int UnusedHandBonus = 5;
	public const int BossClearBonus = 50;

	public const int DollarsPerInterestDollar = 10;
	public const int InterestCap = 50;

	public const int DebtLoanMultiple = 3;
	public const int DebtInterestPercent = 10;
	/// Debt past this multiple of the original loan sends The Collector after you.
	public const int CollectorMultiple = 3;

	/// Every third level is a boss.
	public static bool IsBossLevel(int level) => Level(level) % 3 == 0;

	/// The first boss level at or after this one.
	public static int NextBossLevel(int level) => (Level(level) + 2) / 3 * 3;

	/// A level asks for the previous level's target times its own multiplier:
	/// x1.4 for a standard table, or whatever the boss brings.
	public static long NextScoreTarget(long previousTarget, double multiplier) =>
		(long)Math.Round(previousTarget * multiplier);

	public static (int Minimum, int Maximum) WagerBounds(int level)
	{
		double growth = Math.Pow(WagerGrowth, Level(level) - 1);
		return (ToStep(FirstMinimumWager * growth), ToStep(FirstMaximumWager * growth));
	}

	/// A dollar for every ten held, and the house stops counting at fifty.
	public static int Interest(int bankroll, int cap = InterestCap) =>
		Math.Min(Math.Max(bankroll, 0) / DollarsPerInterestDollar, cap);

	/// The house pays for the clear and for every hand left unplayed, a boss
	/// pays its bonus on top, and the money held as the level ends earns interest.
	public static CashOut CashOutFor(int handsLeft, bool boss, int bankroll, int interestCap)
	{
		if (handsLeft < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(handsLeft), handsLeft, "A level cannot have hands to spare below none.");
		}

		return new CashOut(ClearPayout, handsLeft, handsLeft * UnusedHandBonus, boss ? BossClearBonus : 0,
			Interest(bankroll, interestCap));
	}

	/// The house floats you three minimum wagers when you cannot cover one.
	public static int LoanFor(int minimumWager) => minimumWager * DebtLoanMultiple;

	/// Debt grows at the start of every level it is still outstanding, and the
	/// house rounds the interest up. Whole-dollar math, so ten percent of $200
	/// is $20 and never a cent over.
	public static int GrowDebt(int debt) => debt <= 0 ? 0 : debt + (((debt * DebtInterestPercent) + 99) / 100);

	public static bool CollectorDue(int debt, int originalLoan) =>
		originalLoan > 0 && debt > originalLoan * CollectorMultiple;

	private static int ToStep(double amount) => (int)(Math.Round(amount / WagerStep) * WagerStep);

	private static int Level(int level) => level >= 1
		? level
		: throw new ArgumentOutOfRangeException(nameof(level), level, "Levels start at one.");
}
