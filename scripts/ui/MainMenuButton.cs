using Godot;

public partial class MainMenuButton : Button
{
	public override void _Ready()
	{
		Pressed += OnPressed;
	}

	public override void _ExitTree()
	{
		Pressed -= OnPressed;
	}

	private void OnPressed()
	{
		if (GameStore.Instance == null)
		{
			GD.PushError("MainMenuButton: GameStore.Instance is missing.");
			return;
		}

		GameStore.Instance.GoToMainMenu();
	}
}
