using System;
using Godot;
using Overdrawn.Profiles;

namespace Overdrawn.UI;

/// <summary>
/// Browse the three save slots: name them, see their progress, create, load,
/// reset or delete them. Reset and Delete ask for a second press first.
/// </summary>
public partial class ProfilePanel : MenuPanel
{
	private const string ConfirmText = "Are you sure?";

	private LineEdit _name = null!;
	private Control _summary = null!;
	private ProgressSummary _progress = null!;
	private Control _empty = null!;
	private Control _winsRow = null!;
	private Label _wins = null!;
	private Button _load = null!;
	private Button _reset = null!;
	private Button _create = null!;
	private Button _delete = null!;

	private int _slot;
	private Button? _confirming;
	private string _confirmingText = "";

	private static ProfileSlots Slots => ProfileSlots.Instance;

	public override void _Ready()
	{
		base._Ready();
		_name = GetNode<LineEdit>("%Name");
		_summary = GetNode<Control>("%Summary");
		_progress = GetNode<ProgressSummary>("%Progress");
		_empty = GetNode<Control>("%Empty");
		_winsRow = GetNode<Control>("%WinsRow");
		_wins = GetNode<Label>("%Wins");
		_load = GetNode<Button>("%Load");
		_reset = GetNode<Button>("%Reset");
		_create = GetNode<Button>("%Create");
		_delete = GetNode<Button>("%Delete");

		_name.MaxLength = ProfileSlots.MaxNameLength;
		_name.TextSubmitted += _ => _name.ReleaseFocus();
		_name.FocusExited += Rename;

		_load.Pressed += () =>
		{
			Slots.Load(_slot);
			ShowSlot(_slot);
		};
		_create.Pressed += () =>
		{
			string name = _name.Text.Trim();
			Slots.Create(_slot, name == "" ? $"P{_slot}" : name);
			ShowSlot(_slot);
		};
		_reset.Pressed += () => Confirm(_reset, () => Slots.Reset(_slot));
		_delete.Pressed += () => Confirm(_delete, () => Slots.Delete(_slot));

		var tabs = GetNode<TabStrip>("%Tabs");
		tabs.TabSelected += index => ShowSlot(index + 1);
		tabs.Select(Slots.CurrentSlot - 1);
	}

	private void ShowSlot(int slot)
	{
		_slot = slot;
		CancelConfirm();

		ProfileData? profile = Slots.Get(slot);
		bool exists = profile != null;
		bool current = slot == Slots.CurrentSlot;

		_name.Text = profile?.Name ?? "";
		_summary.Visible = exists;
		_empty.Visible = !exists;
		_winsRow.Visible = exists;
		_load.Visible = exists;
		_reset.Visible = exists;
		_create.Visible = !exists;
		_delete.Disabled = !exists || current;

		if (profile == null)
		{
			return;
		}

		_wins.Text = profile.Wins.ToString();
		_load.Disabled = current;
		_load.Text = current ? "Current Profile" : "Load Profile";
		_progress.ShowProgress(profile.Progress);
	}

	/// An existing profile takes the typed name when the field loses focus;
	/// a blank name puts the old one back.
	private void Rename()
	{
		ProfileData? profile = Slots.Get(_slot);
		if (profile == null)
		{
			return;
		}

		string typed = _name.Text.Trim();
		if (typed == "" || typed == profile.Name)
		{
			_name.Text = profile.Name;
			return;
		}

		Slots.Rename(_slot, typed);
	}

	private void Confirm(Button button, Action action)
	{
		if (_confirming == button)
		{
			action();
			ShowSlot(_slot);
			return;
		}

		CancelConfirm();
		_confirming = button;
		_confirmingText = button.Text;
		button.Text = ConfirmText;
	}

	private void CancelConfirm()
	{
		if (_confirming != null)
		{
			_confirming.Text = _confirmingText;
			_confirming = null;
		}
	}
}
