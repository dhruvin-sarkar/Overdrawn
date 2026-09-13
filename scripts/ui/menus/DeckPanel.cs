using System.Collections.Generic;
using System.Linq;
using Godot;
using Overdrawn.Cards;
using Overdrawn.Core;

namespace Overdrawn.UI;

/// <summary>
/// Customize Deck: pick a card style per suit, previewed on the suit's court
/// cards, which can be dragged and reordered along the table like a real hand.
/// </summary>
public partial class DeckPanel : MenuPanel
{
	private static readonly Rank[] Preview = { Rank.King, Rank.Queen, Rank.Jack };

	[Export] public CardStyleLibrary CardStyles { get; set; } = null!;
	[Export] public PackedScene CardScene { get; set; } = null!;
	[Export] public Vector2 CardSize { get; set; } = new(180, 252);
	[Export] public float PunchDegrees { get; set; } = 8f;

	private readonly List<Card> _cards = new();
	private Control _table = null!;
	private CardPath _path = null!;
	private OptionCycler _style = null!;
	private Suit _suit;

	public override void _Ready()
	{
		base._Ready();
		_table = GetNode<Control>("%Table");
		_path = GetNode<CardPath>("%Path");
		_style = GetNode<OptionCycler>("%Style");

		foreach (Rank _ in Preview)
		{
			Card card = CardScene.Instantiate<Card>();
			card.Size = CardSize;
			_path.AddChild(card);
			_cards.Add(card);
		}

		_table.Resized += LayPath;
		_style.IndexChanged += index =>
		{
			Settings.Instance.SetDeckStyle(_suit, CardStyles.Styles[index].Id);
			ShowCards();
		};

		var highContrast = GetNode<SettingToggle>("%HighContrast");
		highContrast.SetOn(Settings.Instance.HighContrastCards);
		highContrast.Switched += on => Settings.Instance.HighContrastCards = on;

		var tabs = GetNode<TabStrip>("%Tabs");
		tabs.TabSelected += index => ShowSuit((Suit)index);
		tabs.Select((int)Suit.Spades);
	}

	private void ShowSuit(Suit suit)
	{
		_suit = suit;
		string[] names = CardStyles.Styles.Select(style => style.DisplayName).ToArray();
		int picked = CardStyles.Styles.IndexOf(CardStyles.Find(Settings.Instance.DeckStyle(suit)));
		_style.Setup(names, picked);
		ShowCards();
	}

	private void ShowCards()
	{
		CardStyle style = CardStyles.Find(Settings.Instance.DeckStyle(_suit));
		for (int i = 0; i < _cards.Count; i++)
		{
			_cards[i].ShowFace(style, _suit, Preview[i]);
			_cards[i].Punch(i % 2 == 0 ? PunchDegrees : -PunchDegrees);
		}
	}

	/// A straight line across the middle of the table, inset so the end cards
	/// have room to pop.
	private void LayPath()
	{
		float inset = CardSize.X * 0.75f;
		float middle = _table.Size.Y * 0.5f;
		_path.Curve.ClearPoints();
		_path.Curve.AddPoint(new Vector2(inset, middle));
		_path.Curve.AddPoint(new Vector2(_table.Size.X - inset, middle));
	}
}
