using System;
using System.Collections.Generic;
using Godot;
using Overdrawn.Cards;
using Overdrawn.Content;
using Overdrawn.Profiles;
using Overdrawn.Run;
using Overdrawn.Table;

namespace Overdrawn.UI;

/// <summary>
/// Drives the opening sequence: the title and a randomly drawn ace or joker burn
/// into view first, then the surrounding menu furniture fades up once they have landed.
/// </summary>
public partial class MainMenu : Control
{
	[Export] public CardStyleLibrary CardStyles { get; set; } = null!;
	[Export] public PackedScene OptionsScene { get; set; } = null!;
	[Export] public PackedScene ProfileScene { get; set; } = null!;
	[Export] public PackedScene RunStartScene { get; set; } = null!;
	[Export] public StartingDeckLibrary StartingDecks { get; set; } = null!;
	[Export] public PackedScene TableScene { get; set; } = null!;
	[Export] public float BurnDuration { get; set; } = 3.2f;
	[Export] public float FurnitureDelay { get; set; } = 2.0f;
	[Export] public float FurnitureDuration { get; set; } = 0.45f;

	private TextureRect _title = null!;
	private ShaderMaterial _titleMaterial = null!;
	private Control _cardSlot = null!;
	private Card _card = null!;
	private Button _profileButton = null!;
	private Control[] _furniture = null!;

	public override void _Ready()
	{
		_title = GetNode<TextureRect>("Title");
		_titleMaterial = (ShaderMaterial)_title.Material;
		_cardSlot = GetNode<Control>("CardSlot");
		_card = GetNode<Card>("CardSlot/Card");
		_profileButton = GetNode<Button>("ProfileBar/Margin/Column/Slot");

		_cardSlot.Resized += CentreCard;
		CentreCard();
		_card.SnapToHome();
		DrawMenuCard();

		ProfileSlots.Instance.Changed += ShowProfileName;
		ShowProfileName();

		_furniture = new[]
		{
			GetNode<Control>("ButtonBar"),
			GetNode<Control>("ProfileBar"),
		};

		var overlay = GetNode<MenuOverlay>("Overlay");
		overlay.RunRequested += StartRun;
		GetNode<Button>("ButtonBar/Margin/Row/Play").Pressed += () => overlay.Open(RunStartScene);
		GetNode<Button>("ButtonBar/Margin/Row/Options").Pressed += () => overlay.Open(OptionsScene);
		_profileButton.Pressed += () => overlay.Open(ProfileScene);
		GetNode<Button>("ButtonBar/Margin/Row/Quit").Pressed += () => GetTree().Quit();

		PlayIntro();
	}

	/// Deals the player in and sits them down at the first table. The run is
	/// plain C# so it is handed to the table directly rather than through a
	/// scene change.
	private void StartRun(string startingDeckId)
	{
		StartingDeck deck = StartingDecks.Find(startingDeckId);
		RunState run = RunState.Begin(deck.Id, deck.BuildDeck(), (int)GD.Randi(), deck.Terms());

		var table = TableScene.Instantiate<TableScreen>();
		GetTree().Root.AddChild(table);
		GetTree().CurrentScene = table;
		table.Begin(run);
		QueueFree();
	}

	private void CentreCard() => _card.Position = _cardSlot.Size * 0.5f;

	private void ShowProfileName() => _profileButton.Text = ProfileSlots.Instance.Current.Name;

	/// Any style's ace of any suit, or any style's joker.
	private void DrawMenuCard()
	{
		var faces = new List<Action>();
		foreach (CardStyle style in CardStyles.Styles)
		{
			foreach (Suit suit in Enum.GetValues<Suit>())
			{
				faces.Add(() => _card.ShowFace(style, suit, Rank.Ace));
			}

			foreach (Texture2D joker in style.Jokers)
			{
				faces.Add(() => _card.ShowJoker(joker));
			}
		}

		faces[(int)(GD.Randi() % faces.Count)]();
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
