using System.Collections.Generic;
using Godot;
using Overdrawn.Profiles;

namespace Overdrawn.UI;

/// <summary>
/// Overall completion followed by one bar per progress category.
/// </summary>
public partial class ProgressSummary : VBoxContainer
{
	[Export] public PackedScene RowScene { get; set; } = null!;
	[Export] public float TitleWidth { get; set; } = 240f;
	[Export] public int FontSize { get; set; } = 28;

	private StatRow? _overall;
	private readonly List<StatRow> _rows = new();

	public void ShowProgress(ProfileProgress progress)
	{
		IReadOnlyList<ProgressRow> rows = ProgressCatalog.Rows(progress);
		if (_overall == null)
		{
			_overall = AddRow("Progress", FontSize + 8);
			foreach (ProgressRow row in rows)
			{
				_rows.Add(AddRow(row.Label, FontSize));
			}
		}

		float overall = ProgressCatalog.Overall(progress);
		_overall.ShowFill(overall, Percent(overall));
		for (int i = 0; i < rows.Count; i++)
		{
			_rows[i].ShowFill(rows[i].Fraction, $"{Percent(rows[i].Fraction)} ({rows[i].Done}/{rows[i].Total})");
		}
	}

	private StatRow AddRow(string title, int fontSize)
	{
		StatRow row = RowScene.Instantiate<StatRow>();
		row.Title = title;
		row.TitleWidth = TitleWidth;
		row.FontSize = fontSize;
		AddChild(row);
		return row;
	}

	private static string Percent(float fraction) => $"{Mathf.RoundToInt(fraction * 100f)}%";
}
