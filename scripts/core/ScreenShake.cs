using Godot;

namespace Overdrawn.Core;

/// <summary>
/// Trauma-based rumble applied to the whole canvas. Every click adds a small
/// kick; bigger moments can call Kick with more. Scaled by the Screenshake
/// setting and switched off by Reduced Motion.
/// </summary>
public partial class ScreenShake : Node
{
	[Export] public float ClickTrauma { get; set; } = 0.32f;
	[Export] public float MaxOffset { get; set; } = 9f;
	[Export] public float MaxRotationDegrees { get; set; } = 0.35f;
	[Export] public float Decay { get; set; } = 3.2f;
	[Export] public float Frequency { get; set; } = 32f;

	public static ScreenShake Instance { get; private set; } = null!;

	private readonly FastNoiseLite _noise = new() { NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex };
	private float _trauma;
	private float _time;
	private bool _shaking;

	public override void _EnterTree() => Instance = this;

	public void Kick(float trauma)
	{
		Settings settings = Settings.Instance;
		if (settings.ReducedMotion || settings.Screenshake == 0)
		{
			return;
		}

		_trauma = Mathf.Min(1f, _trauma + trauma);
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseButton { Pressed: true } || @event.IsActionPressed("ui_accept"))
		{
			Kick(ClickTrauma);
		}
	}

	public override void _Process(double delta)
	{
		Viewport viewport = GetViewport();
		if (_trauma <= 0f)
		{
			if (_shaking)
			{
				viewport.CanvasTransform = Transform2D.Identity;
				_shaking = false;
			}

			return;
		}

		float dt = (float)delta;
		_time += dt * Frequency;
		_trauma = Mathf.Max(0f, _trauma - Decay * dt);

		// Squared trauma keeps small kicks subtle and big ones punchy.
		float shake = _trauma * _trauma * Settings.Instance.Screenshake / 50f;
		Vector2 offset = new Vector2(_noise.GetNoise2D(_time, 0f), _noise.GetNoise2D(0f, _time)) * MaxOffset * shake;
		float angle = Mathf.DegToRad(MaxRotationDegrees) * shake * _noise.GetNoise2D(_time, _time);

		Vector2 centre = viewport.GetVisibleRect().Size * 0.5f;
		viewport.CanvasTransform = new Transform2D(angle, offset + centre) * new Transform2D(0f, -centre);
		_shaking = true;
	}
}
