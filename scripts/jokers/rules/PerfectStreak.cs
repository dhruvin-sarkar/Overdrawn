using System;
using Overdrawn.Scoring;

namespace Overdrawn.Jokers;

/// The streak bonus doubles with each win instead of adding one: +1, +2, +4,
/// and on up to the usual ten stacks. A push breaks the streak as a loss does.
public sealed class PerfectStreak : JokerRule
{
	public override string Id => "perfect_streak";

	public override bool PushBreaksStreak => true;

	public override int StreakBonus(int streak, int standard)
	{
		int stacks = Math.Min(streak, ScoreCalculator.MaxStreakBonus);
		return stacks == 0 ? 0 : 1 << (stacks - 1);
	}
}
