using Overdrawn.Blackjack;

namespace Overdrawn.Bosses;

/// <summary>
/// What one boss dealer does to a level, as plain C#. A boss is a real casino
/// house rule turned against the player, so most of them only bend the table's
/// rules; a few reach for the shoe, the minimum wager, the score or the debt.
/// The name and the line the player reads live on the BossCard with the same id.
/// </summary>
public abstract class BossRule
{
	public abstract string Id { get; }

	/// The level's target as a multiple of the one before it.
	public abstract double TargetMultiplier { get; }

	public virtual HouseRules Bend(HouseRules rules) => rules;

	/// The table minimum, given what an ordinary table at this level takes.
	public virtual int MinimumWager(int standard) => standard;

	/// Whether a card stays out of the shoe for the level.
	public virtual bool Strikes(PlayingCard card) => false;

	/// How many times over the base Chips count at this table.
	public virtual int BaseChipsScale => 1;

	/// What the debt becomes the moment the player sits down.
	public virtual int DebtOnArrival(int debt) => debt;

	/// Whether clearing the level clears the debt with it.
	public virtual bool WipesDebtOnClear => false;
}
