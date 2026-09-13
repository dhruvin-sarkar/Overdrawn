using System;
using System.Collections.Generic;
using Godot;
using Overdrawn.Cards;

namespace Overdrawn.Core;

public enum WindowModeOption
{
	Windowed,
	Borderless,
	Fullscreen,
}

public enum HitStandLayout
{
	HitThenStand,
	StandThenHit,
}

/// <summary>
/// Player settings. Loaded at startup, applied to the engine, and saved the
/// moment one changes. Anything that depends on a setting refreshes on Changed.
/// </summary>
public partial class Settings : Node
{
	public static readonly float[] GameSpeeds = { 0.5f, 1f, 2f, 4f };
	public static readonly Vector2I[] Resolutions =
	{
		new(1280, 720), new(1366, 768), new(1600, 900), new(1920, 1080), new(2560, 1440), new(3840, 2160),
	};

	public const string DefaultCardStyle = "card_set";
	private const string FilePath = "user://settings.cfg";

	public static Settings Instance { get; private set; } = null!;

	[Signal] public delegate void ChangedEventHandler();

	private float _gameSpeed = 1f;
	private HitStandLayout _hitStand = HitStandLayout.HitThenStand;
	private int _screenshake = 50;
	private bool _highContrastCards;
	private bool _reducedMotion;
	private bool _shadows = true;
	private bool _pixelArtSmoothing = true;
	private int _crt = 70;
	private bool _crtBloom = true;
	private int _masterVolume = 50;
	private int _musicVolume = 100;
	private int _gameVolume = 100;
	private int _currentProfile = 1;
	private readonly Dictionary<Suit, string> _deckStyles = new();

	public float GameSpeed
	{
		get => _gameSpeed;
		set => Commit(ref _gameSpeed, Array.IndexOf(GameSpeeds, value) >= 0
			? value
			: throw new ArgumentOutOfRangeException(nameof(value), value, "Not one of the offered game speeds."));
	}
	public HitStandLayout HitStand { get => _hitStand; set => Commit(ref _hitStand, value); }
	public int Screenshake { get => _screenshake; set => Commit(ref _screenshake, Math.Clamp(value, 0, 100)); }
	public bool HighContrastCards { get => _highContrastCards; set => Commit(ref _highContrastCards, value); }
	public bool ReducedMotion { get => _reducedMotion; set => Commit(ref _reducedMotion, value); }

	public int Monitor { get; private set; }
	public WindowModeOption WindowMode { get; private set; } = WindowModeOption.Windowed;
	public Vector2I Resolution { get; private set; } = new(1920, 1080);
	public bool VSync { get; private set; } = true;

	public bool Shadows { get => _shadows; set => Commit(ref _shadows, value); }
	public bool PixelArtSmoothing { get => _pixelArtSmoothing; set => Commit(ref _pixelArtSmoothing, value); }
	public int Crt { get => _crt; set => Commit(ref _crt, Math.Clamp(value, 0, 100)); }
	public bool CrtBloom { get => _crtBloom; set => Commit(ref _crtBloom, value); }

	public int MasterVolume { get => _masterVolume; set => Commit(ref _masterVolume, Math.Clamp(value, 0, 100)); }
	public int MusicVolume { get => _musicVolume; set => Commit(ref _musicVolume, Math.Clamp(value, 0, 100)); }
	public int GameVolume { get => _gameVolume; set => Commit(ref _gameVolume, Math.Clamp(value, 0, 100)); }

	public int CurrentProfile { get => _currentProfile; set => Commit(ref _currentProfile, value); }

	public override void _EnterTree()
	{
		Instance = this;
		Load();
	}

	public override void _Ready()
	{
		ApplyVideo();
		ApplyEngineState();
	}

	public string DeckStyle(Suit suit) => _deckStyles.GetValueOrDefault(suit, DefaultCardStyle);

	public void SetDeckStyle(Suit suit, string styleId)
	{
		_deckStyles[suit] = styleId;
		Committed();
	}

	/// Video changes wait for the Apply button, so they arrive together.
	public void SetVideo(int monitor, WindowModeOption mode, Vector2I resolution, bool vsync)
	{
		Monitor = monitor;
		WindowMode = mode;
		Resolution = resolution;
		VSync = vsync;
		ApplyVideo();
		Committed();
	}

	private void Commit<T>(ref T field, T value)
	{
		if (EqualityComparer<T>.Default.Equals(field, value))
		{
			return;
		}

		field = value;
		Committed();
	}

	private void Committed()
	{
		ApplyEngineState();
		Save();
		EmitSignal(SignalName.Changed);
	}

	private void ApplyEngineState()
	{
		SetBusVolume("Master", _masterVolume);
		SetBusVolume("Music", _musicVolume);
		SetBusVolume("Game", _gameVolume);

		RenderingServer.GlobalShaderParameterSet("pixel_smoothing", _pixelArtSmoothing ? 1f : 0f);
		RenderingServer.GlobalShaderParameterSet("crt_strength", _crt / 100f);
		RenderingServer.GlobalShaderParameterSet("crt_bloom", _crtBloom ? 1f : 0f);
		RenderingServer.GlobalShaderParameterSet("motion_scale", _reducedMotion ? 0.2f : 1f);
	}

	private static void SetBusVolume(string bus, int volume)
	{
		int index = AudioServer.GetBusIndex(bus);
		if (index < 0)
		{
			throw new InvalidOperationException($"Audio bus '{bus}' is missing from the bus layout.");
		}

		AudioServer.SetBusMute(index, volume == 0);
		AudioServer.SetBusVolumeDb(index, Mathf.LinearToDb(volume / 100f));
	}

	private void ApplyVideo()
	{
		// Inside the editor's Game tab the editor owns the window.
		if (Engine.IsEmbeddedInEditor())
		{
			return;
		}

		int screen = Math.Clamp(Monitor, 0, DisplayServer.GetScreenCount() - 1);
		DisplayServer.WindowSetCurrentScreen(screen);
		DisplayServer.WindowSetVsyncMode(VSync ? DisplayServer.VSyncMode.Enabled : DisplayServer.VSyncMode.Disabled);
		DisplayServer.WindowSetMode(WindowMode switch
		{
			WindowModeOption.Windowed => DisplayServer.WindowMode.Windowed,
			WindowModeOption.Borderless => DisplayServer.WindowMode.Fullscreen,
			WindowModeOption.Fullscreen => DisplayServer.WindowMode.ExclusiveFullscreen,
			_ => throw new ArgumentOutOfRangeException(),
		});

		if (WindowMode == WindowModeOption.Windowed)
		{
			Vector2I screenSize = DisplayServer.ScreenGetUsableRect(screen).Size;
			Vector2I size = new(Math.Min(Resolution.X, screenSize.X), Math.Min(Resolution.Y, screenSize.Y));
			DisplayServer.WindowSetSize(size);
			DisplayServer.WindowSetPosition(DisplayServer.ScreenGetUsableRect(screen).Position + (screenSize - size) / 2);
		}
	}

	private void Load()
	{
		var file = new ConfigFile();
		Error error = file.Load(FilePath);
		if (error == Error.FileNotFound)
		{
			return;
		}

		if (error != Error.Ok)
		{
			throw new InvalidOperationException($"Could not read {FilePath}: {error}.");
		}

		_gameSpeed = (float)file.GetValue("game", "speed", _gameSpeed);
		_hitStand = (HitStandLayout)(int)file.GetValue("game", "hit_stand", (int)_hitStand);
		_screenshake = (int)file.GetValue("game", "screenshake", _screenshake);
		_highContrastCards = (bool)file.GetValue("game", "high_contrast_cards", _highContrastCards);
		_reducedMotion = (bool)file.GetValue("game", "reduced_motion", _reducedMotion);

		Monitor = (int)file.GetValue("video", "monitor", Monitor);
		WindowMode = (WindowModeOption)(int)file.GetValue("video", "window_mode", (int)WindowMode);
		Resolution = (Vector2I)file.GetValue("video", "resolution", Resolution);
		VSync = (bool)file.GetValue("video", "vsync", VSync);

		_shadows = (bool)file.GetValue("graphics", "shadows", _shadows);
		_pixelArtSmoothing = (bool)file.GetValue("graphics", "pixel_art_smoothing", _pixelArtSmoothing);
		_crt = (int)file.GetValue("graphics", "crt", _crt);
		_crtBloom = (bool)file.GetValue("graphics", "crt_bloom", _crtBloom);

		_masterVolume = (int)file.GetValue("audio", "master", _masterVolume);
		_musicVolume = (int)file.GetValue("audio", "music", _musicVolume);
		_gameVolume = (int)file.GetValue("audio", "game", _gameVolume);

		_currentProfile = (int)file.GetValue("profile", "current", _currentProfile);

		foreach (Suit suit in Enum.GetValues<Suit>())
		{
			_deckStyles[suit] = (string)file.GetValue("deck", suit.ToString().ToLowerInvariant(), DefaultCardStyle);
		}
	}

	private void Save()
	{
		var file = new ConfigFile();
		file.SetValue("game", "speed", _gameSpeed);
		file.SetValue("game", "hit_stand", (int)_hitStand);
		file.SetValue("game", "screenshake", _screenshake);
		file.SetValue("game", "high_contrast_cards", _highContrastCards);
		file.SetValue("game", "reduced_motion", _reducedMotion);

		file.SetValue("video", "monitor", Monitor);
		file.SetValue("video", "window_mode", (int)WindowMode);
		file.SetValue("video", "resolution", Resolution);
		file.SetValue("video", "vsync", VSync);

		file.SetValue("graphics", "shadows", _shadows);
		file.SetValue("graphics", "pixel_art_smoothing", _pixelArtSmoothing);
		file.SetValue("graphics", "crt", _crt);
		file.SetValue("graphics", "crt_bloom", _crtBloom);

		file.SetValue("audio", "master", _masterVolume);
		file.SetValue("audio", "music", _musicVolume);
		file.SetValue("audio", "game", _gameVolume);

		file.SetValue("profile", "current", _currentProfile);

		foreach (Suit suit in Enum.GetValues<Suit>())
		{
			file.SetValue("deck", suit.ToString().ToLowerInvariant(), DeckStyle(suit));
		}

		Error error = file.Save(FilePath);
		if (error != Error.Ok)
		{
			throw new InvalidOperationException($"Could not write {FilePath}: {error}.");
		}
	}
}
