using Godot;

namespace Overdrawn.Content;

/// <summary>
/// One boss as the table announces it: its name, the line that says what it
/// does to the table, and the color it is announced in. What it actually does
/// is the BossRule with the same id, which the run draws for itself.
/// </summary>
[GlobalClass]
public partial class BossCard : Resource
{
	[Export] public string Id { get; set; } = "";
	[Export] public string DisplayName { get; set; } = "";
	/// The rule line, straight from the Content Reference.
	[Export(PropertyHint.MultilineText)] public string Rule { get; set; } = "";
	[Export] public Color Color { get; set; } = Colors.White;
}
