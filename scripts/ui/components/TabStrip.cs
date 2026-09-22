using System.Collections.Generic;
using Godot;

namespace Overdrawn.UI;

/// <summary>
/// A row of red tab buttons with a bobbing marker above the selected one. The
/// strip only reports which tab was picked; the owner decides what that shows.
/// </summary>
public partial class TabStrip : HBoxContainer
{
	[Export] public string[] Tabs { get; set; } = System.Array.Empty<string>();
	/// Fixed width per tab, or 0 to share the strip's width.
	[Export] public float TabWidth { get; set; }
	[Export] public float TabHeight { get; set; } = 64f;
	[Export] public Color MarkerColour { get; set; } = new(0.914f, 0.306f, 0.247f);
	[Export] public float MarkerSize { get; set; } = 16f;
	[Export] public float MarkerGlide { get; set; } = 18f;

	[Signal] public delegate void TabSelectedEventHandler(int index);

	public int Selected { get; private set; } = -1;

	private readonly List<Button> _buttons = new();
	private float _markerX;
	private bool _markerPlaced;

	public override void _Ready()
	{
		for (int i = 0; i < Tabs.Length; i++)
		{
			int index = i;
			var button = new MenuButton
			{
				Text = Tabs[i],
				ThemeTypeVariation = "RedButton",
				CustomMinimumSize = new Vector2(TabWidth, TabHeight),
				SizeFlagsHorizontal = TabWidth > 0f ? SizeFlags.ShrinkCenter : SizeFlags.ExpandFill,
				PressCue = "tab",
			};
			button.Pressed += () => Select(index);
			AddChild(button);
			_buttons.Add(button);
		}
	}

	public void Select(int index)
	{
		Selected = index;
		EmitSignal(SignalName.TabSelected, index);
	}

	public override void _Process(double delta)
	{
		if (Selected < 0)
		{
			return;
		}

		Button button = _buttons[Selected];
		float target = button.Position.X + button.Size.X * 0.5f;
		_markerX = _markerPlaced ? Mathf.Lerp(_markerX, target, 1f - Mathf.Exp(-MarkerGlide * (float)delta)) : target;
		_markerPlaced = button.Size.X > 0f;
		QueueRedraw();
	}

	public override void _Draw()
	{
		if (Selected < 0)
		{
			return;
		}

		float bob = Mathf.Sin(Time.GetTicksMsec() / 1000f * 5f) * 3f;
		float top = -MarkerSize - 10f + bob;
		DrawColoredPolygon(new[]
		{
			new Vector2(_markerX - MarkerSize * 0.7f, top),
			new Vector2(_markerX + MarkerSize * 0.7f, top),
			new Vector2(_markerX, top + MarkerSize),
		}, MarkerColour);
	}
}
