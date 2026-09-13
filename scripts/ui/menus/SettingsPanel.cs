using System;
using System.Globalization;
using System.Linq;
using Godot;
using Overdrawn.Core;

namespace Overdrawn.UI;

/// <summary>
/// Game, Video, Graphics and Audio settings. Everything applies the moment it
/// changes except the video group, which waits for Apply.
/// </summary>
public partial class SettingsPanel : MenuPanel
{
	private static readonly string[] OffOn = { "Off", "On" };

	private OptionCycler _monitor = null!;
	private OptionCycler _windowMode = null!;
	private OptionCycler _resolution = null!;
	private OptionCycler _vsync = null!;
	private Button _apply = null!;

	public override void _Ready()
	{
		base._Ready();
		Settings settings = Settings.Instance;

		Control pages = GetNode<Control>("%Pages");
		var tabs = GetNode<TabStrip>("%Tabs");
		tabs.TabSelected += index =>
		{
			for (int i = 0; i < pages.GetChildCount(); i++)
			{
				pages.GetChild<Control>(i).Visible = i == index;
			}
		};
		tabs.Select(0);

		BindCycler("%GameSpeed", Settings.GameSpeeds.Select(speed => speed.ToString(CultureInfo.InvariantCulture)).ToArray(),
			Array.IndexOf(Settings.GameSpeeds, settings.GameSpeed), index => settings.GameSpeed = Settings.GameSpeeds[index]);
		BindCycler("%HitStand", new[] { "Hit/Stand", "Stand/Hit" },
			(int)settings.HitStand, index => settings.HitStand = (HitStandLayout)index);
		BindSlider("%Screenshake", settings.Screenshake, value => settings.Screenshake = value);
		BindToggle("%HighContrast", settings.HighContrastCards, on => settings.HighContrastCards = on);
		BindToggle("%ReducedMotion", settings.ReducedMotion, on => settings.ReducedMotion = on);

		BindCycler("%Shadows", OffOn, settings.Shadows ? 1 : 0, index => settings.Shadows = index == 1);
		BindCycler("%PixelSmoothing", OffOn, settings.PixelArtSmoothing ? 1 : 0, index => settings.PixelArtSmoothing = index == 1);
		BindSlider("%Crt", settings.Crt, value => settings.Crt = value);
		BindCycler("%CrtBloom", OffOn, settings.CrtBloom ? 1 : 0, index => settings.CrtBloom = index == 1);

		BindSlider("%MasterVolume", settings.MasterVolume, value => settings.MasterVolume = value);
		BindSlider("%MusicVolume", settings.MusicVolume, value => settings.MusicVolume = value);
		BindSlider("%GameVolume", settings.GameVolume, value => settings.GameVolume = value);

		SetUpVideo(settings);
	}

	private void SetUpVideo(Settings settings)
	{
		_monitor = GetNode<OptionCycler>("%Monitor");
		_windowMode = GetNode<OptionCycler>("%WindowMode");
		_resolution = GetNode<OptionCycler>("%Resolution");
		_vsync = GetNode<OptionCycler>("%VSync");
		_apply = GetNode<Button>("%Apply");

		int screens = DisplayServer.GetScreenCount();
		_monitor.Setup(Enumerable.Range(1, screens).Select(number => number.ToString()).ToArray(),
			Math.Clamp(settings.Monitor, 0, screens - 1));
		_windowMode.Setup(Enum.GetNames<WindowModeOption>(), (int)settings.WindowMode);
		_resolution.Setup(Settings.Resolutions.Select(size => $"{size.X} X {size.Y}").ToArray(),
			Array.IndexOf(Settings.Resolutions, settings.Resolution));
		_vsync.Setup(new[] { "VSync Off", "VSync On" }, settings.VSync ? 1 : 0);

		foreach (OptionCycler cycler in new[] { _monitor, _windowMode, _resolution, _vsync })
		{
			cycler.IndexChanged += _ => RefreshVideo();
		}

		_apply.Pressed += () =>
		{
			settings.SetVideo(_monitor.Index, (WindowModeOption)_windowMode.Index,
				Settings.Resolutions[_resolution.Index], _vsync.Index == 1);
			RefreshVideo();
		};

		RefreshVideo();
	}

	/// Resolution only means something in a window, and Apply only lights up
	/// when the choices differ from what is running.
	private void RefreshVideo()
	{
		Settings settings = Settings.Instance;
		_resolution.SetLocked(_windowMode.Index != (int)WindowModeOption.Windowed);
		_apply.Disabled = _monitor.Index == settings.Monitor
			&& _windowMode.Index == (int)settings.WindowMode
			&& Settings.Resolutions[_resolution.Index] == settings.Resolution
			&& _vsync.Index == 1 == settings.VSync;
	}

	private void BindCycler(string path, string[] options, int index, Action<int> apply)
	{
		var cycler = GetNode<OptionCycler>(path);
		cycler.Setup(options, index);
		cycler.IndexChanged += picked => apply(picked);
	}

	private void BindSlider(string path, int value, Action<int> apply)
	{
		var slider = GetNode<SettingSlider>(path);
		slider.SetValue(value);
		slider.ValueChanged += picked => apply(picked);
	}

	private void BindToggle(string path, bool on, Action<bool> apply)
	{
		var toggle = GetNode<SettingToggle>(path);
		toggle.SetOn(on);
		toggle.Switched += picked => apply(picked);
	}
}
