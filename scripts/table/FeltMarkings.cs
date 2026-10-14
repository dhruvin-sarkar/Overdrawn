using System.Collections.Generic;
using Godot;
using Overdrawn.Blackjack;

namespace Overdrawn.Table;

/// <summary>
/// The layout printed on the felt: the dealer's arc, the seat's betting circle,
/// and the house rules. A real table prints its terms where the players can read
/// them, so a boss dealer who changes one is read here before the first card.
/// </summary>
public sealed partial class FeltMarkings : Control
{
	[ExportGroup("Arc")]
	/// The apex of the dealer's arc, as a fraction of the felt's height.
	[Export] public float Apex { get; set; } = 0.545f;
	/// How far the arc falls from its apex to the felt's edge, as a fraction of height.
	[Export] public float Drop { get; set; } = 0.15f;
	/// The gap between the two arcs the terms are printed between.
	[Export] public float Band { get; set; } = 50f;
	[Export] public float LineWidth { get; set; } = 3f;

	[ExportGroup("Print")]
	[Export] public Color Ink { get; set; } = new(1f, 0.93f, 0.78f, 0.45f);
	[Export] public int PrintSize { get; set; } = 22;

	private const int ArcPoints = 192;

	private HouseRules _rules = HouseRules.Standard;

	public override void _Ready() => Resized += QueueRedraw;

	/// The terms this table is dealt under. Printing them is how they are read.
	public void ShowRules(HouseRules rules)
	{
		_rules = rules;
		QueueRedraw();
	}

	public override void _Draw()
	{
		float width = Size.X;
		float half = width * 0.5f;
		float drop = Size.Y * Drop;
		float apex = Size.Y * Apex;

		// The band the terms are printed in, swept from a centre far below the
		// felt so only its crown crosses the table.
		float radius = ((half * half) + (drop * drop)) / (2f * drop);
		Vector2 centre = new(half, apex + radius);
		Sweep(centre, radius);
		Sweep(centre, radius - Band);

		DrawString(GetThemeDefaultFont(), new Vector2(0f, apex + PrintSize + 12f), Terms(_rules),
			HorizontalAlignment.Center, width, PrintSize, Ink);
	}

	/// One arc of the band, carried across the whole width of the felt.
	private void Sweep(Vector2 centre, float radius)
	{
		float edge = Mathf.Acos(Mathf.Clamp(-centre.X / radius, -1f, 1f));
		DrawArc(centre, radius, -edge, edge - Mathf.Pi, ArcPoints, Ink, LineWidth, true);
	}

	/// What the player can read off the table before the first card is dealt.
	private static string Terms(HouseRules rules)
	{
		var print = new List<string>
		{
			Mathf.IsEqualApprox((float)rules.BlackjackPayout, 1.5f)
				? "BLACKJACK PAYS 3 TO 2"
				: "BLACKJACK PAYS EVEN MONEY",
			rules.DealerHitsSoft17
				? "DEALER HITS SOFT 17"
				: "DEALER MUST STAND ON ALL 17s",
		};

		if (rules.InsuranceAllowed)
		{
			print.Add("INSURANCE PAYS 2 TO 1");
		}

		if (rules.DealerWinsTies)
		{
			print.Add("THE DEALER TAKES PUSHES");
		}

		if (!rules.SplitAllowed)
		{
			print.Add("NO SPLITTING");
		}

		return string.Join("   •   ", print);
	}
}
