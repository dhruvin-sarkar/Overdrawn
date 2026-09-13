using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Overdrawn.Cards;

/// <summary>
/// Spaces its child cards evenly along the curve. A held card claims the slot
/// under where it is being dragged and the others shuffle aside with a small
/// shove, so letting go reorders the row. Ported from the card_paths reference.
/// </summary>
public partial class CardPath : Path2D
{
	[Export] public float ShoveDegrees { get; set; } = 10f;

	private readonly List<Card> _order = new();

	public IReadOnlyList<Card> Cards => _order;

	public override void _Process(double delta)
	{
		SyncChildren();
		if (_order.Count == 0)
		{
			return;
		}

		float length = Curve.GetBakedLength();
		Card? held = _order.FirstOrDefault(card => card.IsDragging);
		if (held != null)
		{
			ClaimSlotUnder(held, length);
		}

		for (int i = 0; i < _order.Count; i++)
		{
			_order[i].Position = Curve.SampleBaked(length * (i + 0.5f) / _order.Count);
			MoveChild(_order[i], i);
		}
	}

	private void ClaimSlotUnder(Card held, float length)
	{
		float along = Curve.GetClosestOffset(ToLocal(held.FaceGlobalPosition));
		int target = Mathf.Clamp(Mathf.FloorToInt(along / length * _order.Count), 0, _order.Count - 1);
		int current = _order.IndexOf(held);
		if (target == current)
		{
			return;
		}

		int direction = Mathf.Sign(target - current);
		for (int i = current + direction; i != target + direction; i += direction)
		{
			_order[i].Punch(-direction * ShoveDegrees);
		}

		_order.RemoveAt(current);
		_order.Insert(target, held);
	}

	private void SyncChildren()
	{
		_order.RemoveAll(card => !IsInstanceValid(card) || card.GetParent() != this);
		foreach (Card card in GetChildren().OfType<Card>())
		{
			if (!_order.Contains(card))
			{
				_order.Add(card);
			}
		}
	}
}
