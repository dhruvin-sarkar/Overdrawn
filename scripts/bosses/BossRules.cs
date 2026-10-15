using System;
using System.Collections.Generic;
using System.Linq;

namespace Overdrawn.Bosses;

/// <summary>
/// Every boss in the game by id, and when the house may send each one. The
/// fiercer a boss, the later it may sit down, so the first boss tables stay
/// survivable without a single joker. The Collector is never drawn, because
/// debt sends for it, and The Final Table is always the last table.
/// </summary>
public static class BossRules
{
	public const string Collector = "the_collector";
	public const string FinalTable = "the_final_table";

	private sealed record Entry(int EarliestLevel, Func<Random, BossRule> Make);

	/// In draw order, which a seeded run depends on.
	private static readonly (string Id, Entry Entry)[] Entries =
	{
		("the_vault", new(3, _ => new TheVault())),
		("the_twins", new(3, _ => new TheTwins())),
		("the_house", new(3, _ => new TheHouse())),
		("ol_sixty_five", new(3, _ => new OlSixtyFive())),
		("the_whale", new(6, _ => new TheWhale())),
		("cold_shoulder", new(6, _ => new ColdShoulder())),
		("sharp_eyes", new(9, _ => new SharpEyes())),
		("high_stakes", new(9, _ => new HighStakes())),
		("no_mercy", new(12, NoMercy.Draw)),
		(Collector, new(int.MaxValue, _ => new TheCollector())),
		(FinalTable, new(int.MaxValue, _ => new TheFinalTable())),
	};

	public static IEnumerable<string> Ids => Entries.Select(entry => entry.Id);

	/// The bosses the house may draw for a boss table at this level.
	public static IEnumerable<string> DrawnAt(int level) =>
		Entries.Where(entry => entry.Entry.EarliestLevel <= level).Select(entry => entry.Id);

	/// The bosses with one rule of their own, which No Mercy pairs up.
	public static IEnumerable<string> SingleRules =>
		Entries.Where(entry => entry.Entry.EarliestLevel != int.MaxValue && entry.Id != "no_mercy").Select(entry => entry.Id);

	public static BossRule Create(string id, Random random)
	{
		foreach ((string entryId, Entry entry) in Entries)
		{
			if (entryId == id)
			{
				return entry.Make(random);
			}
		}

		throw new ArgumentException($"No boss rule with id '{id}'.", nameof(id));
	}
}
