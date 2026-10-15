using Overdrawn.Blackjack;
using Overdrawn.Run;
using Overdrawn.Scoring;

namespace Overdrawn.Jokers;

/// <summary>
/// What one joker does, as plain C#. Every hook does nothing by default, so a
/// joker overrides only the moments it cares about. The run holds one instance
/// per joker owned, in slot order, and whatever a joker keeps count of lives on
/// that instance. The name, art and price live on the JokerCard with the same id.
/// </summary>
public abstract class JokerRule
{
	public abstract string Id { get; }

	// Scoring, in slot order.

	/// Changes what the hand is before anything scores it, such as scoring a
	/// bust as the hand it was one card earlier.
	public virtual ScoringHand Reframe(ScoringHand hand) => hand;

	/// The joker's own beat, or null when it has nothing to say about this hand.
	public virtual JokerTrigger? OnScore(ScoringHand hand) => null;

	/// A say in what another joker is about to do. Called with where both sit,
	/// because where a joker sits is what decides whether it has one.
	public virtual JokerTrigger Amplify(JokerTrigger trigger, int ownSlot, int firingSlot) => trigger;

	/// How many extra times every joker's beat plays on this hand.
	public virtual int Retriggers(ScoringHand hand) => 0;

	/// What the win streak adds to Mult, given what this table would make it.
	public virtual int StreakBonus(int streak, int standard) => standard;

	// The table, before scoring.

	/// Changes how the hand settled, after HandResolver has ruled on it.
	public virtual HandSettlement Overrule(HandSettlement settlement, Hand hand) => settlement;

	/// Cash the joker pays on this hand, win or lose.
	public virtual int Payout(Hand hand, HandSettlement settlement) => 0;

	public virtual bool PushExtendsStreak => false;
	public virtual bool PushBreaksStreak => false;

	/// Whether a loss leaves the streak standing. Asked once per loss, and the
	/// joker decides whether this is a loss it covers.
	public virtual bool ShieldLoss() => false;

	/// Sees every hand once the table has settled it, bust or not, just before
	/// it scores. A joker that grows counts here, so the hand that grows it
	/// already scores the new count.
	public virtual void OnSettled(ScoringHand hand)
	{
	}

	/// Whether this hand clears the level outright, whatever it scored.
	public virtual bool ClearsLevel(ScoringHand hand) => false;

	// The run.

	/// Runs once, the moment the joker joins the rail.
	public virtual void OnAcquired(RunState run)
	{
	}

	/// Runs at the start of every level, before its first hand.
	public virtual void OnLevelStart(RunState run)
	{
	}

	public virtual int ExtraHands => 0;
	public virtual int InterestCapBonus => 0;

	/// Bends the table's rules in the player's favour.
	public virtual HouseRules Bend(HouseRules rules) => rules;

	/// Whether the dealer's hole card is dealt face up.
	public virtual bool Peek => false;

	/// How debt grows at a level's start, given what the house would charge.
	public virtual int GrowDebt(int debt, int standard) => standard;

	/// Whether the boss's special rule is switched off this level.
	public virtual bool DisablesBossRule => false;

	public virtual bool FreeFirstPackReroll => false;

	/// Whether the player may put the whole bankroll on one hand, once a level.
	public virtual bool AllowsAllIn => false;
}
