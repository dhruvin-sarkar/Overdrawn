using System.Linq;
using Overdrawn.Cards;
using Overdrawn.Scoring;

namespace Overdrawn.Jokers;

/// +10 Chips per face card in the played hand.
public sealed class FaceValue : JokerRule
{
	public const int ChipsPerFace = 10;

	public override string Id => "face_value";

	public override JokerTrigger? OnScore(ScoringHand hand)
	{
		int faces = hand.Hand.Cards.Count(card => card.Rank is Rank.Jack or Rank.Queen or Rank.King);
		return faces == 0 ? null : JokerTrigger.AddChips(faces * ChipsPerFace);
	}
}
