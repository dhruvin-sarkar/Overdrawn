using Overdrawn.Blackjack;

namespace Overdrawn.Scoring;

/// <summary>
/// A finished hand as the scoring pipeline sees it: the cards, how the table
/// settled them, and the run facts a joker is allowed to read. A joker that
/// reframes a hand hands back a copy; the Hand on the felt is never touched.
/// </summary>
/// <param name="Seat">Which of the round's hands this is, from zero. A split's halves follow the hand they came from.</param>
/// <param name="WinStreak">Consecutive wins including this hand, zero if it lost.</param>
/// <param name="HandNumber">Which deal of the level this hand belongs to, from one.</param>
/// <param name="Bankroll">What the player holds once the table has paid, read live.</param>
/// <param name="BaseChipsScale">How many times over the table counts the base Chips.</param>
/// <param name="DeckMult">Mult the starting deck adds to every hand that scores.</param>
public sealed record ScoringHand(Hand Hand, HandSettlement Settlement, int Seat, int WinStreak, int HandNumber, int Bankroll,
	int BaseChipsScale = 1, int DeckMult = 0)
{
	/// The tier the hand scores in: what the cards say, unless a joker reframes it.
	public HandOutcome Outcome { get; init; } = ScoreCalculator.Classify(Hand);
}
