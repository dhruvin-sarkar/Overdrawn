using Godot;

namespace Overdrawn.UI;

/// <summary>
/// A row of small dots showing how many choices there are and which is picked.
/// </summary>
public partial class PageDots : Control
{
	[Export] public float Radius { get; set; } = 3.5f;
	[Export] public float Spacing { get; set; } = 12f;
	[Export] public Color Picked { get; set; } = Colors.White;
	[Export] public Color Unpicked { get; set; } = new(0f, 0f, 0f, 0.45f);

	private int _count;
	private int _selected;

	public void SetDots(int count, int selected)
	{
		_count = count;
		_selected = selected;
		QueueRedraw();
	}

	public override void _Draw()
	{
		float start = Size.X * 0.5f - (_count - 1) * Spacing * 0.5f;
		for (int i = 0; i < _count; i++)
		{
			DrawCircle(new Vector2(start + i * Spacing, Size.Y * 0.5f), Radius, i == _selected ? Picked : Unpicked);
		}
	}
}
