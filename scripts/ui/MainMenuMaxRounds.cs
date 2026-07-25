using Godot;

/// <summary>
/// Cycles max rounds in the main menu (Y / F2) and shows the current value.
/// </summary>
public partial class MainMenuMaxRounds : Node
{
	[Export] public Label ValueLabel { get; set; }
	[Export] public string CycleAction { get; set; } = "menu_max_rounds";
	[Export] public string LabelFormat { get; set; } = "rounds: {0}";

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
		SetProcessInput(true);
		RefreshLabel();
	}

	public override void _Input(InputEvent @event)
	{
		if (@event.IsEcho())
		{
			return;
		}

		if (MainMenuHelp.Instance != null && MainMenuHelp.Instance.IsOpen)
		{
			return;
		}

		if (!InputMap.HasAction(CycleAction))
		{
			return;
		}

		if (!@event.IsActionPressed(CycleAction, allowEcho: false, exactMatch: false))
		{
			return;
		}

		if (GameStore.Instance == null)
		{
			GD.PushError("MainMenuMaxRounds: GameStore.Instance is missing.");
			return;
		}

		GameStore.Instance.CycleMaxRounds();
		RefreshLabel();
		GetViewport()?.SetInputAsHandled();
	}

	private void RefreshLabel()
	{
		if (ValueLabel == null)
		{
			return;
		}

		int rounds = GameStore.Instance?.MaxRounds ?? GameStore.DefaultMaxRounds;
		ValueLabel.Text = string.Format(LabelFormat, rounds);
	}
}
