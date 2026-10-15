using System;
using System.Collections.Generic;

namespace Overdrawn.Jokers;

/// <summary>
/// Every joker rule in the game by id. A joker is a fresh instance each time,
/// because the instance is where a joker keeps whatever it counts.
/// </summary>
public static class JokerRules
{
	private static readonly Dictionary<string, Func<JokerRule>> Factories = new()
	{
		// Common
		["steady_hand"] = () => new SteadyHand(),
		["ace_high"] = () => new AceHigh(),
		["low_roller"] = () => new LowRoller(),
		["face_value"] = () => new FaceValue(),
		["small_time"] = () => new SmallTime(),
		["careful_player"] = () => new CarefulPlayer(),
		["bold_play"] = () => new BoldPlay(),
		["even_money"] = () => new EvenMoney(),
		["warm_up"] = () => new WarmUp(),
		["two_of_a_kind"] = () => new TwoOfAKind(),
		// Uncommon
		["card_counter"] = () => new CardCounter(),
		["flush_table"] = () => new FlushTable(),
		["split_decision"] = () => new SplitDecision(),
		["double_trouble"] = () => new DoubleTrouble(),
		["insurance_policy"] = () => new InsurancePolicy(),
		["hot_streak"] = () => new HotStreak(),
		["push_profiteer"] = () => new PushProfiteer(),
		["deep_pockets"] = () => new DeepPockets(),
		["quick_hands"] = () => new QuickHands(),
		["streak_shield"] = () => new StreakShield(),
		// Rare
		["bust_insurance"] = () => new BustInsurance(),
		["cold_deck"] = () => new ColdDeck(),
		["perfect_streak"] = () => new PerfectStreak(),
		["five_card_charlie_king"] = () => new FiveCardCharlieKing(),
		["vault_breaker"] = () => new VaultBreaker(),
		["whale_watcher"] = () => new WhaleWatcher(),
		["debt_deterrent"] = () => new DebtDeterrent(),
		["marked_deck"] = () => new MarkedDeck(),
		["suit_loyalty"] = () => new SuitLoyalty(),
		["overtime"] = () => new Overtime(),
		// Legendary
		["perfect_sevens"] = () => new PerfectSevens(),
		["the_house_doesnt_always_win"] = () => new TheHouseDoesntAlwaysWin(),
		["mult_engine"] = () => new MultEngine(),
		["bankroll_to_bloodline"] = () => new BankrollToBloodline(),
		["no_such_thing_as_bust"] = () => new NoSuchThingAsBust(),
		["tens_company"] = () => new TensCompany(),
		["all_in"] = () => new AllIn(),
		["house_always_loses"] = () => new HouseAlwaysLoses(),
	};

	public static IReadOnlyCollection<string> Ids => Factories.Keys;

	public static JokerRule Create(string id)
	{
		if (!Factories.TryGetValue(id, out Func<JokerRule>? make))
		{
			throw new ArgumentException($"No joker rule with id '{id}'.", nameof(id));
		}

		return make();
	}
}
