using System;
using System.Text.RegularExpressions;
using Godot;
using Overdrawn.Content;
using Overdrawn.Jokers;

namespace Overdrawn.UI;

/// <summary>
/// Says what a joker is and does, under the card the pointer rests on: its
/// name, its effect line with each number in the colour of what it feeds, and
/// its rarity.
/// </summary>
public partial class JokerTooltip : PanelContainer
{
	/// Balatro's own inks, so a number reads as Chips, Mult or money at a glance.
	public static readonly Color ChipsInk = new("009dff");
	public static readonly Color MultInk = new("fe5f55");
	public static readonly Color MoneyInk = new("f3b958");

	private static readonly Regex Mult = new(@"[+×]?\d+(?:\.\d+)? Mult");
	private static readonly Regex Chips = new(@"\+?\d+ Chips?\b");
	private static readonly Regex Money = new(@"\+?\$\d+");

	/// Space between the card's bottom edge and the tooltip, and the screen edge it keeps clear of.
	[Export] public float Gap { get; set; } = 14f;

	private Label _name = null!;
	private RichTextLabel _effect = null!;
	private Label _rarity = null!;
	private StyleBoxFlat _rarityBox = null!;

	public override void _Ready()
	{
		_name = GetNode<Label>("%Name");
		_effect = GetNode<RichTextLabel>("%Effect");
		_rarity = GetNode<Label>("%Rarity");

		// The pill wears its rarity's colour, so it gets a box of its own.
		_rarityBox = (StyleBoxFlat)_rarity.GetThemeStylebox("normal").Duplicate();
		_rarity.AddThemeStyleboxOverride("normal", _rarityBox);
		Hide();
	}

	/// Shows the joker centred under a card, both in global coordinates.
	public void Describe(JokerCard joker, Rect2 card)
	{
		_name.Text = joker.DisplayName;
		_effect.Text = $"[center]{Inked(joker.Effect)}[/center]";
		_rarity.Text = joker.Rarity.ToString();
		Color ink = RarityInk(joker.Rarity);
		_rarityBox.BgColor = ink;
		_rarityBox.BorderColor = ink.Darkened(0.35f);

		ResetSize();
		float width = GetCombinedMinimumSize().X;
		float right = GetViewportRect().Size.X - width - Gap;
		GlobalPosition = new Vector2(Mathf.Clamp(card.GetCenter().X - (width * 0.5f), Gap, right), card.End.Y + Gap);
		Show();
	}

	/// The effect line as BBCode, with Mult, Chips and dollar amounts inked.
	private static string Inked(string effect)
	{
		string text = effect.Replace("[", "[lb]");
		text = Mult.Replace(text, match => Ink(match.Value, MultInk));
		text = Chips.Replace(text, match => Ink(match.Value, ChipsInk));
		return Money.Replace(text, match => Ink(match.Value, MoneyInk));
	}

	private static string Ink(string text, Color colour) => $"[color=#{colour.ToHtml(false)}]{text}[/color]";

	private static Color RarityInk(JokerRarity rarity) => rarity switch
	{
		JokerRarity.Common => new Color("009dff"),
		JokerRarity.Uncommon => new Color("4bc292"),
		JokerRarity.Rare => new Color("fe5f55"),
		JokerRarity.Legendary => new Color("b26cbb"),
		_ => throw new ArgumentOutOfRangeException(nameof(rarity), rarity, "A joker of no rarity."),
	};
}
