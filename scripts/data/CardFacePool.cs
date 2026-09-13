using System;
using Godot;

namespace Overdrawn.Data;

/// <summary>
/// A set of card faces to draw one from at random, such as the card shown on
/// the main menu.
/// </summary>
[GlobalClass]
public partial class CardFacePool : Resource
{
	[Export] public Godot.Collections.Array<Texture2D> Faces { get; set; } = new();

	public Texture2D PickRandom()
	{
		if (Faces.Count == 0)
		{
			throw new InvalidOperationException($"{ResourcePath} has no card faces.");
		}

		return Faces.PickRandom();
	}
}
