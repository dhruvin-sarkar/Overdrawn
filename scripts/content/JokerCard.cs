using Godot;
using Overdrawn.Jokers;

namespace Overdrawn.Content;

/// <summary>
/// One joker as the shop and the rail show it: its name, art, price and the
/// line that says what it does. What it actually does is the JokerRule with
/// the same id, which is plain C# so it can be tested without the engine.
/// </summary>
[GlobalClass]
public partial class JokerCard : Resource
{
	[Export] public string Id { get; set; } = "";
	[Export] public string DisplayName { get; set; } = "";
	[Export] public JokerRarity Rarity { get; set; }
	[Export] public int Cost { get; set; }
	/// The effect line, straight from the Content Reference.
	[Export(PropertyHint.MultilineText)] public string Effect { get; set; } = "";
	[Export] public Texture2D Art { get; set; } = null!;

	/// What the shop pays to take it back.
	public int SellValue => Cost / 2;

	public JokerRule CreateRule() => JokerRules.Create(Id);
}
