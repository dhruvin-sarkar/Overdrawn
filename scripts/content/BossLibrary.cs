using System;
using Godot;
using Godot.Collections;

namespace Overdrawn.Content;

/// <summary>
/// Every boss in the game, in Content Reference order.
/// </summary>
[GlobalClass]
public partial class BossLibrary : Resource
{
	[Export] public Array<BossCard> Bosses { get; set; } = new();

	public BossCard Find(string id)
	{
		foreach (BossCard boss in Bosses)
		{
			if (boss.Id == id)
			{
				return boss;
			}
		}

		throw new ArgumentException($"No boss with id '{id}' in {ResourcePath}.");
	}
}
