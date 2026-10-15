using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Overdrawn.Tests;

/// The content authored on disk, read straight from the .tres text so the
/// roster can be checked against the rules without the engine.
internal static class Roster
{
	/// The id of every card in one of the data folders.
	public static IEnumerable<string> IdsIn(string folder) =>
		Directory.GetFiles(Path.Combine(RepositoryRoot(), "data", folder), "*.tres")
			.Select(path => Regex.Match(File.ReadAllText(path), "^Id = \"([a-z_]+)\"$", RegexOptions.Multiline).Groups[1].Value);

	private static string RepositoryRoot([CallerFilePath] string self = "") =>
		Path.GetDirectoryName(Path.GetDirectoryName(self))!;
}
