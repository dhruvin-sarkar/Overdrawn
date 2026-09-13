using Godot;

namespace Overdrawn.UI;

/// <summary>
/// Draws the theme's ProgressBar fill across a fraction of its width.
/// </summary>
public partial class FillBar : Control
{
	private float _fraction;

	public float Fraction
	{
		get => _fraction;
		set
		{
			_fraction = Mathf.Clamp(value, 0f, 1f);
			QueueRedraw();
		}
	}

	public override void _Draw()
	{
		if (_fraction > 0f)
		{
			GetThemeStylebox("fill", "ProgressBar")
				.Draw(GetCanvasItem(), new Rect2(Vector2.Zero, new Vector2(Size.X * _fraction, Size.Y)));
		}
	}
}
