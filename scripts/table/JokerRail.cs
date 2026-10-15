using System;
using System.Collections.Generic;
using Godot;
using Overdrawn.Cards;
using Overdrawn.Content;
using Overdrawn.Scoring;
using Overdrawn.UI;

namespace Overdrawn.Table;

/// <summary>
/// The jokers on the felt, left to right in the order they fire. Dragging one
/// along the row moves it to another slot; when one fires it jumps and holds
/// up a tag saying what it added; resting the pointer on one says what it does.
/// </summary>
public partial class JokerRail : Control
{
	[Export] public PackedScene CardScene { get; set; } = null!;
	/// Twice the 71x95 joker art, so every texel lands on whole pixels.
	[Export] public Vector2 CardSize { get; set; } = new(142, 190);
	/// How far apart jokers sit while there is room, as a fraction of a card's width.
	[Export] public float Pitch { get; set; } = 1.12f;

	[ExportGroup("Tags")]
	/// How long a fired joker's tag stays up before it floats off.
	[Export] public float TagHold { get; set; } = 0.55f;
	/// How far a tag drifts up while it fades.
	[Export] public float TagRise { get; set; } = 30f;
	[Export] public int TagFontSize { get; set; } = 30;

	private readonly List<Card> _cards = new();
	private readonly List<JokerCard> _jokers = new();

	private CardPath _path = null!;
	private Control _tags = null!;
	private JokerTooltip _tooltip = null!;

	private Card? _lifted;
	private int _liftedFrom;
	private int _described = -1;

	/// A joker was dropped from one slot into another.
	public event Action<int, int>? Moved;

	public override void _Ready()
	{
		_path = GetNode<CardPath>("%Path");
		_tags = GetNode<Control>("%Tags");
		_tooltip = GetNode<JokerTooltip>("%Tooltip");
		Resized += Lay;
		Lay();
	}

	public override void _Process(double delta)
	{
		TrackDrag();
		Describe();
	}

	/// The joker in a slot, as the rail shows it.
	public JokerCard Joker(int slot) => _jokers[slot];

	/// Seats these jokers in this order, in place of whatever sat here before.
	public void Fill(IEnumerable<JokerCard> jokers)
	{
		foreach (Card card in _cards)
		{
			_path.RemoveChild(card);
			card.QueueFree();
		}

		_cards.Clear();
		_jokers.Clear();
		foreach (JokerCard joker in jokers)
		{
			// Sized before it enters the tree, which is when it sizes its face, and
			// shown after, which is when it can find its art.
			var card = CardScene.Instantiate<Card>();
			card.Size = CardSize;
			_path.AddChild(card);
			card.ShowJoker(joker.Art);
			_cards.Add(card);
			_jokers.Add(joker);
		}

		_lifted = null;
		_described = -1;
		_tooltip.Hide();
		Lay();
		_path.Arrange();
		foreach (Card card in _cards)
		{
			card.SnapToHome();
		}
	}

	/// The jokers stay put while a hand plays out.
	public void Lock(bool locked)
	{
		foreach (Card card in _cards)
		{
			card.Draggable = !locked;
		}
	}

	/// The joker in this slot fires: it jumps, and a tag under it says what the
	/// beat added, one tag for each part of the beat.
	public void Fire(int slot, ScoreStep step)
	{
		Card card = _cards[slot];
		card.Pop();

		Vector2 centre = card.GlobalPosition + new Vector2(0f, CardSize.Y * 0.42f);
		if (step.AddChips != 0)
		{
			centre.Y += RaiseTag($"{step.AddChips:+0;-0}", "BluePanel", centre);
		}

		if (step.AddMult != 0)
		{
			centre.Y += RaiseTag($"{step.AddMult:+0.##;-0.##} Mult", "RedPanel", centre);
		}

		if (step.TimesMult != 1)
		{
			RaiseTag($"X{step.TimesMult:0.##} Mult", "RedPanel", centre);
		}
	}

	/// Pops one tag up centred on a point, and gives back the room it took.
	private float RaiseTag(string text, string variation, Vector2 centre)
	{
		var tag = new PanelContainer { ThemeTypeVariation = variation, MouseFilter = MouseFilterEnum.Ignore };
		var label = new Label { Text = text, HorizontalAlignment = HorizontalAlignment.Center };
		label.AddThemeFontSizeOverride("font_size", TagFontSize);
		tag.AddChild(label);
		_tags.AddChild(tag);

		Vector2 size = tag.GetCombinedMinimumSize();
		tag.Size = size;
		tag.PivotOffset = size * 0.5f;
		tag.GlobalPosition = centre - (size * 0.5f);
		tag.Scale = Vector2.One * 0.3f;

		Tween tween = tag.CreateTween();
		tween.TweenProperty(tag, "scale", Vector2.One, 0.14f)
			.SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
		tween.TweenProperty(tag, "position:y", tag.Position.Y - TagRise, 0.3f)
			.SetDelay(TagHold).SetEase(Tween.EaseType.In);
		tween.Parallel().TweenProperty(tag, "modulate:a", 0f, 0.3f).SetDelay(TagHold);
		tween.TweenCallback(Callable.From(tag.QueueFree));
		return size.Y + 8f;
	}

	/// Notices a joker being picked up and, once it is let go, which slot it
	/// was let go in.
	private void TrackDrag()
	{
		if (_lifted is null)
		{
			if (Card.Held is { } held && _cards.Contains(held))
			{
				_lifted = held;
				_liftedFrom = _cards.IndexOf(held);
			}

			return;
		}

		if (_lifted.IsDragging)
		{
			return;
		}

		Card card = _lifted;
		_lifted = null;
		int to = SlotOnPath(card);
		if (to == _liftedFrom)
		{
			return;
		}

		JokerCard joker = _jokers[_liftedFrom];
		_cards.RemoveAt(_liftedFrom);
		_cards.Insert(to, card);
		_jokers.RemoveAt(_liftedFrom);
		_jokers.Insert(to, joker);
		Moved?.Invoke(_liftedFrom, to);
	}

	private int SlotOnPath(Card card)
	{
		for (int i = 0; i < _path.Cards.Count; i++)
		{
			if (_path.Cards[i] == card)
			{
				return i;
			}
		}

		throw new InvalidOperationException("A joker left the rail while it was being moved.");
	}

	/// Describes whichever joker the pointer rests on, unless one is being carried.
	private void Describe()
	{
		int hovered = Card.Held is null ? _cards.FindIndex(card => card.IsHovered) : -1;
		if (hovered == _described)
		{
			return;
		}

		_described = hovered;
		if (hovered < 0)
		{
			_tooltip.Hide();
			return;
		}

		Card card = _cards[hovered];
		_tooltip.Describe(_jokers[hovered], new Rect2(card.GlobalPosition - (CardSize * 0.5f), CardSize));
	}

	/// A straight row through the middle of the rail, a card and a bit apart and
	/// centred while there is room, squeezing together once there is not.
	private void Lay()
	{
		float chord = Mathf.Min(CardSize.X * Pitch * _cards.Count, Size.X);
		float left = (Size.X - chord) * 0.5f;
		float middle = Size.Y * 0.5f;
		_path.Curve.ClearPoints();
		_path.Curve.AddPoint(new Vector2(left, middle));
		_path.Curve.AddPoint(new Vector2(left + chord, middle));
	}
}
