using System;
using System.Collections.Generic;
using System.Linq;
using Overdrawn.Blackjack;

namespace Overdrawn.Bosses;

/// Combines two other boss rules at random. Both are in force at once, and the
/// table asks for No Mercy's own multiplier rather than either of theirs.
public sealed class NoMercy : BossRule
{
	public NoMercy(BossRule first, BossRule second)
	{
		if (first.Id == second.Id)
		{
			throw new ArgumentException("No Mercy combines two different rules.", nameof(second));
		}

		Rules = new[] { first, second };
	}

	public override string Id => "no_mercy";
	public override double TargetMultiplier => 2.2;

	/// The two bosses whose rules this table deals by.
	public IReadOnlyList<BossRule> Rules { get; }

	public override int BaseChipsScale => Rules.Aggregate(1, (scale, rule) => scale * rule.BaseChipsScale);
	public override bool WipesDebtOnClear => Rules.Any(rule => rule.WipesDebtOnClear);

	public static NoMercy Draw(Random random)
	{
		List<string> pool = BossRules.SingleRules.ToList();
		int first = random.Next(pool.Count);
		int second = random.Next(pool.Count - 1);
		if (second >= first)
		{
			second++;
		}

		return new NoMercy(BossRules.Create(pool[first], random), BossRules.Create(pool[second], random));
	}

	public override HouseRules Bend(HouseRules rules) => Rules.Aggregate(rules, (bent, rule) => rule.Bend(bent));

	public override int MinimumWager(int standard) =>
		Rules.Aggregate(standard, (minimum, rule) => rule.MinimumWager(minimum));

	public override bool Strikes(PlayingCard card) => Rules.Any(rule => rule.Strikes(card));

	public override int DebtOnArrival(int debt) => Rules.Aggregate(debt, (owed, rule) => rule.DebtOnArrival(owed));
}
