using Godot;

namespace Overdrawn.UI;

/// <summary>
/// Drives the opening sequence: the title and card burn into view first, then
/// the surrounding menu furniture fades up once they have landed.
/// </summary>
public partial class MainMenu : Control
{
	[Export] public float BurnDuration { get; set; } = 1.6f;
	[Export] public float FurnitureDelay { get; set; } = 1.0f;
	[Export] public float FurnitureDuration { get; set; } = 0.45f;

	private TextureRect _title = null!;
	private TiltCard _card = null!;
	private ShaderMaterial _titleMaterial = null!;
	private Control[] _furniture = null!;

	public override void _Ready()
	{
		_title = GetNode<TextureRect>("Title");
		_card = GetNode<TiltCard>("Card");
		_titleMaterial = (ShaderMaterial)_title.Material;

		_furniture = new[]
		{
			GetNode<Control>("ButtonBar"),
			GetNode<Control>("ProfileBar"),
			GetNode<Control>("LanguageBar"),
		};

		GetNode<Button>("ButtonBar/Margin/Row/Quit").Pressed += () => GetTree().Quit();

		PlayIntro();
	}

	private void PlayIntro()
	{
		SetTitleProgress(0f);
		_card.SetDissolve(0f);

		foreach (Control piece in _furniture)
		{
			piece.Modulate = new Color(1f, 1f, 1f, 0f);
		}

		Tween tween = CreateTween().SetParallel();
		tween.TweenMethod(Callable.From<float>(SetTitleProgress), 0f, 1f, BurnDuration)
			.SetTrans(Tween.TransitionType.Cubic)
			.SetEase(Tween.EaseType.Out);
		tween.TweenMethod(Callable.From<float>(_card.SetDissolve), 0f, 1f, BurnDuration)
			.SetTrans(Tween.TransitionType.Cubic)
			.SetEase(Tween.EaseType.Out)
			.SetDelay(0.15f);

		foreach (Control piece in _furniture)
		{
			tween.TweenProperty(piece, "modulate:a", 1f, FurnitureDuration)
				.SetDelay(FurnitureDelay);
		}
	}

	private void SetTitleProgress(float value) =>
		_titleMaterial.SetShaderParameter("progress", value);
}
