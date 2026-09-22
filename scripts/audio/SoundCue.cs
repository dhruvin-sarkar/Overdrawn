using System;
using Godot;
using Godot.Collections;

namespace Overdrawn.Audio;

/// <summary>
/// One named sound. Several variants keep a repeated sound from wearing thin,
/// and every play lands somewhere in the pitch range.
/// </summary>
[GlobalClass]
public partial class SoundCue : Resource
{
	[Export] public StringName Id { get; set; } = "";
	[Export] public Array<AudioStream> Variants { get; set; } = new();
	[Export] public float VolumeDb { get; set; }
	[Export] public Vector2 PitchRange { get; set; } = new(0.98f, 1.02f);

	public AudioStream Pick() => Variants.Count > 0
		? Variants[(int)(GD.Randi() % Variants.Count)]
		: throw new InvalidOperationException($"Sound cue '{Id}' has no variants.");

	public float Pitch() => (float)GD.RandRange(PitchRange.X, PitchRange.Y);
}
