using System;
using Godot;
using Godot.Collections;

namespace Overdrawn.Cards;

/// <summary>
/// Every card art style the player can pick from, in display order.
/// </summary>
[GlobalClass]
public partial class CardStyleLibrary : Resource
{
	[Export] public Array<CardStyle> Styles { get; set; } = new();

	public CardStyle Find(string id)
	{
		foreach (CardStyle style in Styles)
		{
			if (style.Id == id)
			{
				return style;
			}
		}

		throw new ArgumentException($"No card style with id '{id}' in {ResourcePath}.");
	}
}
