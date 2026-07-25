using Godot;

public partial class GameManager : Node3D
{
	[Signal]
	public delegate void TimeChangedEventHandler(float timeRemaining);

	[Signal]
	public delegate void SecondTickedEventHandler(int secondsRemaining);

	[Signal]
	public delegate void PhaseReachedEventHandler(int phaseSeconds);

	[Signal]
	public delegate void RoundFinishedEventHandler();

	[ExportCategory("Global Countdown")]
	[Export] public float DurationSeconds { get; set; } = 60.0f;
	[Export] public bool AutoStart { get; set; } = true;
	[Export] public bool PauseOnFinish { get; set; } = true;

	[ExportCategory("Phases")]
	[Export] public int[] PhaseThresholds { get; set; } = { 45, 30, 15, 10 };

	[ExportCategory("Display")]
	[Export] private Label3D _countdownLabel;

	public float TimeRemaining { get; private set; }
	public bool IsRunning { get; private set; }
	public bool IsFinished { get; private set; }

	public int SecondsRemaining => Mathf.Max(0, Mathf.CeilToInt(TimeRemaining));

	private int _lastDisplayedSecond = -1;
	private readonly System.Collections.Generic.HashSet<int> _reachedPhases = new();

	public override void _Ready()
	{
		ResetCountdown(start: AutoStart);
	}

	public override void _Process(double delta)
	{
		if (!IsRunning || IsFinished)
		{
			return;
		}

		TimeRemaining = Mathf.Max(0.0f, TimeRemaining - (float)delta);
		EmitSignal(SignalName.TimeChanged, TimeRemaining);
		UpdateDisplayedSecond();
		CheckPhases();

		if (TimeRemaining <= 0.0f)
		{
			FinishRound();
		}
	}

	public void StartRound()
	{
		if (IsFinished || TimeRemaining <= 0.0f)
		{
			ResetCountdown(start: true);
			return;
		}

		IsRunning = true;
		UpdateCountdownLabel();
	}

	public void PauseRound()
	{
		IsRunning = false;
	}

	public void ResumeRound()
	{
		if (IsFinished || TimeRemaining <= 0.0f)
		{
			return;
		}

		IsRunning = true;
	}

	public void StopRound()
	{
		IsRunning = false;
		IsFinished = true;
		TimeRemaining = 0.0f;
		UpdateCountdownLabel();
	}

	public void ResetCountdown(bool start = false)
	{
		TimeRemaining = Mathf.Max(0.1f, DurationSeconds);
		IsFinished = false;
		IsRunning = start;
		_lastDisplayedSecond = -1;
		_reachedPhases.Clear();
		UpdateDisplayedSecond();
		UpdateCountdownLabel();
	}

	private void FinishRound()
	{
		TimeRemaining = 0.0f;
		IsFinished = true;
		if (PauseOnFinish)
		{
			IsRunning = false;
		}

		UpdateDisplayedSecond();
		UpdateCountdownLabel();
		EmitSignal(SignalName.RoundFinished);
	}

	private void UpdateDisplayedSecond()
	{
		int seconds = SecondsRemaining;
		if (seconds == _lastDisplayedSecond)
		{
			return;
		}

		_lastDisplayedSecond = seconds;
		UpdateCountdownLabel();
		EmitSignal(SignalName.SecondTicked, seconds);
	}

	private void CheckPhases()
	{
		if (PhaseThresholds == null)
		{
			return;
		}

		int seconds = SecondsRemaining;
		foreach (int threshold in PhaseThresholds)
		{
			if (seconds > threshold || _reachedPhases.Contains(threshold))
			{
				continue;
			}

			_reachedPhases.Add(threshold);
			EmitSignal(SignalName.PhaseReached, threshold);
		}
	}

	private void UpdateCountdownLabel()
	{
		if (_countdownLabel == null)
		{
			return;
		}

		_countdownLabel.Text = SecondsRemaining.ToString();
	}
}
