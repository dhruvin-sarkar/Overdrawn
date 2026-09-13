using Godot;

namespace Overdrawn.UI;

/// <summary>
/// A card that leans toward the cursor with a faked 3D perspective, can be
/// picked up and dragged, and springs back to where it started on release.
/// </summary>
public partial class TiltCard : Control
{
	[Export] public float MaxTilt { get; set; } = 22f;
	[Export] public float TiltSmoothing { get; set; } = 24f;
	[Export] public float DragLean { get; set; } = 0.03f;
	[Export] public float MaxDragLean { get; set; } = 26f;
	[Export] public float ReturnDuration { get; set; } = 0.3f;
	[Export] public float HoverScale { get; set; } = 1.05f;
	[Export] public float DragScale { get; set; } = 1.12f;
	[Export] public float ScaleSmoothing { get; set; } = 20f;

	private SubViewport _faceViewport = null!;
	private TextureRect _art = null!;
	private TextureRect _display = null!;
	private ShaderMaterial _material = null!;

	private Vector2 _pointer;
	private Vector2 _home;
	private bool _homeCaptured;
	private bool _hovered;
	private bool _dragging;
	private Vector2 _grabOffset;
	private Vector2 _lastMouse;
	private Vector2 _dragVelocity;
	private Vector2 _tilt;
	private float _scale = 1f;
	private Tween? _returnTween;

	public override void _Ready()
	{
		_faceViewport = GetNode<SubViewport>("FaceViewport");
		_art = GetNode<TextureRect>("FaceViewport/Art");
		_display = GetNode<TextureRect>("Display");
		_display.Texture = _faceViewport.GetTexture();
		_material = (ShaderMaterial)_display.Material;

		SyncFaceResolution();
		Resized += SyncFaceResolution;

		_pointer = GetGlobalMousePosition();
		MouseFilter = MouseFilterEnum.Stop;
		MouseEntered += () => _hovered = true;
		MouseExited += () => _hovered = false;
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;

		if (!_homeCaptured)
		{
			_home = GlobalPosition;
			PivotOffset = Size * 0.5f;
			_homeCaptured = true;
		}

		Vector2 mouse = _pointer;

		if (_dragging)
		{
			GlobalPosition = mouse - _grabOffset;
			Vector2 frameVelocity = (mouse - _lastMouse) / Mathf.Max(dt, 0.0001f);
			_dragVelocity = _dragVelocity.Lerp(frameVelocity, Damp(26f, dt));
		}

		_lastMouse = mouse;

		_tilt = _tilt.Lerp(TiltTarget(mouse), Damp(TiltSmoothing, dt));
		_material.SetShaderParameter("y_rot", _tilt.X);
		_material.SetShaderParameter("x_rot", _tilt.Y);

		float targetScale = _dragging ? DragScale : _hovered ? HoverScale : 1f;
		_scale = Mathf.Lerp(_scale, targetScale, Damp(ScaleSmoothing, dt));
		Scale = new Vector2(_scale, _scale);
	}

	public override void _GuiInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true })
		{
			BeginDrag();
			AcceptEvent();
		}
	}

	public override void _Input(InputEvent @event)
	{
		// Tracked from events rather than polled, so the card responds to any
		// input source the viewport routes, not just the OS cursor.
		if (@event is InputEventMouseMotion motion)
		{
			_pointer = motion.Position;
		}
		else if (_dragging && @event is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false })
		{
			EndDrag();
		}
	}

	/// Where the card should be leaning this frame, in degrees (y_rot, x_rot).
	private Vector2 TiltTarget(Vector2 mouse)
	{
		if (_dragging)
		{
			return new Vector2(
				Mathf.Clamp(_dragVelocity.X * DragLean, -MaxDragLean, MaxDragLean),
				Mathf.Clamp(-_dragVelocity.Y * DragLean, -MaxDragLean, MaxDragLean));
		}

		if (!_hovered)
		{
			return Vector2.Zero;
		}

		Vector2 centred = ((mouse - GlobalPosition) / Size - Vector2.One * 0.5f) * 2f;
		return new Vector2(centred.X * MaxTilt, -centred.Y * MaxTilt);
	}

	private void BeginDrag()
	{
		_returnTween?.Kill();
		_returnTween = null;
		_dragging = true;
		_dragVelocity = Vector2.Zero;
		_grabOffset = _pointer - GlobalPosition;
		MoveToFront();
	}

	private void EndDrag()
	{
		_dragging = false;
		_dragVelocity = Vector2.Zero;

		_returnTween?.Kill();
		_returnTween = CreateTween();
		_returnTween.SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
		_returnTween.TweenProperty(this, "global_position", _home, ReturnDuration);
	}

	/// 0 = fully burned away, 1 = fully present. Driven by the menu intro.
	public void SetDissolve(float value) => _material.SetShaderParameter("progress", value);

	public void SetFace(Texture2D face) => _art.Texture = face;

	/// The tilt shader sizes its quad from the texture, so the face texture must
	/// match this control.
	private void SyncFaceResolution() => _faceViewport.Size = (Vector2I)Size.Round();

	/// Frame-rate independent lerp weight.
	private static float Damp(float rate, float dt) => 1f - Mathf.Exp(-rate * dt);
}
