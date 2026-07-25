using System;
using System.Globalization;
using Countdown.scripts;
using Godot;

namespace Countdown.Scripts.Game;

public partial class CountdownChallengeManager : Node
{
	[Signal]
	public delegate void CountdownEvaluatedEventHandler(
		int winnerPlayerId,
		float playerOneTime,
		float playerTwoTime,
		bool isTie
	);

	[Signal]
	public delegate void BothPlayersTimedOutEventHandler(float timeoutValue);

	[Signal]
	public delegate void StateChangedEventHandler(int state);

	[ExportCategory("Countdown")]
	[Export] public float CountdownStartValue { get; set; } = 15.0f;
	[Export] public float DecimalDisplayThreshold { get; set; } = 10.0f;

	[ExportCategory("Timing")]
	[Export] public float CalculatingDuration { get; set; } = 1.0f;
	[Export] public float ResultDisplayDuration { get; set; } = 2.0f;
	[Export] public float NewCountdownDisplayDuration { get; set; } = 1.0f;

	[ExportCategory("Evaluation")]
	[Export] public float TieTolerance { get; set; } = 0.001f;

	[ExportCategory("Text")]
	[Export] public string CalculatingText { get; set; } = "Calculating...";
	[Export] public string NewCountdownText { get; set; } = "New Countdown";

	[ExportCategory("Result Colors")]
	[Export] public Color DefaultValueColor { get; set; } = Colors.White;
	[Export] public Color WinnerColor { get; set; } = Colors.Green;
	[Export] public Color LoserColor { get; set; } = Colors.Red;
	[Export] public Color TieColor { get; set; } = Colors.Orange;

	[ExportCategory("Input")]
	[Export] public string PlayerOneLockAction { get; set; } = "player_1_lock_time";
	[Export] public string PlayerTwoLockAction { get; set; } = "player_2_lock_time";
	[Export] public int PlayerOneDeviceId { get; set; } = 0;
	[Export] public int PlayerTwoDeviceId { get; set; } = 1;
	[Export] public bool AcceptKeyboardFallback { get; set; } = true;

	[ExportCategory("Labels")]
	[Export] public Label3D GlobalCountdownLabel { get; set; }
	[Export] public Label3D PlayerOneValueLabel { get; set; }
	[Export] public Label3D PlayerTwoValueLabel { get; set; }

	[ExportCategory("Lifecycle")]
	[Export] public bool AutoStart { get; set; } = true;

	public SoundManager SoundManager { get; set; }

	public CountdownChallengeState State { get; private set; } = CountdownChallengeState.Preparing;
	public float CurrentCountdown { get; private set; }

	private readonly PlayerCountdownEntry _playerOne = new() { PlayerId = 1 };
	private readonly PlayerCountdownEntry _playerTwo = new() { PlayerId = 2 };

	private float _stateTimer;
	private bool _bothTimedOut;
	private int _pendingWinnerId;
	private bool _pendingIsTie;
	private float _pendingPlayerOneTime;
	private float _pendingPlayerTwoTime;

	public override void _Ready()
	{
		SetProcessUnhandledInput(true);
		SoundManager = ((GameManager)GetParent()).SoundManager;

		if (AutoStart)
		{
			StartChallenge();
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (State != CountdownChallengeState.Running)
		{
			return;
		}

		if (@event == null || !@event.IsPressed() || @event.IsEcho())
		{
			return;
		}

		if (IsLockActionForPlayer(@event, 1))
		{
			RegisterPlayerTime(1);
			GetViewport()?.SetInputAsHandled();
			return;
		}

		if (IsLockActionForPlayer(@event, 2))
		{
			RegisterPlayerTime(2);
			GetViewport()?.SetInputAsHandled();
		}
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;

		switch (State)
		{
			case CountdownChallengeState.Running:
				ProcessRunning(dt);
				break;
			case CountdownChallengeState.Calculating:
				ProcessTimedState(dt, CalculatingDuration, OnCalculatingFinished);
				break;
			case CountdownChallengeState.ShowingResult:
				ProcessTimedState(dt, ResultDisplayDuration, EnterStartingNewCountdown);
				break;
			case CountdownChallengeState.StartingNewCountdown:
				ProcessTimedState(dt, NewCountdownDisplayDuration, StartChallenge);
				break;
		}
	}

	public void StartChallenge()
	{
		BindPlayerLabels();
		ValidateLabels();

		CurrentCountdown = CountdownStartValue;
		_stateTimer = 0.0f;
		_bothTimedOut = false;
		_pendingWinnerId = 0;
		_pendingIsTie = false;
		_pendingPlayerOneTime = 0.0f;
		_pendingPlayerTwoTime = 0.0f;

		ResetPlayerEntry(_playerOne);
		ResetPlayerEntry(_playerTwo);
		ApplyDefaultLabelColors();
		HidePlayerLabels();
		SetState(CountdownChallengeState.Running);
		UpdateGlobalCountdownLabel();
		StartCountdownSoundEffectsTimer();
	}

	public void ResetChallenge()
	{
		BindPlayerLabels();

		CurrentCountdown = CountdownStartValue;
		_stateTimer = 0.0f;
		_bothTimedOut = false;

		ResetPlayerEntry(_playerOne);
		ResetPlayerEntry(_playerTwo);
		ApplyDefaultLabelColors();
		HidePlayerLabels();
		SetState(CountdownChallengeState.Preparing);
		UpdateGlobalCountdownLabel();
	}

	public void RegisterPlayerTime(int playerId)
	{
		if (playerId is not 1 and not 2)
		{
			GD.PushWarning($"Invalid player id: {playerId}");
			return;
		}

		if (State != CountdownChallengeState.Running)
		{
			return;
		}

		PlayerCountdownEntry entry = GetEntry(playerId);
		if (entry.HasLockedTime)
		{
			return;
		}

		entry.HasLockedTime = true;
		entry.LockedManually = true;
		entry.LockedTime = Math.Max(0.0, CurrentCountdown);
		ShowPlayerLockedValue(entry);
		TryBeginEvaluationFromLocks();
	}

	public void ForceEvaluate()
	{
		if (State != CountdownChallengeState.Running)
		{
			return;
		}

		bool bothTimedOut = !_playerOne.HasLockedTime && !_playerTwo.HasLockedTime;
		AssignMissingPlayersCurrentCountdown();
		BeginCalculating(bothTimedOut);
	}

	public void SetCountdownStartValue(float value)
	{
		CountdownStartValue = Math.Max(0.1f, value);
	}

	private void ProcessRunning(float delta)
	{
		CurrentCountdown -= delta;

		if (CurrentCountdown <= 0.0f)
		{
			CurrentCountdown = 0.0f;
			UpdateGlobalCountdownLabel();
			HandleZeroReached();
			return;
		}

		UpdateGlobalCountdownLabel();
	}

	private void ProcessTimedState(float delta, float duration, Action onFinished)
	{
		_stateTimer -= delta;
		if (_stateTimer > 0.0f)
		{
			return;
		}

		onFinished();
	}

	private void HandleZeroReached()
	{
		bool bothMissed = !_playerOne.HasLockedTime && !_playerTwo.HasLockedTime;
		AssignMissingPlayersCurrentCountdown();
		BeginCalculating(bothTimedOut: bothMissed);
	}

	private void AssignMissingPlayersCurrentCountdown()
	{
		if (!_playerOne.HasLockedTime)
		{
			_playerOne.HasLockedTime = true;
			_playerOne.LockedManually = false;
			_playerOne.LockedTime = 0.0;
			ShowPlayerLockedValue(_playerOne);
		}

		if (!_playerTwo.HasLockedTime)
		{
			_playerTwo.HasLockedTime = true;
			_playerTwo.LockedManually = false;
			_playerTwo.LockedTime = 0.0;
			ShowPlayerLockedValue(_playerTwo);
		}
	}

	private void TryBeginEvaluationFromLocks()
	{
		if (_playerOne.HasLockedTime && _playerTwo.HasLockedTime)
		{
			BeginCalculating(bothTimedOut: false);
		}
	}

	private void BeginCalculating(bool bothTimedOut)
	{
		_bothTimedOut = bothTimedOut;
		PrepareEvaluationResult();
		_stateTimer = Math.Max(0.05f, CalculatingDuration);
		SetState(CountdownChallengeState.Calculating);
		SetGlobalLabelText(CalculatingText);
	}

	private void OnCalculatingFinished()
	{
		ApplyEvaluationVisuals();
		EmitEvaluationSignals();
		_stateTimer = Math.Max(0.05f, ResultDisplayDuration);
		SetState(CountdownChallengeState.ShowingResult);
	}

	private void EnterStartingNewCountdown()
	{
		_stateTimer = Math.Max(0.05f, NewCountdownDisplayDuration);
		SetState(CountdownChallengeState.StartingNewCountdown);
		SetGlobalLabelText(NewCountdownText);
	}

	private void PrepareEvaluationResult()
	{
		_pendingPlayerOneTime = (float)_playerOne.LockedTime;
		_pendingPlayerTwoTime = (float)_playerTwo.LockedTime;

		bool oneManual = _playerOne.LockedManually;
		bool twoManual = _playerTwo.LockedManually;

		if (oneManual && !twoManual)
		{
			_pendingIsTie = false;
			_pendingWinnerId = 1;
			return;
		}

		if (twoManual && !oneManual)
		{
			_pendingIsTie = false;
			_pendingWinnerId = 2;
			return;
		}

		double distanceOne = Math.Abs(_playerOne.LockedTime);
		double distanceTwo = Math.Abs(_playerTwo.LockedTime);
		_pendingIsTie = Math.Abs(distanceOne - distanceTwo) <= TieTolerance;

		if (_pendingIsTie)
		{
			_pendingWinnerId = 0;
			return;
		}

		_pendingWinnerId = distanceOne < distanceTwo ? 1 : 2;
	}

	private void ApplyEvaluationVisuals()
	{
		ApplyDefaultLabelColors();

		if (_pendingIsTie)
		{
			SetPlayerLabelColor(_playerOne, TieColor);
			SetPlayerLabelColor(_playerTwo, TieColor);
			return;
		}

		if (_pendingWinnerId == 1)
		{
			SetPlayerLabelColor(_playerOne, WinnerColor);
			SetPlayerLabelColor(_playerTwo, LoserColor);
			return;
		}

		SetPlayerLabelColor(_playerOne, LoserColor);
		SetPlayerLabelColor(_playerTwo, WinnerColor);
	}

	private void EmitEvaluationSignals()
	{
		if (_bothTimedOut)
		{
			EmitSignal(SignalName.BothPlayersTimedOut, 0.0f);
		}

		EmitSignal(
			SignalName.CountdownEvaluated,
			_pendingWinnerId,
			_pendingPlayerOneTime,
			_pendingPlayerTwoTime,
			_pendingIsTie
		);
	}

	private bool IsLockActionForPlayer(InputEvent @event, int playerId)
	{
		string action = playerId == 1 ? PlayerOneLockAction : PlayerTwoLockAction;
		if (string.IsNullOrEmpty(action) || !InputMap.HasAction(action))
		{
			return false;
		}

		if (!@event.IsActionPressed(action, exactMatch: false))
		{
			return false;
		}

		int expectedDevice = playerId == 1 ? PlayerOneDeviceId : PlayerTwoDeviceId;

		if (@event is InputEventJoypadButton or InputEventJoypadMotion)
		{
			return @event.Device == expectedDevice;
		}

		if (@event is InputEventKey)
		{
			return AcceptKeyboardFallback;
		}

		return false;
	}

	private void ShowPlayerLockedValue(PlayerCountdownEntry entry)
	{
		if (entry.ValueLabel == null)
		{
			return;
		}

		entry.ValueLabel.Text = FormatLockedValue(entry.LockedTime);
		entry.ValueLabel.Modulate = DefaultValueColor;
		entry.ValueLabel.Visible = true;
	}

	private void HidePlayerLabels()
	{
		if (_playerOne.ValueLabel != null)
		{
			_playerOne.ValueLabel.Visible = false;
			_playerOne.ValueLabel.Text = string.Empty;
		}

		if (_playerTwo.ValueLabel != null)
		{
			_playerTwo.ValueLabel.Visible = false;
			_playerTwo.ValueLabel.Text = string.Empty;
		}
	}

	private void ApplyDefaultLabelColors()
	{
		SetPlayerLabelColor(_playerOne, DefaultValueColor);
		SetPlayerLabelColor(_playerTwo, DefaultValueColor);
	}

	private static void SetPlayerLabelColor(PlayerCountdownEntry entry, Color color)
	{
		if (entry.ValueLabel == null)
		{
			return;
		}

		entry.ValueLabel.Modulate = color;
	}

	private void ResetPlayerEntry(PlayerCountdownEntry entry)
	{
		entry.HasLockedTime = false;
		entry.LockedManually = false;
		entry.LockedTime = 0.0;
	}

	private PlayerCountdownEntry GetEntry(int playerId)
	{
		return playerId == 1 ? _playerOne : _playerTwo;
	}

	private void BindPlayerLabels()
	{
		_playerOne.ValueLabel = PlayerOneValueLabel;
		_playerTwo.ValueLabel = PlayerTwoValueLabel;
	}

	private void ValidateLabels()
	{
		if (GlobalCountdownLabel == null)
		{
			GD.PushWarning("CountdownChallengeManager: GlobalCountdownLabel is not assigned.");
		}

		if (PlayerOneValueLabel == null)
		{
			GD.PushWarning("CountdownChallengeManager: PlayerOneValueLabel is not assigned.");
		}

		if (PlayerTwoValueLabel == null)
		{
			GD.PushWarning("CountdownChallengeManager: PlayerTwoValueLabel is not assigned.");
		}
	}

	private void SetState(CountdownChallengeState next)
	{
		State = next;
		EmitSignal(SignalName.StateChanged, (int)next);
	}

	private void UpdateGlobalCountdownLabel()
	{
		SetGlobalLabelText(FormatCountdown(CurrentCountdown));
	}

	private void SetGlobalLabelText(string text)
	{
		if (GlobalCountdownLabel == null)
		{
			return;
		}

		GlobalCountdownLabel.Text = text;
	}

	private string FormatCountdown(double value)
	{
		double displayValue = Math.Max(0.0, value);

		if (displayValue > DecimalDisplayThreshold)
		{
			return Math.Ceiling(displayValue).ToString("0", CultureInfo.InvariantCulture);
		}

		return displayValue.ToString("0.00", CultureInfo.InvariantCulture);
	}

	private static string FormatLockedValue(double value)
	{
		return value.ToString("0.00", CultureInfo.InvariantCulture);
	}

	private async void StartCountdownSoundEffectsTimer()
	{
		await ToSignal(GetTree().CreateTimer(CountdownStartValue - 3), "timeout");
		SoundManager?.PlayCountdownActiveAudioStream();
		await ToSignal(GetTree().CreateTimer(1), "timeout");
		SoundManager?.PlayCountdownActiveAudioStream();
		await ToSignal(GetTree().CreateTimer(1), "timeout");
		SoundManager?.PlayCountdownActiveAudioStream();
	}

	private sealed class PlayerCountdownEntry
	{
		public int PlayerId { get; init; }
		public bool HasLockedTime { get; set; }
		public bool LockedManually { get; set; }
		public double LockedTime { get; set; }
		public Label3D ValueLabel { get; set; }
	}
}
