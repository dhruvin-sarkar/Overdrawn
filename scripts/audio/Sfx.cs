using System;
using System.Collections.Generic;
using Godot;

namespace Overdrawn.Audio;

/// <summary>
/// Plays named sounds from the bank on the Game bus. A handful of voices are
/// kept ready so overlapping sounds never cut each other short.
/// </summary>
public partial class Sfx : Node
{
	private const string BankPath = "res://data/audio/sound_bank.tres";
	private const int Voices = 12;

	public static Sfx Instance { get; private set; } = null!;

	private SoundBank _bank = null!;
	private readonly List<AudioStreamPlayer> _voices = new();
	private int _oldest;

	public override void _EnterTree()
	{
		Instance = this;
		_bank = GD.Load<SoundBank>(BankPath)
			?? throw new InvalidOperationException($"Could not load the sound bank at {BankPath}.");

		for (int i = 0; i < Voices; i++)
		{
			var voice = new AudioStreamPlayer { Bus = "Game" };
			AddChild(voice);
			_voices.Add(voice);
		}
	}

	public void Play(StringName id)
	{
		SoundCue cue = _bank.Find(id);
		AudioStreamPlayer voice = FreeVoice();
		voice.Stream = cue.Pick();
		voice.VolumeDb = cue.VolumeDb;
		voice.PitchScale = cue.Pitch();
		voice.Play();
	}

	/// A quiet voice if there is one, otherwise the one that has been going longest.
	private AudioStreamPlayer FreeVoice()
	{
		foreach (AudioStreamPlayer voice in _voices)
		{
			if (!voice.Playing)
			{
				return voice;
			}
		}

		_oldest = (_oldest + 1) % _voices.Count;
		return _voices[_oldest];
	}
}
