using Godot;

/// <summary>
/// Pulsierender Prompt-Text im Main Menu (oben mittig).
/// </summary>
public partial class MainMenuPromptLabel : Label
{
	[Export] public float PulseSpeed { get; set; } = 2.4f;
	[Export] public float PulseScaleAmount { get; set; } = 0.1f;

	private Vector2 _baseScale = Vector2.One;
	private float _time;

	public override void _Ready()
	{
		_baseScale = Scale == Vector2.Zero ? Vector2.One : Scale;
		CallDeferred(MethodName.RefreshPivot);
		Resized += OnResized;
		Visible = true;
		Modulate = Colors.White;
	}

	public override void _ExitTree()
	{
		Resized -= OnResized;
	}

	public override void _Process(double delta)
	{
		_time += (float)delta * PulseSpeed;
		float wave = (Mathf.Sin(_time) + 1.0f) * 0.5f;
		float scale = 1.0f + (wave - 0.5f) * 2.0f * PulseScaleAmount;
		Scale = _baseScale * scale;
	}

	private void OnResized()
	{
		RefreshPivot();
	}

	private void RefreshPivot()
	{
		PivotOffset = Size * 0.5f;
	}
}
