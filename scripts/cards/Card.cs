using System;
using Godot;
using Overdrawn.Audio;
using Overdrawn.Core;

namespace Overdrawn.Cards;

/// <summary>
/// A playing card that leans toward the cursor with a faked 3D perspective,
/// pops when hovered, and can be picked up and dragged with a projected shadow.
/// The node itself marks the card's home; the visible face chases the cursor
/// while held and springs back home otherwise, so owners such as a menu slot or
/// a CardPath only ever move the node.
/// </summary>
public partial class Card : Node2D
{
	[Export] public Vector2 Size { get; set; } = new(142, 199);

	[ExportGroup("Tilt")]
	[Export] public float MaxTilt { get; set; } = 18f;
	[Export] public float IdleSway { get; set; } = 3f;
	[Export] public float TiltRate { get; set; } = 24f;

	[ExportGroup("Pop")]
	[Export] public float HoverScale { get; set; } = 1.06f;
	[Export] public float PopScale { get; set; } = 1.14f;
	[Export] public float DragScale { get; set; } = 1.18f;
	[Export] public float PopDegrees { get; set; } = 5f;

	[ExportGroup("Movement")]
	[Export] public float FollowRate { get; set; } = 30f;
	[Export] public float ReturnStiffness { get; set; } = 320f;
	[Export] public float ReturnDamping { get; set; } = 24f;
	/// Degrees of in-plane lean per pixel per second of movement.
	[Export] public float LeanPerSpeed { get; set; } = 0.012f;
	[Export] public float MaxLean { get; set; } = 16f;
	[Export] public float DragTiltPerSpeed { get; set; } = 0.03f;
	[Export] public float MaxDragTilt { get; set; } = 26f;

	[ExportGroup("Shadow")]
	[Export] public float ShadowAlpha { get; set; } = 0.35f;
	/// Shadow drop as a fraction of card height, resting and lifted.
	[Export] public float ShadowRestDrop { get; set; } = 0.03f;
	[Export] public float ShadowLiftDrop { get; set; } = 0.13f;
	/// Sideways shadow offset in pixels at the edge of the screen.
	[Export] public float ShadowSpread { get; set; } = 40f;

	/// The card currently picked up, so only one moves at a time.
	public static Card? Held { get; private set; }

	public bool IsDragging => Held == this;

	/// Where the face is drawn right now, trailing the home position.
	public Vector2 FaceGlobalPosition => _facePosition;

	private SubViewport _viewport = null!;
	private TextureRect _art = null!;
	private ShaderMaterial _artMaterial = null!;
	private Sprite2D _face = null!;
	private Sprite2D _shadow = null!;
	private Control _hitBox = null!;

	private CardStyle? _style;
	private Suit _suit;
	private Rank _rank;
	private Texture2D? _joker;

	private Vector2 _pointer;
	private Vector2 _facePosition;
	private Vector2 _velocity;
	private Vector2 _grabOffset;
	private Vector2 _tilt;
	private float _lean;
	private float _scale = 1f;
	private float _punch;
	private float _lift;
	private float _dissolve = 1f;
	private float _swayPhase;
	private bool _hovered;
	private Tween? _scaleTween;
	private Tween? _punchTween;

	public override void _Ready()
	{
		_viewport = GetNode<SubViewport>("FaceViewport");
		_art = GetNode<TextureRect>("FaceViewport/Art");
		_artMaterial = (ShaderMaterial)_art.Material;
		_face = GetNode<Sprite2D>("Face");
		_shadow = GetNode<Sprite2D>("Shadow");
		_hitBox = GetNode<Control>("HitBox");

		// The tilt shader sizes its quad from the texture, so the face renders at card size.
		_viewport.Size = (Vector2I)Size.Round();
		Texture2D faceTexture = _viewport.GetTexture();
		_face.Texture = faceTexture;
		_shadow.Texture = faceTexture;

		_hitBox.Size = Size;
		_hitBox.PivotOffset = Size * 0.5f;
		_hitBox.MouseEntered += OnMouseEntered;
		_hitBox.MouseExited += OnMouseExited;
		_hitBox.GuiInput += OnHitBoxInput;

		_facePosition = GlobalPosition;
		_swayPhase = GD.Randf() * Mathf.Tau;
	}

	public override void _EnterTree() => Settings.Instance.Changed += OnSettingsChanged;

	public override void _ExitTree()
	{
		Settings.Instance.Changed -= OnSettingsChanged;
		if (IsDragging)
		{
			Held = null;
		}
	}

	/// Places the face at home immediately, for when the home is first laid out.
	public void SnapToHome()
	{
		_facePosition = GlobalPosition;
		_velocity = Vector2.Zero;
	}

	public void ShowFace(CardStyle style, Suit suit, Rank rank)
	{
		_style = style;
		_suit = suit;
		_rank = rank;
		_joker = null;
		RefreshArt();
	}

	public void ShowJoker(Texture2D joker)
	{
		_joker = joker;
		_style = null;
		RefreshArt();
	}

	/// 0 = fully burned away, 1 = fully present.
	public void SetDissolve(float progress)
	{
		_dissolve = progress;
		_face.SetInstanceShaderParameter("progress", progress);
	}

	/// A quick in-plane wobble, used for hover pops and when shoved aside.
	public void Punch(float degrees)
	{
		_punchTween?.Kill();
		_punchTween = CreateTween();
		Callable setPunch = Callable.From<float>(value => _punch = value);
		_punchTween.TweenMethod(setPunch, _punch, degrees, 0.04f);
		_punchTween.TweenMethod(setPunch, degrees, -degrees * 0.5f, 0.06f);
		_punchTween.TweenMethod(setPunch, -degrees * 0.5f, 0f, 0.1f);
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouse mouse)
		{
			_pointer = GetCanvasTransform().AffineInverse() * mouse.Position;
		}

		if (IsDragging && @event is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false })
		{
			Held = null;
			Sfx.Instance.Play("card_place");
			ScaleTo(_hovered ? HoverScale : 1f);
		}
	}

	public override void _Process(double delta)
	{
		float dt = Mathf.Min((float)delta, 1f / 30f);
		float motion = Settings.Instance.ReducedMotion ? 0f : 1f;

		if (IsDragging)
		{
			Vector2 previous = _facePosition;
			_facePosition = _facePosition.Lerp(_pointer - _grabOffset, Damp(FollowRate, dt));
			_velocity = _velocity.Lerp((_facePosition - previous) / dt, Damp(26f, dt));
		}
		else
		{
			Vector2 acceleration = (GlobalPosition - _facePosition) * ReturnStiffness - _velocity * ReturnDamping;
			_velocity += acceleration * dt;
			_facePosition += _velocity * dt;
		}

		float leanTarget = Mathf.Clamp(_velocity.X * LeanPerSpeed, -MaxLean, MaxLean) * motion;
		_lean = Mathf.Lerp(_lean, leanTarget, Damp(20f, dt));
		_tilt = _tilt.Lerp(TiltTarget(motion), Damp(TiltRate, dt));
		_lift = Mathf.Lerp(_lift, IsDragging ? 1f : 0f, Damp(12f, dt));

		float rotation = Mathf.DegToRad(_lean + _punch);
		Vector2 local = ToLocal(_facePosition);

		_face.Position = local;
		_face.Rotation = rotation;
		_face.Scale = Vector2.One * _scale;
		_face.SetInstanceShaderParameter("y_rot", _tilt.X);
		_face.SetInstanceShaderParameter("x_rot", _tilt.Y);

		float screenCentre = GetViewportRect().Size.X * 0.5f;
		float spread = (_facePosition.X - screenCentre) / screenCentre * ShadowSpread * (0.3f + 0.7f * _lift);
		float drop = Size.Y * Mathf.Lerp(ShadowRestDrop, ShadowLiftDrop, _lift);
		_shadow.Position = local + new Vector2(spread, drop);
		_shadow.Rotation = rotation;
		_shadow.Scale = Vector2.One * _scale;
		_shadow.Modulate = new Color(0f, 0f, 0f, ShadowAlpha * _dissolve);
		_shadow.Visible = Settings.Instance.Shadows;

		_hitBox.Position = local - Size * 0.5f;
		_hitBox.Rotation = rotation;
		_hitBox.Scale = Vector2.One * _scale;

		bool away = _facePosition.DistanceTo(GlobalPosition) > 4f;
		ZIndex = IsDragging || away ? 10 : 0;
	}

	/// Where the face should be leaning this frame, in degrees (y_rot, x_rot).
	private Vector2 TiltTarget(float motion)
	{
		Vector2 target = Vector2.Zero;
		if (IsDragging || _hovered)
		{
			// The face leans away from the cursor, and keeps leaning while held
			// because it trails the cursor by however far it is behind.
			Vector2 centred = ((_pointer - _facePosition) / (Size * 0.5f * _scale)).Clamp(-Vector2.One, Vector2.One);
			target = new Vector2(centred.X * MaxTilt, -centred.Y * MaxTilt);
		}

		if (IsDragging)
		{
			target += new Vector2(
				Mathf.Clamp(_velocity.X * DragTiltPerSpeed, -MaxDragTilt, MaxDragTilt),
				Mathf.Clamp(-_velocity.Y * DragTiltPerSpeed, -MaxDragTilt, MaxDragTilt));
		}

		float seconds = Time.GetTicksMsec() / 1000f;
		float sway = IdleSway * motion * (_hovered || IsDragging ? 0.2f : 1f);
		return target + new Vector2(Mathf.Sin(seconds + _swayPhase), Mathf.Cos(seconds * 0.8f + _swayPhase)) * sway;
	}

	private void OnMouseEntered()
	{
		_hovered = true;
		if (Held == null)
		{
			Sfx.Instance.Play("card_hover");
			Pop();
		}
	}

	private void OnMouseExited()
	{
		_hovered = false;
		if (!IsDragging)
		{
			ScaleTo(1f);
		}
	}

	private void OnHitBoxInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } && Held == null)
		{
			Held = this;
			Sfx.Instance.Play("card_pick");
			_grabOffset = _pointer - _facePosition;
			ScaleTo(DragScale);
			_hitBox.AcceptEvent();
		}
	}

	private void Pop()
	{
		_scaleTween?.Kill();
		_scaleTween = CreateTween();
		Callable setScale = Callable.From<float>(SetFaceScale);
		_scaleTween.TweenMethod(setScale, _scale, PopScale, 0.06f)
			.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
		_scaleTween.TweenMethod(setScale, PopScale, HoverScale, 0.14f)
			.SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
		Punch(GD.Randf() < 0.5f ? -PopDegrees : PopDegrees);
	}

	private void ScaleTo(float target)
	{
		_scaleTween?.Kill();
		_scaleTween = CreateTween();
		_scaleTween.TweenMethod(Callable.From<float>(SetFaceScale), _scale, target, 0.15f)
			.SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
	}

	private void SetFaceScale(float value) => _scale = value;

	private void OnSettingsChanged()
	{
		if (_style != null || _joker != null)
		{
			RefreshArt();
		}
	}

	private void RefreshArt()
	{
		CardArt art = _joker != null
			? new CardArt(_joker, null)
			: (_style ?? throw new InvalidOperationException("Card has no face to show."))
				.Art(_suit, _rank, Settings.Instance.HighContrastCards);

		_art.Texture = art.Texture;
		_artMaterial.SetShaderParameter("atlas_rect", AtlasRect(art.Texture));
		_artMaterial.SetShaderParameter("ink_count", art.Recolour?.Inks.Count ?? 0);

		if (art.Recolour is not { } swap)
		{
			return;
		}

		for (int i = 0; i < swap.Inks.Count; i++)
		{
			_artMaterial.SetShaderParameter($"ink_{i}", Rgb(swap.Inks[i]));
			_artMaterial.SetShaderParameter($"contrast_{i}", Rgb(swap.ContrastInks[i]));
		}

		_artMaterial.SetShaderParameter("region", swap.Region);
		_artMaterial.SetShaderParameter("edge_inset", swap.EdgeInset);
	}

	private static Vector4 AtlasRect(Texture2D texture)
	{
		if (texture is not AtlasTexture atlas)
		{
			return new Vector4(0f, 0f, 1f, 1f);
		}

		Vector2 size = atlas.Atlas.GetSize();
		Rect2 region = atlas.Region;
		return new Vector4(region.Position.X / size.X, region.Position.Y / size.Y, region.Size.X / size.X, region.Size.Y / size.Y);
	}

	private static Vector3 Rgb(Color colour) => new(colour.R, colour.G, colour.B);

	/// Frame-rate independent lerp weight.
	private static float Damp(float rate, float dt) => 1f - Mathf.Exp(-rate * dt);
}
