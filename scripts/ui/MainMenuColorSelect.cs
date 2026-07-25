using Godot;

/// <summary>
/// Farbauswahl im Main Menu — gleiche Actions wie ingame:
/// MoveLeft/Right = Farbe wählen, ActionPlayer oder Lock-Taste = bestätigen / freigeben.
/// </summary>
public partial class MainMenuColorSelect : Node3D
{
	[Export] public Node3D PlayerOneRoot { get; set; }
	[Export] public Node3D PlayerTwoRoot { get; set; }
	[Export] public MeshInstance3D PlayerOneRing { get; set; }
	[Export] public MeshInstance3D PlayerTwoRing { get; set; }
	[Export] public Node3D PlayerOneColors { get; set; }
	[Export] public Node3D PlayerTwoColors { get; set; }

	[Export] public float SelectedScale { get; set; } = 1.35f;
	[Export] public float InputDeadzone { get; set; } = 0.5f;
	[Export] public float InputCooldownSeconds { get; set; } = 0.22f;
	[Export] public Label PromptLabel { get; set; }
	[Export] public string ChooseColorText { get; set; } = "choose your color";
	[Export] public string StartText { get; set; } = "Press start or space..";

	private static readonly string[] SwatchNames = ["Blue", "Red", "Yellow", "Green"];

	private readonly PlayerPicker _playerOne = new() { PlayerId = 1 };
	private readonly PlayerPicker _playerTwo = new() { PlayerId = 2 };

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
		SetProcess(true);
		SetProcessInput(true);

		CallDeferred(MethodName.Initialize);
	}

	private void Initialize()
	{
		ResolveNodes();
		SetupPicker(_playerOne, PlayerOneRoot, PlayerOneRing, PlayerOneColors, defaultIndex: 0);
		SetupPicker(_playerTwo, PlayerTwoRoot, PlayerTwoRing, PlayerTwoColors, defaultIndex: 1);
		EnsurePromptUi();
		RefreshAllVisuals();
		UpdatePromptText();
	}

	private void EnsurePromptUi()
	{
		Node menu = GetParent();
		if (menu == null)
		{
			return;
		}

		PromptLabel ??= menu.GetNodeOrNull<Label>("MenuUI/PromptLabel");
		if (PromptLabel != null)
		{
			return;
		}

		var layer = menu.GetNodeOrNull<CanvasLayer>("MenuUI");
		if (layer == null)
		{
			layer = new CanvasLayer
			{
				Name = "MenuUI",
				Layer = 100
			};
			menu.AddChild(layer);
		}

		var label = new MainMenuPromptLabel
		{
			Name = "PromptLabel",
			Text = ChooseColorText,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			AnchorLeft = 0.0f,
			AnchorTop = 0.0f,
			AnchorRight = 1.0f,
			AnchorBottom = 0.0f,
			OffsetLeft = 40.0f,
			OffsetTop = 28.0f,
			OffsetRight = -40.0f,
			OffsetBottom = 110.0f,
			GrowHorizontal = Control.GrowDirection.Both
		};

		var font = new SystemFont
		{
			FontNames = ["Georgia", "Times New Roman", "serif"]
		};
		label.AddThemeFontOverride("font", font);
		label.AddThemeFontSizeOverride("font_size", 48);
		label.AddThemeColorOverride("font_color", new Color(0.95f, 0.96f, 0.98f, 1.0f));
		label.AddThemeColorOverride("font_outline_color", new Color(0.0f, 0.0f, 0.0f, 0.9f));
		label.AddThemeConstantOverride("outline_size", 12);

		layer.AddChild(label);
		PromptLabel = label;
	}

	public override void _Input(InputEvent @event)
	{
		if (@event.IsEcho() || @event is InputEventJoypadMotion)
		{
			return;
		}

		HandleInputEvent(_playerOne, @event, OpponentBlockedIndex(_playerOne));
		HandleInputEvent(_playerTwo, @event, OpponentBlockedIndex(_playerTwo));
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;
		UpdateStickInput(_playerOne, OpponentBlockedIndex(_playerOne), dt, deviceId: 0);
		UpdateStickInput(_playerTwo, OpponentBlockedIndex(_playerTwo), dt, deviceId: 1);
	}

	private int? OpponentBlockedIndex(PlayerPicker picker)
	{
		PlayerPicker other = picker.PlayerId == 1 ? _playerTwo : _playerOne;
		return other.Confirmed ? other.ColorIndex : null;
	}

	private void ResolveNodes()
	{
		Node menu = GetParent();
		if (menu == null)
		{
			GD.PushError("MainMenuColorSelect: missing parent.");
			return;
		}

		PlayerOneRoot = menu.GetNodeOrNull<Node3D>("Player1");
		PlayerTwoRoot = menu.GetNodeOrNull<Node3D>("Player2");
		PlayerOneRing = menu.GetNodeOrNull<MeshInstance3D>("Player1/IndicatorRing");
		PlayerTwoRing = menu.GetNodeOrNull<MeshInstance3D>("Player2/IndicatorRing");
		PlayerOneColors = menu.GetNodeOrNull<Node3D>("Player1/ColorSwatches");
		PlayerTwoColors = menu.GetNodeOrNull<Node3D>("Player2/ColorSwatches");
	}

	private void SetupPicker(
		PlayerPicker picker,
		Node3D root,
		MeshInstance3D ring,
		Node3D colorsRoot,
		int defaultIndex
	)
	{
		picker.Root = root;
		picker.Ring = ring;
		picker.SwatchRoot = colorsRoot;
		picker.ColorIndex = PlayerColorStore.Instance?.GetColorIndex(picker.PlayerId) ?? defaultIndex;
		picker.InputCooldown = 0.0f;
		picker.Confirmed = false;
		picker.StickHeld = false;

		if (colorsRoot == null)
		{
			GD.PushWarning($"MainMenuColorSelect: missing ColorSwatches for player {picker.PlayerId}.");
			return;
		}

		EnsureRingMaterial(picker);
		BindSwatches(picker);
		PlayerColorStore.Instance?.SetColorIndex(picker.PlayerId, picker.ColorIndex);
		ApplyRingColor(picker);
	}

	private void EnsureRingMaterial(PlayerPicker picker)
	{
		if (picker.Ring == null)
		{
			return;
		}

		Shader shader = GD.Load<Shader>("res://resources/player/indicator_ring.gdshader");
		picker.RingMaterial = new ShaderMaterial { Shader = shader };
		picker.Ring.MaterialOverride = picker.RingMaterial;
		picker.Ring.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
	}

	private void BindSwatches(PlayerPicker picker)
	{
		picker.Swatches = new MeshInstance3D[PlayerColorStore.ColorCount];
		picker.BaseScales = new Vector3[PlayerColorStore.ColorCount];

		for (int i = 0; i < SwatchNames.Length; i++)
		{
			MeshInstance3D swatch = picker.SwatchRoot.GetNodeOrNull<MeshInstance3D>(SwatchNames[i]);
			if (swatch == null)
			{
				GD.PushWarning(
					$"MainMenuColorSelect: missing swatch '{SwatchNames[i]}' under {picker.SwatchRoot.Name}."
				);
				continue;
			}

			if (swatch.MaterialOverride is StandardMaterial3D sharedMat)
			{
				swatch.MaterialOverride = (StandardMaterial3D)sharedMat.Duplicate();
			}

			picker.Swatches[i] = swatch;
			picker.BaseScales[i] = swatch.Scale == Vector3.Zero ? Vector3.One : swatch.Scale;
		}
	}

	private void HandleInputEvent(PlayerPicker picker, InputEvent @event, int? blockedIndex)
	{
		if (picker.SwatchRoot == null)
		{
			return;
		}

		int expectedDevice = picker.PlayerId == 1 ? 0 : 1;
		if (@event is InputEventJoypadButton or InputEventJoypadMotion)
		{
			int device = @event.Device;
			if (device != expectedDevice && !(picker.PlayerId == 1 && device < 0))
			{
				return;
			}
		}

		string action = $"ActionPlayer{picker.PlayerId}";
		string lockAction = picker.PlayerId == 1 ? "player_1_lock_time" : "player_2_lock_time";
		bool pressedAction = @event.IsActionPressed(action, allowEcho: false, exactMatch: false);
		bool pressedLock = @event.IsActionPressed(lockAction, allowEcho: false, exactMatch: false);
		if (pressedAction || pressedLock)
		{
			if (picker.Confirmed)
			{
				ReleaseConfirm(picker);
			}
			else
			{
				TryConfirm(picker, blockedIndex);
			}

			GetViewport()?.SetInputAsHandled();
			return;
		}

		if (picker.Confirmed)
		{
			return;
		}

		string left = $"MoveLeftPlayer{picker.PlayerId}";
		string right = $"MoveRightPlayer{picker.PlayerId}";
		int direction = 0;
		if (@event.IsActionPressed(left, allowEcho: false, exactMatch: false))
		{
			direction = -1;
		}
		else if (@event.IsActionPressed(right, allowEcho: false, exactMatch: false))
		{
			direction = 1;
		}

		if (direction != 0)
		{
			ApplyColorStep(picker, direction, blockedIndex);
			GetViewport()?.SetInputAsHandled();
		}
	}

	private void UpdateStickInput(PlayerPicker picker, int? blockedIndex, float delta, int deviceId)
	{
		if (picker.SwatchRoot == null || picker.Confirmed)
		{
			return;
		}

		picker.InputCooldown = Mathf.Max(0.0f, picker.InputCooldown - delta);

		float axis = Input.GetJoyAxis(deviceId, JoyAxis.LeftX);
		bool outside = Mathf.Abs(axis) >= InputDeadzone;
		if (!outside)
		{
			picker.StickHeld = false;
			return;
		}

		int direction = axis > 0.0f ? 1 : -1;
		if (!picker.StickHeld)
		{
			picker.StickHeld = true;
			picker.InputCooldown = InputCooldownSeconds;
			ApplyColorStep(picker, direction, blockedIndex);
			return;
		}

		if (picker.InputCooldown <= 0.0f)
		{
			picker.InputCooldown = InputCooldownSeconds;
			ApplyColorStep(picker, direction, blockedIndex);
		}
	}

	private void ApplyColorStep(PlayerPicker picker, int direction, int? blockedIndex)
	{
		int next = CycleLocal(picker.ColorIndex, direction, blockedIndex);
		if (next == picker.ColorIndex)
		{
			return;
		}

		picker.ColorIndex = next;
		PlayerColorStore.Instance?.SetColorIndex(picker.PlayerId, next);
		ApplyRingColor(picker);
		RefreshPickerVisuals(picker, blockedIndex);
	}

	private static int CycleLocal(int current, int direction, int? blockedIndex)
	{
		int step = direction >= 0 ? 1 : -1;
		int next = current;
		for (int i = 0; i < PlayerColorStore.ColorCount; i++)
		{
			next = (next + step + PlayerColorStore.ColorCount) % PlayerColorStore.ColorCount;
			if (blockedIndex.HasValue && next == blockedIndex.Value)
			{
				continue;
			}

			return next;
		}

		return current;
	}

	private void TryConfirm(PlayerPicker picker, int? blockedIndex)
	{
		if (blockedIndex.HasValue && picker.ColorIndex == blockedIndex.Value)
		{
			return;
		}

		picker.Confirmed = true;
		PlayerColorStore.Instance?.SetColorIndex(picker.PlayerId, picker.ColorIndex);
		ApplyRingColor(picker);
		RefreshAllVisuals();
		GD.Print(
			$"Player {picker.PlayerId} confirmed color {PlayerColorStore.GetColorName(picker.ColorIndex)}"
		);
	}

	private void ReleaseConfirm(PlayerPicker picker)
	{
		picker.Confirmed = false;
		RefreshAllVisuals();
		GD.Print($"Player {picker.PlayerId} released color selection");
	}

	private void RefreshAllVisuals()
	{
		RefreshPickerVisuals(_playerOne, OpponentBlockedIndex(_playerOne));
		RefreshPickerVisuals(_playerTwo, OpponentBlockedIndex(_playerTwo));
		UpdatePromptText();
	}

	private void UpdatePromptText()
	{
		if (PromptLabel == null)
		{
			return;
		}

		bool bothReady = _playerOne.Confirmed && _playerTwo.Confirmed;
		PromptLabel.Text = bothReady ? StartText : ChooseColorText;
	}

	private void RefreshPickerVisuals(PlayerPicker picker, int? blockedIndex)
	{
		if (picker.Swatches == null)
		{
			return;
		}

		for (int i = 0; i < picker.Swatches.Length; i++)
		{
			MeshInstance3D swatch = picker.Swatches[i];
			if (swatch == null)
			{
				continue;
			}

			bool blocked = blockedIndex.HasValue && blockedIndex.Value == i;
			bool selected = i == picker.ColorIndex;
			Vector3 baseScale = picker.BaseScales[i];
			swatch.Scale = selected ? baseScale * SelectedScale : baseScale;
			swatch.Visible = !picker.Confirmed || selected;

			if (swatch.MaterialOverride is StandardMaterial3D mat)
			{
				Color baseColor = PlayerColorStore.GetColorByIndex(i);
				float alpha = blocked ? 0.25f : (selected ? 1.0f : 0.7f);
				mat.AlbedoColor = new Color(baseColor.R, baseColor.G, baseColor.B, alpha);
				mat.Emission = baseColor;
				mat.EmissionEnergyMultiplier = selected ? 3.4f : 1.6f;
			}
		}

		if (picker.SwatchRoot != null)
		{
			picker.SwatchRoot.Visible = true;
		}
	}

	private void ApplyRingColor(PlayerPicker picker)
	{
		if (picker.RingMaterial == null)
		{
			return;
		}

		Color color = PlayerColorStore.GetColorByIndex(picker.ColorIndex);
		picker.RingMaterial.SetShaderParameter("ring_color", new Color(color.R, color.G, color.B, 0.95f));
	}

	private sealed class PlayerPicker
	{
		public int PlayerId { get; init; }
		public Node3D Root { get; set; }
		public MeshInstance3D Ring { get; set; }
		public ShaderMaterial RingMaterial { get; set; }
		public Node3D SwatchRoot { get; set; }
		public MeshInstance3D[] Swatches { get; set; }
		public Vector3[] BaseScales { get; set; }
		public int ColorIndex { get; set; }
		public bool Confirmed { get; set; }
		public float InputCooldown { get; set; }
		public bool StickHeld { get; set; }
	}
}
