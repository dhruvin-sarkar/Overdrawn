using Godot;

namespace Overdrawn.UI;

/// <summary>
/// A panel shown by the MenuOverlay. Panels never open or close themselves;
/// they ask by signal, so they stay unaware of how they are presented.
/// Every panel scene has a %Back button.
/// </summary>
public partial class MenuPanel : PanelContainer
{
	[Signal] public delegate void BackPressedEventHandler();
	[Signal] public delegate void OpenRequestedEventHandler(PackedScene scene);

	public override void _Ready() =>
		GetNode<Button>("%Back").Pressed += () => EmitSignal(SignalName.BackPressed);

	protected void RequestOpen(PackedScene scene) => EmitSignal(SignalName.OpenRequested, scene);
}
