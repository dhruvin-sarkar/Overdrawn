using Godot;

namespace Overdrawn.UI;

/// <summary>
/// A light title pill beside a dark value box. The value is either coloured
/// text or a completion bar with text over it. Long titles shrink to fit the
/// pill, so every value box in a column lines up.
/// </summary>
public partial class StatRow : HBoxContainer
{
	[Export] public string Title { get; set; } = "";
	[Export] public float TitleWidth { get; set; } = 260f;
	[Export] public int FontSize { get; set; } = 32;
	[Export] public int MinFontSize { get; set; } = 16;

	private Label _value = null!;
	private FillBar _fill = null!;

	public override void _Ready()
	{
		var pill = GetNode<PanelContainer>("%TitlePill");
		pill.CustomMinimumSize = new Vector2(TitleWidth, 0f);

		Label title = GetNode<Label>("%Title");
		title.Text = Title;
		float room = TitleWidth - pill.GetThemeStylebox("panel").GetMinimumSize().X;
		Font font = title.GetThemeFont("font");
		int size = FontSize;
		while (size > MinFontSize && font.GetStringSize(Title, HorizontalAlignment.Left, -1, size).X > room)
		{
			size--;
		}

		title.AddThemeFontSizeOverride("font_size", size);

		_value = GetNode<Label>("%Value");
		_value.AddThemeFontSizeOverride("font_size", FontSize);
		_fill = GetNode<FillBar>("%Fill");
	}

	public void ShowValue(string text, Color colour)
	{
		_value.Text = text;
		_value.AddThemeColorOverride("font_color", colour);
		_fill.Fraction = 0f;
	}

	public void ShowFill(float fraction, string text)
	{
		_value.Text = text;
		_value.RemoveThemeColorOverride("font_color");
		_fill.Fraction = fraction;
	}
}
