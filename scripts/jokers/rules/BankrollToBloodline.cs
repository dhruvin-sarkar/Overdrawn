using Overdrawn.Run;
using Overdrawn.Scoring;

namespace Overdrawn.Jokers;

/// At the start of each level, 10% of the bankroll is spent into permanent
/// Mult at $10 a Mult. Dollars short of a whole Mult carry over to the next.
public sealed class BankrollToBloodline : JokerRule
{
	public const int PercentTaken = 10;
	public const int DollarsPerMult = 10;

	public override string Id => "bankroll_to_bloodline";

	/// Every dollar converted so far this run.
	public int Converted { get; private set; }

	public int Mult => Converted / DollarsPerMult;

	public override void OnLevelStart(RunState run)
	{
		int taken = run.Bankroll * PercentTaken / 100;
		if (taken == 0)
		{
			return;
		}

		run.Spend(taken);
		Converted += taken;
	}

	public override JokerTrigger? OnScore(ScoringHand hand) => Mult == 0 ? null : JokerTrigger.AddMult(Mult);
}
