using System;
using Godot;
using Godot.Collections;

namespace Overdrawn.Content;

/// <summary>
/// Every deck a run can start from, in the order they are offered.
/// </summary>
[GlobalClass]
public partial class StartingDeckLibrary : Resource
{
	[Export] public Array<StartingDeck> Decks { get; set; } = new();

	public StartingDeck Find(string id)
	{
		foreach (StartingDeck deck in Decks)
		{
			if (deck.Id == id)
			{
				return deck;
			}
		}

		throw new ArgumentException($"No starting deck with id '{id}' in {ResourcePath}.");
	}
}
