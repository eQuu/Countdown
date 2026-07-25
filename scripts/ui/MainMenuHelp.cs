using Godot;

/// <summary>
/// Toggles the main-menu help overlay (Select / F1).
/// </summary>
public partial class MainMenuHelp : Node
{
	public static MainMenuHelp Instance { get; private set; }

	[Export] public Control Overlay { get; set; }
	[Export] public Control HintLabel { get; set; }
	[Export] public string HelpAction { get; set; } = "menu_help";

	public bool IsOpen { get; private set; }

	public override void _EnterTree()
	{
		Instance = this;
	}

	public override void _ExitTree()
	{
		if (Instance == this)
		{
			Instance = null;
		}
	}

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
		SetProcessInput(true);

		if (Overlay != null)
		{
			Overlay.Visible = false;
			Overlay.MouseFilter = Control.MouseFilterEnum.Stop;
		}

		IsOpen = false;
		RefreshHintVisibility();
	}

	public override void _Input(InputEvent @event)
	{
		if (@event.IsEcho())
		{
			return;
		}

		if (!InputMap.HasAction(HelpAction))
		{
			return;
		}

		if (!@event.IsActionPressed(HelpAction, allowEcho: false, exactMatch: false))
		{
			return;
		}

		Toggle();
		GetViewport()?.SetInputAsHandled();
	}

	public void Toggle()
	{
		SetOpen(!IsOpen);
	}

	public void SetOpen(bool open)
	{
		IsOpen = open;
		if (Overlay != null)
		{
			Overlay.Visible = open;
		}

		RefreshHintVisibility();
	}

	private void RefreshHintVisibility()
	{
		if (HintLabel != null)
		{
			// Hide the hint while reading help so it does not pulse over the box.
			HintLabel.Visible = !IsOpen;
		}
	}
}
