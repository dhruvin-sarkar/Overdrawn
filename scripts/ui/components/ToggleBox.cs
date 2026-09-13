using Godot;

namespace Overdrawn.UI;

/// <summary>
/// A square check box: a dark well with a white rim that fills red and shows a
/// tick when on.
/// </summary>
public partial class ToggleBox : MenuButton
{
	[Export] public Color Well { get; set; } = new(0.137f, 0.176f, 0.188f);
	[Export] public Color Rim { get; set; } = Colors.White;
	[Export] public Color Filled { get; set; } = new(0.914f, 0.306f, 0.247f);

	private readonly StyleBoxFlat _box = new();

	public override void _Ready()
	{
		base._Ready();
		ToggleMode = true;
		Flat = true;
		_box.SetBorderWidthAll(5);
		_box.SetCornerRadiusAll(14);
		_box.BorderColor = Rim;
		Toggled += _ => QueueRedraw();
	}

	public override void _Draw()
	{
		_box.BgColor = ButtonPressed ? Filled : Well;
		_box.Draw(GetCanvasItem(), new Rect2(Vector2.Zero, Size));

		if (ButtonPressed)
		{
			DrawPolyline(new[]
			{
				Size * new Vector2(0.26f, 0.52f),
				Size * new Vector2(0.43f, 0.7f),
				Size * new Vector2(0.75f, 0.3f),
			}, Rim, Size.X * 0.11f);
		}
	}
}
