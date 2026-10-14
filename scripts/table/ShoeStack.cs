using Godot;

namespace Overdrawn.Table;

/// <summary>
/// The shoe drawn as the pile it is. The stack thins as the cards run out, so
/// a shoe close to its shuffle looks like one without being counted.
/// </summary>
public sealed partial class ShoeStack : Control
{
	/// How many backs are drawn when the shoe is full.
	[Export] public int Depth { get; set; } = 11;
	/// How far each card sits above the one beneath it.
	[Export] public float Lift { get; set; } = 5f;
	[Export] public Vector2 CardSize { get; set; } = new(160f, 214f);
	/// How dark the card at the bottom of the pile sits under the ones above it.
	[Export] public float Shade { get; set; } = 0.45f;

	private Texture2D _back = null!;
	private float _fullness;

	public override void _Ready() => Resized += QueueRedraw;

	/// <param name="fullness">What is left in the shoe, 0 to 1.</param>
	public void ShowStock(Texture2D back, float fullness)
	{
		_back = back;
		_fullness = Mathf.Clamp(fullness, 0f, 1f);
		QueueRedraw();
	}

	public override void _Draw()
	{
		// The table hands the back over in Begin; the first layout pass is earlier.
		if (_back is null)
		{
			return;
		}

		int cards = Mathf.Max(1, Mathf.RoundToInt(Depth * _fullness));

		// Sized so a full shoe just fits, which keeps the card the same size as
		// the pile wears down. The bottom card rests on the table and the stack
		// grows up from it, so the top drops as the shoe empties.
		float height = Size.Y - ((Depth - 1) * Lift);
		float width = height * CardSize.X / CardSize.Y;
		float left = (Size.X - width) * 0.5f;

		for (int i = 0; i < cards; i++)
		{
			// Only the sliver of each card below the next one shows, so shading
			// down the pile is what makes the edge read as depth at all.
			float lit = cards == 1 ? 1f : Mathf.Lerp(Shade, 1f, i / (cards - 1f));
			DrawTextureRect(_back, new Rect2(left, Size.Y - height - (i * Lift), width, height), false,
				new Color(lit, lit, lit));
		}
	}
}
