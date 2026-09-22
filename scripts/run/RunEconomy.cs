using System;

namespace Overdrawn.Run;

/// <summary>
/// Every number a run runs on, straight from the Content Reference: what a
/// level asks for, what a table lets you wager, and what the house pays.
/// </summary>
public static class RunEconomy
{
	public const int LevelsPerRun = 18;
	public const int HandsPerLevel = 8;

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
	public const double DebtInterest = 0.1;
	/// Debt past this multiple of the original loan sends The Collector after you.
	public const int CollectorMultiple = 3;

	/// Every third level is a boss.
	public static bool IsBossLevel(int level) => Level(level) % 3 == 0;

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

	public static int LevelPayout(int unusedHands, bool boss)
	{
		if (unusedHands < 0 || unusedHands > HandsPerLevel)
		{
			throw new ArgumentOutOfRangeException(nameof(unusedHands), unusedHands, "A level has eight hands.");
		}

		return ClearPayout + (unusedHands * UnusedHandBonus) + (boss ? BossClearBonus : 0);
	}

	/// The house floats you three minimum wagers when you cannot cover one.
	public static int LoanFor(int level) => WagerBounds(level).Minimum * DebtLoanMultiple;

	/// Debt grows at the start of every level it is still outstanding, and the
	/// house rounds the interest up.
	public static int GrowDebt(int debt) => debt <= 0 ? 0 : (int)Math.Ceiling(debt * (1 + DebtInterest));

	public static bool CollectorDue(int debt, int originalLoan) =>
		originalLoan > 0 && debt > originalLoan * CollectorMultiple;

	private static int ToStep(double amount) => (int)(Math.Round(amount / WagerStep) * WagerStep);

	private static int Level(int level) => level >= 1
		? level
		: throw new ArgumentOutOfRangeException(nameof(level), level, "Levels start at one.");
}
