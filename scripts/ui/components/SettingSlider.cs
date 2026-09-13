using Godot;

namespace Overdrawn.UI;

/// <summary>
/// A titled 0-100 slider with its number shown in a pill beside the bar.
/// </summary>
public partial class SettingSlider : VBoxContainer
{
	[Export] public string Title { get; set; } = "";

	[Signal] public delegate void ValueChangedEventHandler(int value);

	private HSlider _slider = null!;
	private Label _number = null!;

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

	public void SetValue(int value)
	{
		_slider.SetValueNoSignal(value);
		_number.Text = value.ToString();
	}
}
