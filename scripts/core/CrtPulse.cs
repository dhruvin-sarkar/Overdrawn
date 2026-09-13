using Godot;

namespace Overdrawn.Core;

/// <summary>
/// Jolts the CRT on every click: scanlines, colour split and noise flare up and
/// settle back within a moment. The shader scales the jolt by the CRT setting.
/// </summary>
public partial class CrtPulse : Node
{
	[Export] public float ClickPulse { get; set; } = 1f;
	[Export] public float Decay { get; set; } = 3.5f;

	private float _pulse;

	public override void _Ready() => SetProcess(false);

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseButton { Pressed: true } || @event.IsActionPressed("ui_accept"))
		{
			_pulse = Mathf.Min(1f, _pulse + ClickPulse);
			SetProcess(true);
		}
	}

	public override void _Process(double delta)
	{
		_pulse = Mathf.Max(0f, _pulse - Decay * (float)delta);
		RenderingServer.GlobalShaderParameterSet("crt_pulse", _pulse * _pulse);
		SetProcess(_pulse > 0f);
	}
}
