using System;
using System.Collections.Generic;
using Godot;
using Overdrawn.Audio;
using Overdrawn.Core;

namespace Overdrawn.UI;

/// <summary>
/// Stacks menu panels over the title screen. A panel rises from below with a
/// small overshoot; backing out drops it away and brings back the panel beneath,
/// or clears the overlay when none remain. Esc backs out as well.
/// </summary>
public partial class MenuOverlay : Control
{
	[Export] public float RiseDuration { get; set; } = 0.34f;
	[Export] public float DropDuration { get; set; } = 0.2f;
	[Export] public float DimAlpha { get; set; } = 0.45f;
	/// How far a panel travels when Reduced Motion is on.
	[Export] public float CalmDistance { get; set; } = 40f;

	private readonly Stack<Control> _holders = new();
	private readonly Dictionary<Control, Tween> _slides = new();
	private ColorRect _dim = null!;
	private Tween? _dimTween;

	public override void _Ready()
	{
		_dim = GetNode<ColorRect>("Dim");
		Visible = false;
	}

	public void Open(PackedScene scene)
	{
		Sfx.Instance.Play("panel_open");
		if (_holders.TryPeek(out Control? covered))
		{
			Slide(covered, rising: false, () => covered.Visible = false);
		}
		else
		{
			Visible = true;
			FadeDim(DimAlpha);
		}

		MenuPanel panel = scene.Instantiate<MenuPanel>();
		panel.BackPressed += Back;
		panel.OpenRequested += Open;

		// The holder is what slides, so the panel can stay centred by its anchors.
		var holder = new Control { MouseFilter = MouseFilterEnum.Ignore };
		AddChild(holder);
		holder.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		holder.AddChild(panel);
		panel.SetAnchorsAndOffsetsPreset(LayoutPreset.Center, LayoutPresetMode.Minsize);
		panel.GrowHorizontal = GrowDirection.Both;
		panel.GrowVertical = GrowDirection.Both;

		_holders.Push(holder);
		Slide(holder, rising: true, () => { });
	}

	public void Back()
	{
		Sfx.Instance.Play("panel_close");
		Control leaving = _holders.Pop();
		Slide(leaving, rising: false, () =>
		{
			_slides.Remove(leaving);
			leaving.QueueFree();
		});

		if (_holders.TryPeek(out Control? revealed))
		{
			revealed.Visible = true;
			Slide(revealed, rising: true, () => { });
		}
		else
		{
			FadeDim(0f).Finished += () => Visible = false;
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (_holders.Count > 0 && @event.IsActionPressed("ui_cancel"))
		{
			Sfx.Instance.Play("cancel");
			Back();
			GetViewport().SetInputAsHandled();
		}
	}

	private void Slide(Control holder, bool rising, Action finished)
	{
		if (_slides.TryGetValue(holder, out Tween? running))
		{
			running.Kill();
		}

		bool calm = Settings.Instance.ReducedMotion;
		float below = calm ? CalmDistance : Size.Y;
		holder.MouseBehaviorRecursive = rising ? MouseBehaviorRecursiveEnum.Inherited : MouseBehaviorRecursiveEnum.Disabled;

		Tween tween = holder.CreateTween().SetParallel();
		if (rising)
		{
			holder.Position = new Vector2(0f, below);
			holder.Modulate = calm ? Colors.Transparent : Colors.White;
			tween.TweenProperty(holder, "position:y", 0f, RiseDuration)
				.SetTrans(calm ? Tween.TransitionType.Sine : Tween.TransitionType.Back)
				.SetEase(Tween.EaseType.Out);
		}
		else
		{
			tween.TweenProperty(holder, "position:y", below, DropDuration)
				.SetTrans(calm ? Tween.TransitionType.Sine : Tween.TransitionType.Back)
				.SetEase(Tween.EaseType.In);
		}

		if (calm)
		{
			tween.TweenProperty(holder, "modulate:a", rising ? 1f : 0f, rising ? RiseDuration : DropDuration);
		}

		tween.Chain().TweenCallback(Callable.From(finished));
		_slides[holder] = tween;
	}

	private Tween FadeDim(float alpha)
	{
		_dimTween?.Kill();
		_dimTween = CreateTween();
		_dimTween.TweenProperty(_dim, "color:a", alpha, 0.15f);
		return _dimTween;
	}
}
