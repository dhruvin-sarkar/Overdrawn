using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;
using Overdrawn.Core;

namespace Overdrawn.Profiles;

/// <summary>
/// The three save slots. One is always the current profile; it is created as
/// "P1" on first launch so there is always someone to play as.
/// </summary>
public partial class ProfileSlots : Node
{
	public const int SlotCount = 3;
	public const int MaxNameLength = 16;
	private const string Folder = "user://profiles";

	private static readonly JsonSerializerOptions Json = new()
	{
		WriteIndented = true,
		Converters = { new JsonStringEnumConverter() },
	};

	public static ProfileSlots Instance { get; private set; } = null!;

	[Signal] public delegate void ChangedEventHandler();

	private readonly ProfileData?[] _slots = new ProfileData?[SlotCount];

	public int CurrentSlot => Settings.Instance.CurrentProfile;

	public ProfileData Current =>
		Get(CurrentSlot) ?? throw new InvalidOperationException($"Current profile slot {CurrentSlot} is empty.");

	public override void _EnterTree()
	{
		Instance = this;
		DirAccess.MakeDirRecursiveAbsolute(Folder);

		for (int slot = 1; slot <= SlotCount; slot++)
		{
			_slots[slot - 1] = Read(slot);
		}

		if (CurrentSlot is < 1 or > SlotCount)
		{
			Settings.Instance.CurrentProfile = 1;
		}

		if (Get(CurrentSlot) == null)
		{
			Create(CurrentSlot, $"P{CurrentSlot}");
		}
	}

	public ProfileData? Get(int slot) => _slots[Index(slot)];

	public void Create(int slot, string name)
	{
		if (Get(slot) != null)
		{
			throw new InvalidOperationException($"Profile slot {slot} is already in use.");
		}

		Store(slot, new ProfileData { Name = ValidName(name) });
	}

	public void Rename(int slot, string name)
	{
		ProfileData profile = Existing(slot);
		profile.Name = ValidName(name);
		Store(slot, profile);
	}

	/// Clears stats and progress but keeps the name.
	public void Reset(int slot) => Store(slot, new ProfileData { Name = Existing(slot).Name });

	public void Delete(int slot)
	{
		Existing(slot);
		if (slot == CurrentSlot)
		{
			throw new InvalidOperationException("The current profile cannot be deleted.");
		}

		DirAccess.RemoveAbsolute(PathFor(slot));
		_slots[Index(slot)] = null;
		EmitSignal(SignalName.Changed);
	}

	public void Load(int slot)
	{
		Existing(slot);
		Settings.Instance.CurrentProfile = slot;
		EmitSignal(SignalName.Changed);
	}

	public static string ValidName(string name)
	{
		string trimmed = name.Trim();
		if (trimmed.Length == 0)
		{
			throw new ArgumentException("A profile needs a name.");
		}

		return trimmed.Length > MaxNameLength ? trimmed[..MaxNameLength] : trimmed;
	}

	private ProfileData Existing(int slot) =>
		Get(slot) ?? throw new InvalidOperationException($"Profile slot {slot} is empty.");

	private void Store(int slot, ProfileData profile)
	{
		using FileAccess file = FileAccess.Open(PathFor(slot), FileAccess.ModeFlags.Write)
			?? throw new InvalidOperationException($"Could not write {PathFor(slot)}: {FileAccess.GetOpenError()}.");
		file.StoreString(JsonSerializer.Serialize(profile, Json));
		_slots[Index(slot)] = profile;
		EmitSignal(SignalName.Changed);
	}

	private static ProfileData? Read(int slot)
	{
		string path = PathFor(slot);
		if (!FileAccess.FileExists(path))
		{
			return null;
		}

		using FileAccess file = FileAccess.Open(path, FileAccess.ModeFlags.Read)
			?? throw new InvalidOperationException($"Could not read {path}: {FileAccess.GetOpenError()}.");
		return JsonSerializer.Deserialize<ProfileData>(file.GetAsText(), Json)
			?? throw new InvalidOperationException($"{path} does not contain a profile.");
	}

	private static string PathFor(int slot) => $"{Folder}/slot_{slot}.json";

	private static int Index(int slot) => slot is >= 1 and <= SlotCount
		? slot - 1
		: throw new ArgumentOutOfRangeException(nameof(slot), slot, $"Profile slots run from 1 to {SlotCount}.");
}
