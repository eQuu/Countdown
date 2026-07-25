using Godot;

public partial class GameOverReturnPrompt : MainMenuPromptLabel
{
	[Export] public string StartAction { get; set; } = "menu_start";

	public override void _Ready()
	{
		base._Ready();
		SetProcessUnhandledInput(true);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!InputMap.HasAction(StartAction))
		{
			return;
		}

		if (!@event.IsActionPressed(StartAction, allowEcho: false, exactMatch: false))
		{
			return;
		}

		GetViewport()?.SetInputAsHandled();

		if (GameStore.Instance == null)
		{
			GD.PushError("GameOverReturnPrompt: GameStore.Instance is missing.");
			return;
		}

		GameStore.Instance.GoToMainMenu();
	}
}
