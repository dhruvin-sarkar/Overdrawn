using System.Collections.Generic;
using Godot;

namespace Overdrawn.Cards;

/// <summary>
/// One hand of cards, fanned and overlapping, with its total and its stake
/// underneath. The dealer gets one of these; the player gets one per hand a
/// split makes, and the row of seats divides the width between them.
/// </summary>
public partial class HandFan : Control
{
	[Export] public Vector2 CardSize { get; set; } = new(142, 199);
	/// How far apart cards sit, as a fraction of a card's width.
	[Export] public float Pitch { get; set; } = 0.62f;
	/// How far the middle of the fan lifts above its ends.
	[Export] public float Arc { get; set; } = 16f;
	[Export] public float FooterHeight { get; set; } = 64f;
	[Export] public float DimmedAlpha { get; set; } = 0.55f;

	private readonly List<Card> _cards = new();

	private CardPath _path = null!;
	private Control _footer = null!;
	private Label _total = null!;
	private Control _stake = null!;
	private Label _stakeValue = null!;

	public IReadOnlyList<Card> Cards => _cards;

	public override void _Ready()
	{
		_path = GetNode<CardPath>("%Path");
		_footer = GetNode<Control>("%Footer");
		_total = GetNode<Label>("%TotalValue");
		_stake = GetNode<Control>("%Stake");
		_stakeValue = GetNode<Label>("%StakeValue");

		Resized += Lay;
		Lay();
	}

	/// Puts a card in the fan with its face still back at the shoe, so the
	/// card's own return spring flies it in.
	public void Deal(Card card, Vector2 from)
	{
		Adopt(card);
		card.SnapFaceTo(from);
	}

	/// Takes a card in wherever its face currently is, which is how a split
	/// slides one half's card across to its own seat.
	public void Adopt(Card card)
	{
		card.Size = CardSize;
		card.Draggable = false;
		_path.AddChild(card);
		_cards.Add(card);
		Lay();
	}

	/// Hands a card over to another fan.
	public Card Release(int index)
	{
		Card card = _cards[index];
		_cards.RemoveAt(index);
		_path.RemoveChild(card);
		Lay();
		return card;
	}

	public void Clear()
	{
		foreach (Card card in _cards)
		{
			card.QueueFree();
		}

		_cards.Clear();
		Lay();
	}

	public void ShowTotal(string total) => _total.Text = total;

	public void ShowStake(int wager)
	{
		_stake.Visible = wager > 0;
		_stakeValue.Text = $"${wager}";
	}

	/// The hand being acted on sits at full strength; the others wait dimmed.
	public void SetActive(bool active) => Modulate = new Color(1f, 1f, 1f, active ? 1f : DimmedAlpha);

	/// Sets the curve so the cards sit at a fixed overlap, centred in the seat,
	/// tightening only when a long hand would otherwise overflow.
	private void Lay()
	{
		int count = Mathf.Max(_path.GetChildCount(), 1);
		// A card's centre sits half a pitch inside each end, so the cards cover
		// pitch x (count - 1) plus one whole card.
		float room = Mathf.Max(Size.X - CardSize.X, 0f) / Mathf.Max(count - 1, 1);
		float pitch = Mathf.Min(CardSize.X * Pitch, room);
		float chord = pitch * count;
		float middle = (Size.Y - FooterHeight) * 0.5f;
		float left = (Size.X - chord) * 0.5f;

		_footer.Visible = _cards.Count > 0;
		_path.Curve.ClearPoints();
		_path.Curve.AddPoint(new Vector2(left, middle));
		if (Arc > 0f)
		{
			_path.Curve.AddPoint(new Vector2(Size.X * 0.5f, middle - Arc));
		}

		_path.Curve.AddPoint(new Vector2(left + chord, middle));
	}
}
