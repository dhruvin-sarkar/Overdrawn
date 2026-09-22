using Godot;
using Overdrawn.Audio;

namespace Overdrawn.UI;

/// <summary>
/// Gives a menu button its feel: it swells on hover and dips when held. Colour
/// stays in the scene's StyleBox, so a button only needs one style defined.
/// Scale is used rather than position so container layout is never fought.
/// </summary>
public partial class MenuButton : Button
{
	[Export] public float HoverScale { get; set; } = 1.05f;
	[Export] public float PressScale { get; set; } = 0.96f;
	[Export] public float Response { get; set; } = 20f;
	[Export] public StringName PressCue { get; set; } = "button";
	[Export] public StringName HoverCue { get; set; } = "hover";

	private float _scale = 1f;
	private bool _hovered;
	private bool _held;

	public override void _Ready()
	{
		RecentrePivot();
		Resized += RecentrePivot;
		MouseEntered += () =>
		{
			_hovered = true;
			if (!Disabled)
			{
				Sfx.Instance.Play(HoverCue);
			}
		};
		MouseExited += () =>
		{
			_hovered = false;
			_held = false;
		};
		ButtonDown += () =>
		{
			_held = true;
			Sfx.Instance.Play(PressCue);
		};
		ButtonUp += () => _held = false;
	}

	public override void _Process(double delta)
	{
		float target = _held ? PressScale : _hovered ? HoverScale : 1f;
		_scale = Mathf.Lerp(_scale, target, 1f - Mathf.Exp(-Response * (float)delta));
		Scale = new Vector2(_scale, _scale);
	}

	private void RecentrePivot() => PivotOffset = Size * 0.5f;
}
