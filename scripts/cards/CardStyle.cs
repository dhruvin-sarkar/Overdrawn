using System;
using Godot;
using Godot.Collections;

namespace Overdrawn.Cards;

/// <summary>
/// One art style for a full deck: every face, the jokers and the back. High
/// contrast either swaps in dedicated art or recolours the club and diamond ink.
/// </summary>
[GlobalClass]
public partial class CardStyle : Resource
{
	public const int MaxInkPairs = 2;

	[Export] public string Id { get; set; } = "";
	[Export] public string DisplayName { get; set; } = "";

	/// Each suit lists Ace through King.
	[Export] public Array<Texture2D> Spades { get; set; } = new();
	[Export] public Array<Texture2D> Hearts { get; set; } = new();
	[Export] public Array<Texture2D> Clubs { get; set; } = new();
	[Export] public Array<Texture2D> Diamonds { get; set; } = new();
	[Export] public Array<Texture2D> Jokers { get; set; } = new();
	[Export] public Texture2D Back { get; set; } = null!;

	[ExportGroup("High Contrast")]
	/// Dedicated art, Ace through King. Leave empty to recolour the ink instead.
	[Export] public Array<Texture2D> ContrastClubs { get; set; } = new();
	[Export] public Array<Texture2D> ContrastDiamonds { get; set; } = new();
	[Export] public Array<Color> ClubInks { get; set; } = new();
	[Export] public Array<Color> ClubContrastInks { get; set; } = new();
	[Export] public Array<Color> DiamondInks { get; set; } = new();
	[Export] public Array<Color> DiamondContrastInks { get; set; } = new();
	/// Fraction of the card holding the rank and pip, recoloured on face cards
	/// so the portraits keep their colours.
	[Export] public Vector2 CornerSize { get; set; } = new(0.25f, 0.3f);
	/// Fraction of the card's width and height left alone along each edge, for
	/// styles whose outline shares a colour with the ink.
	[Export] public Vector2 EdgeInset { get; set; }

	public CardArt Art(Suit suit, Rank rank, bool highContrast)
	{
		bool contrastSuit = highContrast && suit is Suit.Clubs or Suit.Diamonds;
		if (!contrastSuit)
		{
			return new CardArt(Pick(FacesOf(suit), rank), null);
		}

		Array<Texture2D> contrastArt = suit == Suit.Clubs ? ContrastClubs : ContrastDiamonds;
		if (contrastArt.Count > 0)
		{
			return new CardArt(Pick(contrastArt, rank), null);
		}

		Array<Color> inks = suit == Suit.Clubs ? ClubInks : DiamondInks;
		Array<Color> contrastInks = suit == Suit.Clubs ? ClubContrastInks : DiamondContrastInks;
		if (inks.Count == 0 || inks.Count != contrastInks.Count || inks.Count > MaxInkPairs)
		{
			throw new InvalidOperationException($"{ResourcePath} needs 1 to {MaxInkPairs} matching {suit} ink pairs.");
		}

		bool faceCard = rank is Rank.Jack or Rank.Queen or Rank.King;
		Vector2 region = faceCard ? CornerSize : Vector2.One;
		return new CardArt(Pick(FacesOf(suit), rank), new InkSwap(inks, contrastInks, region, EdgeInset));
	}

	private Array<Texture2D> FacesOf(Suit suit) => suit switch
	{
		Suit.Spades => Spades,
		Suit.Hearts => Hearts,
		Suit.Clubs => Clubs,
		Suit.Diamonds => Diamonds,
		_ => throw new ArgumentOutOfRangeException(nameof(suit)),
	};

	private Texture2D Pick(Array<Texture2D> faces, Rank rank)
	{
		if (faces.Count != 13)
		{
			throw new InvalidOperationException($"{ResourcePath} must list 13 faces per suit, found {faces.Count}.");
		}

		return faces[(int)rank - 1];
	}
}

/// The art to draw for one card, with an optional ink recolour.
public readonly record struct CardArt(Texture2D Texture, InkSwap? Recolour);

/// Swaps each ink for its contrast colour. Region is the fraction of the card
/// covered from the top-left corner and mirrored from the bottom-right, so
/// Vector2.One means the whole card.
public readonly record struct InkSwap(Array<Color> Inks, Array<Color> ContrastInks, Vector2 Region, Vector2 EdgeInset);
