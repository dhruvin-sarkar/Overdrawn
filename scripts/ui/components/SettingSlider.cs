using Godot;

namespace Overdrawn.UI;

/// <summary>
/// A titled slider with its number shown in a pill beside the bar. The scene
/// sets a 0-100 range; anything with its own range sets one.
/// </summary>
public partial class SettingSlider : VBoxContainer
{
	[Export] public string Title { get; set; } = "";

	[Signal] public delegate void ValueChangedEventHandler(int value);

	private HSlider _slider = null!;
	private Label _number = null!;

	public int Value => (int)_slider.Value;

	public override void _Ready()
	{
		GetNode<Label>("%Title").Text = Title;
		_slider = GetNode<HSlider>("%Slider");
		_number = GetNode<Label>("%Number");

		_slider.ValueChanged += value =>
		{
			_number.Text = ((int)value).ToString();
			EmitSignal(SignalName.ValueChanged, (int)value);
		};
	}

	/// For a slider whose range is not fixed in the scene, such as a wager
	/// whose bounds grow every level.
	public void SetRange(int minimum, int maximum, int step)
	{
		_slider.MinValue = minimum;
		_slider.MaxValue = maximum;
		_slider.Step = step;
	}

	public void SetValue(int value)
	{
		_slider.SetValueNoSignal(value);
		_number.Text = value.ToString();
	}
}
