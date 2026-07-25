using Godot;

/// <summary>
/// Panel with a chunky pixel-art style frame drawn in <see cref="_Draw"/>.
/// </summary>
public partial class PixelFramePanel : Panel
{
	[Export] public Color FillColor { get; set; } = new(0.06f, 0.07f, 0.1f, 0.92f);
	[Export] public Color OuterBorderColor { get; set; } = new(0.12f, 0.12f, 0.14f, 1.0f);
	[Export] public Color HighlightColor { get; set; } = new(0.92f, 0.93f, 0.95f, 1.0f);
	[Export] public Color MidBorderColor { get; set; } = new(0.55f, 0.58f, 0.65f, 1.0f);
	[Export] public Color InnerBorderColor { get; set; } = new(0.22f, 0.24f, 0.28f, 1.0f);
	[Export] public int PixelSize { get; set; } = 4;

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Stop;
		// Hide default panel style so only our draw shows.
		AddThemeStyleboxOverride("panel", new StyleBoxEmpty());
		QueueRedraw();
	}

	public override void _Notification(int what)
	{
		if (what == NotificationResized)
		{
			QueueRedraw();
		}
	}

	public override void _Draw()
	{
		int px = Mathf.Max(1, PixelSize);
		Rect2 rect = new(Vector2.Zero, Size);

		DrawRect(rect, FillColor, filled: true);

		// Outer dark frame
		DrawPixelBorder(rect, OuterBorderColor, px);

		// Highlight inset
		Rect2 highlight = rect.Grow(-px);
		if (highlight.Size.X > 0 && highlight.Size.Y > 0)
		{
			DrawPixelBorder(highlight, HighlightColor, px);
		}

		// Mid tone
		Rect2 mid = rect.Grow(-px * 2);
		if (mid.Size.X > 0 && mid.Size.Y > 0)
		{
			DrawPixelBorder(mid, MidBorderColor, px);
		}

		// Inner dark line
		Rect2 inner = rect.Grow(-px * 3);
		if (inner.Size.X > 0 && inner.Size.Y > 0)
		{
			DrawPixelBorder(inner, InnerBorderColor, px);
		}
	}

	private void DrawPixelBorder(Rect2 rect, Color color, int thickness)
	{
		float t = thickness;
		// Top
		DrawRect(new Rect2(rect.Position.X, rect.Position.Y, rect.Size.X, t), color, true);
		// Bottom
		DrawRect(new Rect2(rect.Position.X, rect.Position.Y + rect.Size.Y - t, rect.Size.X, t), color, true);
		// Left
		DrawRect(new Rect2(rect.Position.X, rect.Position.Y, t, rect.Size.Y), color, true);
		// Right
		DrawRect(new Rect2(rect.Position.X + rect.Size.X - t, rect.Position.Y, t, rect.Size.Y), color, true);
	}
}
