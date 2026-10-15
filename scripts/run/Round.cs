using System;
using System.Collections.Generic;
using System.Linq;
using Overdrawn.Blackjack;
using Overdrawn.Jokers;
using Overdrawn.Scoring;

namespace Overdrawn.Run;

public enum RoundPhase
{
	Betting,
	PlayerTurn,
	DealerTurn,
	Settled,
}

/// How one hand ended at the table and what it scored. A split produces one of
/// these per half.
public readonly record struct HandResult(Hand Hand, HandSettlement Settlement, ScoreBreakdown Score);

/// <summary>
/// One hand at the table, from the wager to the score. It sequences the hand
/// and nothing else: HandResolver decides what is legal and who won,
/// ScoreCalculator turns the result into Chips x Mult, and the run holds the
/// money. Each call moves the hand exactly one step the player can watch.
/// </summary>
public sealed class Round
{
	private readonly RunState _run;
	private readonly List<Hand> _hands = new();
	private readonly List<HandResult> _results = new();

	public Round(RunState run)
	{
		_run = run;
		Dealer = new Hand(0);
	}

	public RoundPhase Phase { get; private set; } = RoundPhase.Betting;
	public Hand Dealer { get; }
	public IReadOnlyList<Hand> Hands => _hands;
	public IReadOnlyList<HandResult> Results => _results;

	/// The hand the player is acting on, which a split moves through left to right.
	public int ActiveIndex { get; private set; }
	public Hand Active => _hands[ActiveIndex];

	/// The dealer's second card is dealt face down and stays that way until the
	/// dealer turns it over, which a natural on either side does not skip. A
	/// player with Peek sees it from the deal.
	public bool HoleCardHidden { get; private set; }

	public IReadOnlyList<Move> LegalMoves => Phase == RoundPhase.PlayerTurn ? Moves(Active) : Array.Empty<Move>();

	/// What this table takes, with the ceiling pulled down to what the player
	/// actually holds. The one place a wager range is decided.
	public (int Minimum, int Maximum) WagerBounds
	{
		get
		{
			(int minimum, int maximum) = _run.TableLimits;
			return (minimum, Math.Min(maximum, _run.Bankroll));
		}
	}

	/// <param name="wager">Within the table's bounds, and already covered by the bankroll.</param>
	public void Begin(int wager)
	{
		if (Phase != RoundPhase.Betting)
		{
			throw new InvalidOperationException("This hand has already been dealt.");
		}

		(int minimum, int maximum) = WagerBounds;
		if (wager < minimum || wager > maximum)
		{
			throw new ArgumentOutOfRangeException(nameof(wager), wager, $"This table takes ${minimum} to ${maximum}.");
		}

		_run.Stake(wager);
		_run.SpendHand();
		_run.Shoe.BeginHand();
		HoleCardHidden = !_run.Peek;

		var hand = new Hand(wager);
		_hands.Add(hand);

		// Round the table: the player first, the dealer's second card face down.
		hand.Deal(Draw());
		Dealer.Deal(Draw());
		hand.Deal(Draw());
		Dealer.Deal(Draw());

		// A natural on either side ends the hand before anyone acts.
		Phase = hand.IsBlackjack || Dealer.IsBlackjack ? RoundPhase.DealerTurn : RoundPhase.PlayerTurn;
	}

	public void Play(Move move)
	{
		if (Phase != RoundPhase.PlayerTurn)
		{
			throw new InvalidOperationException("It is not the player's turn.");
		}

		if (!LegalMoves.Contains(move))
		{
			throw new InvalidOperationException($"{move} is not legal on this hand.");
		}

		switch (move)
		{
			case Move.Hit:
				Active.Take(Draw());
				break;
			case Move.Stand:
				Active.Stand();
				break;
			case Move.Double:
				_run.Stake(Active.Wager);
				Active.DoubleDown();
				Active.Take(Draw());
				break;
			case Move.Split:
				Split();
				break;
			case Move.Surrender:
				Active.Surrender();
				break;
		}

		Advance();
	}

	/// Turns the hole card over, the beat the dealer's turn opens on.
	public void RevealHole()
	{
		if (Phase != RoundPhase.DealerTurn)
		{
			throw new InvalidOperationException("The dealer only shows the hole card on their own turn.");
		}

		HoleCardHidden = false;
	}

	/// Draws one card for the dealer. False once the dealer is done, so the
	/// screen can pace the draws one at a time.
	public bool DealerStep()
	{
		if (Phase != RoundPhase.DealerTurn)
		{
			throw new InvalidOperationException("The dealer is not playing.");
		}

		if (!DealerPlaysOn)
		{
			return false;
		}

		Dealer.Take(Draw());
		return true;
	}

	/// Settles every hand for money, then scores every hand, each in the order
	/// it was dealt. The rail has its say at each step, in slot order:
	/// HandResolver rules on the hand, a joker may overrule it, the money moves,
	/// the streak is recorded, the rail sees the hand, and then it scores.
	public void Finish()
	{
		if (Phase != RoundPhase.DealerTurn)
		{
			throw new InvalidOperationException("The hand is not ready to settle.");
		}

		if (DealerPlaysOn)
		{
			throw new InvalidOperationException("The dealer still has cards to draw.");
		}

		IReadOnlyList<JokerRule> jokers = _run.Jokers;
		for (int seat = 0; seat < _hands.Count; seat++)
		{
			Hand hand = _hands[seat];
			HandSettlement settlement = HandResolver.Settle(hand, Dealer, _run.Rules);
			foreach (JokerRule joker in jokers)
			{
				settlement = joker.Overrule(settlement, hand);
			}

			_run.Collect(hand.Wager + settlement.Payout);

			int cash = 0;
			foreach (JokerRule joker in jokers)
			{
				cash += joker.Payout(hand, settlement);
			}

			if (cash != 0)
			{
				_run.Collect(cash);
			}

			_run.RecordOutcome(settlement);

			var scoring = new ScoringHand(hand, settlement, seat, _run.WinStreak, _run.HandsPlayed, _run.Bankroll,
				_run.BaseChipsScale);
			foreach (JokerRule joker in jokers)
			{
				joker.OnSettled(scoring);
			}

			ScoreBreakdown score = ScoreCalculator.Score(scoring, jokers);
			_run.AddScore(score.Score);
			_results.Add(new HandResult(hand, settlement, score));

			if (jokers.Any(joker => joker.ClearsLevel(scoring)))
			{
				_run.ClearLevel();
			}
		}

		Phase = RoundPhase.Settled;
	}

	/// The dealer plays only while a hand is still in contention: a bust, a
	/// surrender and a natural have all settled themselves already.
	private bool DealerPlaysOn => !Dealer.IsBlackjack
		&& _hands.Any(hand => !hand.IsBust && !hand.Surrendered && !hand.IsBlackjack)
		&& HandResolver.DealerDraws(Dealer, _run.Rules);

	private IReadOnlyList<Move> Moves(Hand hand) =>
		HandResolver.LegalMoves(hand, Dealer, _run.Bankroll, _run.Rules, _hands.Count - 1);

	/// Splitting replaces the hand with two of its own, because neither half can
	/// be a natural any more.
	private void Split()
	{
		Hand hand = Active;
		PlayingCard moved = hand.SplitOff();
		_run.Stake(hand.Wager);

		var kept = new Hand(hand.Wager, fromSplit: true);
		kept.Deal(hand.Cards[0]);
		var dealt = new Hand(hand.Wager, fromSplit: true);
		dealt.Deal(moved);

		_hands[ActiveIndex] = kept;
		_hands.Insert(ActiveIndex + 1, dealt);

		kept.Deal(Draw());
		dealt.Deal(Draw());
	}

	/// Moves to the next hand with something left to decide, and hands over to
	/// the dealer once there is none.
	private void Advance()
	{
		while (ActiveIndex < _hands.Count - 1 && Moves(_hands[ActiveIndex]).Count == 0)
		{
			ActiveIndex++;
		}

		if (Moves(Active).Count == 0)
		{
			Phase = RoundPhase.DealerTurn;
		}
	}

	private PlayingCard Draw() => _run.Shoe.Draw();
}
