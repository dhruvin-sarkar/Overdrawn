using System;
using Godot;
using Godot.Collections;

namespace Overdrawn.Content;

/// <summary>
/// Every joker in the game, in Content Reference order.
/// </summary>
[GlobalClass]
public partial class JokerLibrary : Resource
{
	[Export] public Array<JokerCard> Jokers { get; set; } = new();

	public JokerCard Find(string id)
	{
		foreach (JokerCard joker in Jokers)
		{
			if (joker.Id == id)
			{
				return joker;
			}
		}

		throw new ArgumentException($"No joker with id '{id}' in {ResourcePath}.");
	}
}
