using System;
using Overdrawn.Run;
using Overdrawn.Scoring;

namespace Overdrawn.Jokers;

/// Permanent +1 Mult per 3 consecutive wins this run, capped at +10. It grows
/// on the win that completes each run of three and never shrinks, even once
/// the streak that built it is broken.
public sealed class HotStreak : JokerRule
{
	public const int WinsPerStack = 3;
	public const int Cap = 10;

	/// The streak as of the last hand, so a streak that a push or a shield has
	/// held still is not counted twice.
	private int _streakSeen;

	public override string Id => "hot_streak";

	public int Stacks { get; private set; }

	/// The streak it joins at counts as seen, so only a win after it arrives can
	/// complete a three.
	public override void OnAcquired(RunState run) => _streakSeen = run.WinStreak;

	public override void OnSettled(ScoringHand hand)
	{
		bool grew = hand.WinStreak > _streakSeen;
		_streakSeen = hand.WinStreak;
		if (grew && hand.WinStreak % WinsPerStack == 0)
		{
			Stacks = Math.Min(Cap, Stacks + 1);
		}
	}

	public override JokerTrigger? OnScore(ScoringHand hand) => Stacks == 0 ? null : JokerTrigger.AddMult(Stacks);
}
