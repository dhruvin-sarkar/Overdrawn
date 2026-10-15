using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Overdrawn.Audio;
using Overdrawn.Blackjack;
using Overdrawn.Cards;
using Overdrawn.Content;
using Overdrawn.Core;
using Overdrawn.Run;
using Overdrawn.Scoring;
using Overdrawn.UI;

namespace Overdrawn.Table;

/// <summary>
/// The table: a rail of run statistics on the left, the felt on the right, and
/// one hand of blackjack at a time. The round owns the rules; this owns the
/// clock, pulling one step at a time so the player can watch each card land.
/// </summary>
public partial class TableScreen : Control
{
	[Export] public PackedScene CardScene { get; set; } = null!;
	[Export] public PackedScene FanScene { get; set; } = null!;
	[Export] public CardStyleLibrary CardStyles { get; set; } = null!;
	[Export] public JokerLibrary Jokers { get; set; } = null!;
	[Export] public PackedScene OptionsScene { get; set; } = null!;

	[ExportGroup("Pacing")]
	/// Between the four opening cards.
	[Export] public float DealGap { get; set; } = 0.14f;
	/// Between the dealer's draws, deliberately slower than the deal.
	[Export] public float DrawGap { get; set; } = 0.22f;
	[Export] public float DealerThinking { get; set; } = 0.3f;
	/// The gap that separates money from score.
	[Export] public float SettleHold { get; set; } = 0.35f;
	[Export] public float ReadoutHold { get; set; } = 0.2f;

	[ExportGroup("Feedback")]
	/// How fast the rail's numbers chase the run's real numbers.
	[Export] public float ReadoutRate { get; set; } = 9f;
	[Export] public float CalmCount { get; set; } = 0.35f;
	[Export] public float LoudCount { get; set; } = 1.1f;
	[Export] public float CalmTrauma { get; set; } = 0.1f;
	[Export] public float LoudTrauma { get; set; } = 0.8f;
	[Export] public float CalmPulse { get; set; } = 0.2f;
	[Export] public float LoudPulse { get; set; } = 0.9f;
	/// How many times its share of the target a hand has to score to feel loud.
	[Export] public float LoudMultiple { get; set; } = 16f;
	/// The gap between one beat of the scoring pipeline and the next.
	[Export] public float BeatGap { get; set; } = 0.18f;
	/// How far a readout box swells on its beat.
	[Export] public float BeatSwell { get; set; } = 0.18f;
	/// How much of a hand's full kick one beat of its pipeline gets, so a hand
	/// of many beats does not hit many times harder than a hand of one.
	[Export] public float BeatShare { get; set; } = 0.35f;

	private static readonly Color Gold = new(1f, 0.84f, 0.25f);

	private readonly List<HandFan> _seats = new();
	private readonly Dictionary<Move, Button> _moves = new();

	private RunState _run = null!;
	private Round? _round;
	private bool _animating;
	private float _shownScore;
	private float _shownMoney;
	private float _chipsPop;
	private float _multPop;

	/// How many of the round's hands have finished playing their beats. The
	/// rail holds back the rest of the run's score until they have.
	private int _played;

	private HandFan _dealerFan = null!;
	private JokerRail _rail = null!;
	private Label _jokerCount = null!;
	private HBoxContainer _seatRow = null!;
	private Control _betSpot = null!;
	private Control _actions = null!;
	private Label _banner = null!;
	private Label _wagerMin = null!;
	private Label _wagerMax = null!;
	private SettingSlider _wager = null!;
	private Button _deal = null!;
	private Control _shoePile = null!;
	private ShoeStack _shoeBack = null!;
	private Label _shoeCount = null!;
	private Label _chips = null!;
	private Label _mult = null!;
	private Control _chipsBox = null!;
	private Control _multBox = null!;
	private StatRow _score = null!;
	private StatRow _hands = null!;
	private Label _money = null!;
	private Label _outcome = null!;
	private Label _levelCount = null!;
	private Label _handCount = null!;
	private MenuOverlay _overlay = null!;

	public override void _Ready()
	{
		_dealerFan = GetNode<HandFan>("%DealerFan");
		_rail = GetNode<JokerRail>("%JokerRail");
		_jokerCount = GetNode<Label>("%JokerCount");
		_seatRow = GetNode<HBoxContainer>("%PlayerSeats");
		_betSpot = GetNode<Control>("%BetSpot");
		_actions = GetNode<Control>("%Actions");
		_banner = GetNode<Label>("%Banner");
		_wagerMin = GetNode<Label>("%WagerMin");
		_wagerMax = GetNode<Label>("%WagerMax");
		_wager = GetNode<SettingSlider>("%Wager");
		_deal = GetNode<Button>("%Deal");
		_shoePile = GetNode<Control>("%ShoePile");
		_shoeBack = GetNode<ShoeStack>("%ShoeBack");
		_shoeCount = GetNode<Label>("%ShoeTally");
		_chips = GetNode<Label>("%ChipsValue");
		_mult = GetNode<Label>("%MultValue");
		_chipsBox = GetNode<Control>("%Chips");
		_multBox = GetNode<Control>("%Mult");
		_chipsBox.Resized += () => _chipsBox.PivotOffset = _chipsBox.Size * 0.5f;
		_multBox.Resized += () => _multBox.PivotOffset = _multBox.Size * 0.5f;
		_score = GetNode<StatRow>("%Score");
		_hands = GetNode<StatRow>("%Hands");
		_money = GetNode<Label>("%MoneyValue");
		_outcome = GetNode<Label>("%OutcomeName");
		_levelCount = GetNode<Label>("%LevelCount");
		_handCount = GetNode<Label>("%HandCount");
		_overlay = GetNode<MenuOverlay>("%Overlay");

		_moves[Move.Hit] = GetNode<Button>("%Hit");
		_moves[Move.Stand] = GetNode<Button>("%Stand");
		_moves[Move.Double] = GetNode<Button>("%Double");
		_moves[Move.Split] = GetNode<Button>("%Split");
		foreach ((Move move, Button button) in _moves)
		{
			button.Pressed += () => OnMove(move);
		}

		_deal.Pressed += OnDeal;
		GetNode<Button>("%Options").Pressed += () => _overlay.Open(OptionsScene);
		_rail.Moved += (from, to) => _run.MoveJoker(from, to);
	}

	/// Sits the player down at this level's table. Starting a level is what
	/// shuffles the shoe, the way a dealer does.
	public void Begin(RunState run)
	{
		_run = run;
		_run.Shoe.BeginLevel(Array.Empty<PlayingCard>());

		_dealerFan.ShowStake(0);
		_shownScore = _run.LevelScore;
		_shownMoney = _run.Bankroll;

		GetNode<Label>("%TableName").Text = $"Level {_run.Level}";
		_levelCount.Text = $"{_run.Level}/{RunEconomy.LevelsPerRun}";
		GetNode<FeltMarkings>("%FeltMarkings").ShowRules(_run.Rules);
		_rail.Fill(_run.Jokers.Select(joker => Jokers.Find(joker.Id)));
		_jokerCount.Text = $"{_run.Jokers.Count}/{_run.JokerSlots}";
		GetNode<Label>("%Target").Text = $"{_run.ScoreTarget:N0}";
		GetNode<Label>("%Reward").Text = $"Clear for ${RunEconomy.ClearPayout} + ${RunEconomy.UnusedHandBonus} a hand";

		// The felt starts where the rail ends. A panel is never laid out narrower
		// than its contents, so a row that outgrows the rail silently pushes it
		// over the felt and buries the first joker rather than wrapping. Checked
		// here because a row is only as wide as the text it has been given.
		float needed = GetNode<Control>("Rail").GetCombinedMinimumSize().X;
		float edge = GetNode<Control>("Felt").OffsetLeft;
		if (needed > edge)
		{
			throw new InvalidOperationException(
				$"The rail needs {needed}px but the felt starts at {edge}px; a row in the rail is too wide.");
		}

		OpenBetting();
	}

	public override void _Process(double delta)
	{
		if (_run is null)
		{
			return;
		}

		float ease = Damp(ReadoutRate, (float)delta);
		long released = Released();
		_shownScore = Mathf.Lerp(_shownScore, released, ease);
		_shownMoney = Mathf.Lerp(_shownMoney, _run.Bankroll, ease);
		_chipsPop = Mathf.Lerp(_chipsPop, 0f, ease);
		_multPop = Mathf.Lerp(_multPop, 0f, ease);

		// A lerp stops moving once its step falls under the float's precision, so
		// the last digit is snapped home rather than left parked short of it.
		if (Mathf.Abs(released - _shownScore) < 1f)
		{
			_shownScore = released;
		}

		float swell = Settings.Instance.ReducedMotion ? 0f : BeatSwell;
		_chipsBox.Scale = Vector2.One * (1f + (_chipsPop * swell));
		_multBox.Scale = Vector2.One * (1f + (_multPop * swell));

		_score.ShowFill(_shownScore / _run.ScoreTarget, $"{Mathf.RoundToInt(_shownScore):N0}");
		_money.Text = $"${Mathf.RoundToInt(_shownMoney):N0}";
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event.IsActionPressed("ui_cancel"))
		{
			_overlay.Open(OptionsScene);
			AcceptEvent();
		}
	}

	/// Opens the betting circle for the next hand, or says why there is not one.
	private void OpenBetting()
	{
		_round = null;
		foreach (HandFan seat in _seats)
		{
			seat.QueueFree();
		}

		_seats.Clear();
		_dealerFan.Clear();
		_dealerFan.ShowTotal("");
		_chips.Text = "0";
		_mult.Text = "0";
		_outcome.Text = "";

		if (_run.LevelCleared || _run.HandsRemaining == 0)
		{
			_betSpot.Visible = false;
			_actions.Visible = false;
			_banner.Visible = true;
			_banner.Text = _run.LevelCleared ? "LEVEL CLEARED" : "OUT OF HANDS";
			Refresh();
			return;
		}

		// Debt triggers on its own the moment the bankroll cannot cover the
		// table's minimum, so the slider always has a legal range to offer.
		if (_run.CoverMinimumWager())
		{
			Sfx.Instance.Play("cancel");
		}

		(int minimum, int maximum) = new Round(_run).WagerBounds;
		_wager.SetRange(minimum, maximum, RunEconomy.WagerStep);
		_wager.SetValue(minimum);
		_wagerMin.Text = $"${minimum}";
		_wagerMax.Text = $"${maximum}";
		_betSpot.Visible = true;
		_actions.Visible = false;
		_banner.Visible = false;
		Refresh();
	}

	private async void OnDeal()
	{
		_round = new Round(_run);
		_round.Begin(_wager.Value);
		_played = 0;
		_animating = true;
		_betSpot.Visible = false;
		_actions.Visible = true;
		Refresh();

		await DealOut(DealGap);
		await Dwell(ReadoutHold);
		await Continue();
	}

	private async void OnMove(Move move)
	{
		Round round = _round!;
		int seat = round.ActiveIndex;
		round.Play(move);
		_animating = true;
		Refresh();

		switch (move)
		{
			case Move.Split:
				await Split(seat);
				break;
			case Move.Double:
				Sfx.Instance.Play("card_pick");
				ScreenShake.Instance.Kick(0.14f);
				CrtPulse.Instance.Pulse(0.25f);
				break;
			case Move.Stand:
				Sfx.Instance.Play("button");
				break;
		}

		await DealOut(DealGap);

		if (round.Hands[seat].IsBust)
		{
			Sfx.Instance.Play("cancel");
			ScreenShake.Instance.Kick(0.45f);
			CrtPulse.Instance.Pulse(0.7f);
			await Dwell(SettleHold);
		}

		await Continue();
	}

	/// Either hands back to the player or plays the dealer out and settles.
	private async Task Continue()
	{
		if (_round!.Phase == RoundPhase.PlayerTurn)
		{
			_animating = false;
			Refresh();
			return;
		}

		// The settle runs inside a finally because these handlers are async void:
		// a throw that skipped this would leave every button disabled with the
		// round still open, which reads as a freeze rather than as the fault.
		try
		{
			await PlayDealer();
			_round.Finish();
			await ShowResults();
		}
		finally
		{
			_animating = false;
			OpenBetting();
		}
	}

	/// Deals every card the round has that the felt has not, one at a time. A
	/// card is only ever in flight on its own, so none can land behind its
	/// neighbour.
	private async Task DealOut(float gap)
	{
		bool dealt = false;
		while (SyncOne())
		{
			dealt = true;
			Sfx.Instance.Play("tab");
			Refresh();
			ScreenShake.Instance.Kick(0.06f);
			CrtPulse.Instance.Pulse(0.1f);
			await Dwell(gap);
		}

		if (dealt)
		{
			Sfx.Instance.Play("card_place");
		}
	}

	/// The pair pulls apart: the second card takes a seat of its own, and only
	/// then does each half draw.
	private async Task Split(int seat)
	{
		Sfx.Instance.Play("tab");
		ScreenShake.Instance.Kick(0.2f);
		CrtPulse.Instance.Pulse(0.35f);

		HandFan from = _seats[seat];
		HandFan to = AddSeat(seat + 1);
		// The card keeps its place on screen while it changes seats, so it
		// slides across rather than flying back to the shoe.
		to.Adopt(from.Release(1));
		await Dwell(DrawGap);
		Refresh();
	}

	private async Task PlayDealer()
	{
		await Dwell(DealerThinking);

		_round!.RevealHole();

		Card hole = _dealerFan.Cards[1];
		if (hole.IsFaceDown)
		{
			Sfx.Instance.Play("card_pick");
			ScreenShake.Instance.Kick(0.22f);
			CrtPulse.Instance.Pulse(0.45f);
			await ToSignal(hole.Flip(), Tween.SignalName.Finished);
			Refresh();
		}

		int drawn = 0;
		while (_round!.DealerStep())
		{
			drawn++;
			// A long dealer run escalates on its own.
			ScreenShake.Instance.Kick(0.04f * drawn);
			await DealOut(DrawGap);
		}

		if (_round.Dealer.IsBust)
		{
			Sfx.Instance.Play("panel_close");
			ScreenShake.Instance.Kick(0.35f);
			CrtPulse.Instance.Pulse(0.55f);
		}

		await Dwell(ReadoutHold);
	}

	/// Money first and instantly, then the score beat by beat: every step of the
	/// pipeline is one readout beat, and a hand's total only lands on the rail
	/// once its last beat has played.
	private async Task ShowResults()
	{
		foreach (HandResult result in _round!.Results)
		{
			_banner.Visible = true;
			_banner.Text = Word(result.Settlement.Result);
			Sfx.Instance.Play("panel_open");
			ScreenShake.Instance.Kick(0.1f);
			CrtPulse.Instance.Pulse(0.3f);
			await Dwell(SettleHold);

			// Every hand builds its total up from nothing.
			_chips.Text = "0";
			_mult.Text = "0";

			// A bust scores no beats, so its outcome is the only caption it gets.
			if (result.Score.Steps.Count == 0)
			{
				_outcome.Text = result.Score.Outcome.Label();
			}

			foreach (ScoreStep step in result.Score.Steps)
			{
				await PlayStep(step, result.Score.Outcome);
			}

			_played++;

			if (result.Score.Score == 0)
			{
				continue;
			}

			float loud = Magnitude(result.Score.Score);
			Sfx.Instance.Play(loud > 0.6f ? "intro" : "card_place");
			ScreenShake.Instance.Kick(Mathf.Lerp(CalmTrauma, LoudTrauma, loud));
			CrtPulse.Instance.Pulse(Mathf.Lerp(CalmPulse, LoudPulse, loud));
			await Dwell(Mathf.Lerp(CalmCount, LoudCount, loud));
		}
	}

	/// One beat of the pipeline. Whichever boxes the step moved take their new
	/// values and swell, and the beat is as loud as the running total it leaves
	/// behind, so a hand grows into the impact that follows it.
	private async Task PlayStep(ScoreStep step, HandOutcome outcome)
	{
		float loud = Magnitude((long)(step.Chips * step.Mult));
		_outcome.Text = Caption(step, outcome);
		if (step.Kind == ScoreStepKind.Joker)
		{
			_rail.Fire(step.Slot, step);
		}

		if (step.AddChips != 0)
		{
			_chips.Text = $"{step.Chips}";
			_chipsPop = Mathf.Min(1f, _chipsPop + loud);
			Sfx.Instance.Play("chips");
		}

		if (step.AddMult != 0 || step.TimesMult != 1)
		{
			_mult.Text = $"{step.Mult:0.##}";
			_multPop = Mathf.Min(1f, _multPop + loud);
			Sfx.Instance.Play("mult");
		}

		ScreenShake.Instance.Kick(Mathf.Lerp(CalmTrauma, LoudTrauma, loud) * BeatShare);
		CrtPulse.Instance.Pulse(Mathf.Lerp(CalmPulse, LoudPulse, loud) * BeatShare);
		await Dwell(BeatGap);
	}

	/// What a beat is called while it plays.
	private string Caption(ScoreStep step, HandOutcome outcome) => step.Kind switch
	{
		ScoreStepKind.Base => outcome.Label(),
		ScoreStepKind.Streak => "Win Streak",
		ScoreStepKind.DealerBust => "Dealer Bust",
		ScoreStepKind.Joker => _rail.Joker(step.Slot).DisplayName,
		_ => throw new ArgumentOutOfRangeException(nameof(step), step.Kind, "Uncaptioned beat."),
	};

	/// The score the beats have let through: the run's total less every hand of
	/// this round whose beats have not played yet. The rail chases this so a
	/// hand lands on its own beat instead of the moment the round settled.
	private long Released()
	{
		long score = _run.LevelScore;
		if (_round is null)
		{
			return score;
		}

		for (int i = _played; i < _round.Results.Count; i++)
		{
			score -= _round.Results[i].Score.Score;
		}

		return score;
	}

	/// How big a hand's score felt, as a fraction: zero at nothing, one at many
	/// times what a hand has to average to clear the level. Logarithmic, because
	/// score grows multiplicatively across a run while the hand count is flat.
	private float Magnitude(long score)
	{
		float par = (float)_run.ScoreTarget / RunEconomy.HandsPerLevel;
		return Mathf.Clamp(Mathf.Log(1f + (score / par)) / Mathf.Log(1f + LoudMultiple), 0f, 1f);
	}

	/// Puts the single card the felt is most behind on, which deals the opening
	/// round the table in order: whoever is furthest behind, the player before
	/// the dealer on a tie. False once the felt matches the round.
	private bool SyncOne()
	{
		Round round = _round!;
		while (_seats.Count < round.Hands.Count)
		{
			AddSeat(_seats.Count);
		}

		HandFan? behind = null;
		Hand? hand = null;
		int worst = 0;

		for (int i = 0; i < _seats.Count; i++)
		{
			int owed = round.Hands[i].Cards.Count - _seats[i].Cards.Count;
			if (owed > worst)
			{
				(worst, behind, hand) = (owed, _seats[i], round.Hands[i]);
			}
		}

		if (round.Dealer.Cards.Count - _dealerFan.Cards.Count > worst)
		{
			(behind, hand) = (_dealerFan, round.Dealer);
		}

		if (behind is null)
		{
			return false;
		}

		DealNext(behind, hand!, behind == _dealerFan && round.HoleCardHidden);
		return true;
	}

	private void DealNext(HandFan fan, Hand hand, bool concealHole)
	{
		PlayingCard dealt = hand.Cards[fan.Cards.Count];
		bool hole = concealHole && fan.Cards.Count == 1;

		// Seat the card before telling it what to show: a Card only finds its
		// own viewport and art once it is in the tree.
		Card card = CardScene.Instantiate<Card>();
		fan.Deal(card, _shoePile.GlobalPosition + (_shoePile.Size * 0.5f));

		if (hole)
		{
			card.ShowFaceDown(BackStyle(), dealt.Suit, dealt.Rank);
		}
		else
		{
			card.ShowFace(CardStyles.Find(Settings.Instance.DeckStyle(dealt.Suit)), dealt.Suit, dealt.Rank);
		}
	}

	private HandFan AddSeat(int index)
	{
		var fan = FanScene.Instantiate<HandFan>();
		fan.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		_seatRow.AddChild(fan);
		_seatRow.MoveChild(fan, index);
		_seats.Insert(index, fan);
		return fan;
	}

	/// Pushes the round's state onto the screen. Called once after every change,
	/// never polled.
	private void Refresh()
	{
		Round? round = _round;
		foreach ((Move move, Button button) in _moves)
		{
			button.Disabled = _animating || round is null || !round.LegalMoves.Contains(move);
		}

		_deal.Disabled = _animating || round is not null || _run.LevelCleared || _run.HandsRemaining == 0;
		_rail.Lock(_animating);
		_hands.ShowValue($"{_run.HandsRemaining}/{RunEconomy.HandsPerLevel}", Colors.White);
		_handCount.Text = $"{RunEconomy.HandsPerLevel - _run.HandsRemaining}/{RunEconomy.HandsPerLevel}";
		int stock = _run.Shoe.Deck.Count + _run.Shoe.Temporary.Count;
		_shoeCount.Text = $"{_run.Shoe.Remaining}/{stock}";
		_shoeBack.ShowStock(BackStyle().Back, _run.Shoe.Remaining / (float)stock);

		if (round is null)
		{
			return;
		}

		_dealerFan.ShowTotal(round.HoleCardHidden ? $"{Showing(round.Dealer)}" : $"{round.Dealer.Total}");
		for (int i = 0; i < _seats.Count && i < round.Hands.Count; i++)
		{
			Hand hand = round.Hands[i];
			_seats[i].ShowTotal(hand.IsBust ? $"{hand.Total} BUST" : $"{hand.Total}");
			_seats[i].ShowStake(hand.Wager);
			_seats[i].SetActive(round.Phase != RoundPhase.PlayerTurn || i == round.ActiveIndex);
		}
	}

	private async Task Dwell(float seconds)
	{
		Tween tween = CreateTween();
		tween.TweenInterval(seconds);
		await ToSignal(tween, Tween.SignalName.Finished);
	}

	/// The back belongs to the shoe rather than to a card: picking it by the
	/// card's own suit would give the hole card away.
	private CardStyle BackStyle() => CardStyles.Find(Settings.Instance.DeckStyle(Suit.Spades));

	/// What the dealer is showing while the hole card is still down.
	private static int Showing(Hand dealer) => dealer.Cards[0].Rank == Rank.Ace ? 11 : dealer.Cards[0].Value;

	private static string Word(Settlement result) => result switch
	{
		Settlement.PlayerBlackjack => "BLACKJACK",
		Settlement.PlayerWin => "WIN",
		Settlement.DealerBust => "DEALER BUSTS",
		Settlement.Push => "PUSH",
		Settlement.PlayerBust => "BUST",
		Settlement.PlayerSurrender => "SURRENDERED",
		Settlement.DealerBlackjack => "DEALER BLACKJACK",
		Settlement.DealerWin => "DEALER WINS",
		_ => throw new ArgumentOutOfRangeException(nameof(result), result, "Unworded settlement."),
	};

	private static float Damp(float rate, float dt) => 1f - Mathf.Exp(-rate * dt);
}
