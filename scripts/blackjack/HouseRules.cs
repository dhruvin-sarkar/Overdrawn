namespace Overdrawn.Blackjack;

/// <summary>
/// The table's rules. The defaults are an ordinary Vegas table; a boss dealer
/// sits down with one or two of them changed, and the player can read the
/// difference before the first card is dealt.
/// </summary>
public sealed record HouseRules
{
	public static readonly HouseRules Standard = new();

	/// A dealer on soft 17 hits instead of standing.
	public bool DealerHitsSoft17 { get; init; }
	/// What a natural pays, as a multiple of the wager. 1.5 is 3:2, 1.0 even money.
	public double BlackjackPayout { get; init; } = 1.5;
	public bool DoubleAllowed { get; init; } = true;
	public bool DoubleAfterSplit { get; init; } = true;
	public bool SplitAllowed { get; init; } = true;
	/// How many extra hands a split may produce.
	public int MaxSplits { get; init; } = 3;
	/// Any two ten-value cards split as a pair, not just two of one rank.
	public bool SplitAnyTens { get; init; }
	/// Split Aces take one card each and that is the hand, as at nearly every table.
	public bool HitSplitAces { get; init; }
	public bool SurrenderAllowed { get; init; } = true;
	public bool InsuranceAllowed { get; init; } = true;
	/// The dealer takes pushes. A boss trick, never an ordinary table.
	public bool DealerWinsTies { get; init; }
}
