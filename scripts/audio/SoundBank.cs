using System;
using Godot;
using Godot.Collections;

namespace Overdrawn.Audio;

/// <summary>
/// Every sound the game can ask for by name.
/// </summary>
[GlobalClass]
public partial class SoundBank : Resource
{
	[Export] public Array<SoundCue> Cues { get; set; } = new();

	public SoundCue Find(StringName id)
	{
		foreach (SoundCue cue in Cues)
		{
			if (cue.Id == id)
			{
				return cue;
			}
		}

		throw new ArgumentException($"No sound cue named '{id}' in {ResourcePath}.");
	}
}
