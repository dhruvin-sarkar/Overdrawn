using Godot;

namespace Overdrawn.UI;

/// <summary>
/// A label with a check box beside it.
/// </summary>
public partial class SettingToggle : HBoxContainer
{
	[Export] public string Title { get; set; } = "";

	[Signal] public delegate void SwitchedEventHandler(bool on);

	private ToggleBox _box = null!;

	public override void _Ready()
	{
		GetNode<Label>("%Title").Text = Title;
		_box = GetNode<ToggleBox>("%Box");
		_box.Toggled += on => EmitSignal(SignalName.Switched, on);
	}

	public void SetOn(bool on)
	{
		_box.SetPressedNoSignal(on);
		_box.QueueRedraw();
	}
}
