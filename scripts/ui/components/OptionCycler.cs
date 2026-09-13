using System;
using System.Collections.Generic;
using Godot;

namespace Overdrawn.UI;

/// <summary>
/// Steps through a fixed list of choices with arrow buttons, wrapping at either
/// end. Dots under the value show the position in the list.
/// </summary>
public partial class OptionCycler : VBoxContainer
{
	[Export] public string Title { get; set; } = "";
	[Export] public float ValueWidth { get; set; } = 300f;

	[Signal] public delegate void IndexChangedEventHandler(int index);

	public int Index { get; private set; }

	private IReadOnlyList<string> _options = Array.Empty<string>();
	private bool _locked;
	private Button _previous = null!;
	private Button _next = null!;
	private Control _value = null!;
	private Label _valueLabel = null!;
	private PageDots _dots = null!;

	public override void _Ready()
	{
		Label title = GetNode<Label>("%Title");
		title.Text = Title;
		title.Visible = Title != "";

		_previous = GetNode<Button>("%Previous");
		_next = GetNode<Button>("%Next");
		_value = GetNode<Control>("%Value");
		_valueLabel = GetNode<Label>("%ValueLabel");
		_dots = GetNode<PageDots>("%Dots");

		_value.CustomMinimumSize = new Vector2(ValueWidth, _value.CustomMinimumSize.Y);
		_value.Resized += () => _value.PivotOffset = _value.Size * 0.5f;
		_previous.Pressed += () => Step(-1);
		_next.Pressed += () => Step(1);
	}

	public void Setup(IReadOnlyList<string> options, int index)
	{
		if (index < 0 || index >= options.Count)
		{
			throw new ArgumentOutOfRangeException(nameof(index), index, $"'{Title}' has {options.Count} options.");
		}

		_options = options;
		Index = index;
		Refresh();
	}

	/// A locked cycler still shows its value but its arrows do nothing.
	public void SetLocked(bool locked)
	{
		_locked = locked;
		Refresh();
	}

	private void Step(int direction)
	{
		Index = (Index + direction + _options.Count) % _options.Count;
		Refresh();

		_value.Scale = new Vector2(1.08f, 1.08f);
		_value.CreateTween().TweenProperty(_value, "scale", Vector2.One, 0.18f)
			.SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);

		EmitSignal(SignalName.IndexChanged, Index);
	}

	private void Refresh()
	{
		_valueLabel.Text = _options[Index];
		_dots.SetDots(_options.Count, Index);
		bool stuck = _locked || _options.Count < 2;
		_previous.Disabled = stuck;
		_next.Disabled = stuck;
	}
}
