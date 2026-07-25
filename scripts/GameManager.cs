using Countdown.scripts;
using Godot;
using Countdown.Scripts.Game;

public partial class GameManager : Node3D
{
	[Signal]
	public delegate void ScoreChangedEventHandler(int playerOneScore, int playerTwoScore);

	[Signal]
	public delegate void MatchWonEventHandler(int winnerPlayerId, int playerOneScore, int playerTwoScore);

	[Signal]
	public delegate void ScoreLimitReachedEventHandler(int winnerPlayerId, int playerOneScore, int playerTwoScore);

	[ExportCategory("Challenge")]
	[Export] private CountdownChallengeManager _countdownChallenge;
	[Export] public bool AutoStartChallenge { get; set; } = true;

	[ExportCategory("Labels")]
	[Export] public Label3D GlobalCountdownLabel { get; set; }
	[Export] public Label3D PlayerOneValueLabel { get; set; }
	[Export] public Label3D PlayerTwoValueLabel { get; set; }
	[Export] public Label3D PlayerOneScoreLabel { get; set; }
	[Export] public Label3D PlayerTwoScoreLabel { get; set; }

	[ExportCategory("Players")]
	[Export] public Player PlayerOne { get; set; }
	[Export] public Player PlayerTwo { get; set; }

	[ExportCategory("Managers")]
	[Export] public LightingManager LightingManager { get; set; }
	[Export] public SoundManager SoundManager { get; set; }

	[ExportCategory("Timeout Punishment")]
	[Export] public float CeilingLightsDelaySeconds { get; set; } = 2.0f;
	[Export] public float TimeoutStunSeconds { get; set; } = 2.5f;

	[ExportCategory("Score")]
	[Export] public int MaxScore { get; set; } = 2;
	[Export] public Color PlayerOneScoreColor { get; set; } = new Color(0.1f, 0.4f, 1.0f);
	[Export] public Color PlayerTwoScoreColor { get; set; } = new Color(1.0f, 0.15f, 0.1f);

	public CountdownChallengeManager Challenge => _countdownChallenge;
	public int PlayerOneScore { get; private set; }
	public int PlayerTwoScore { get; private set; }
	public bool IsMatchFinished { get; private set; }

	private bool _playerSignalsConnected;

	public override void _Ready()
	{
		ResolveChallengeManager();
		ConnectChallengeSignals();
		ScoreLimitReached += OnScoreLimitReached;
		CallDeferred(MethodName.BeginChallengeAfterTreeReady);
	}

	public override void _ExitTree()
	{
		DisconnectChallengeSignals();
		DisconnectPlayerSignals();
		ScoreLimitReached -= OnScoreLimitReached;
	}

	public void StartChallenge()
	{
		_countdownChallenge?.StartChallenge();
	}

	public void ResetChallenge()
	{
		_countdownChallenge?.ResetChallenge();
	}

	public void ResetScores()
	{
		PlayerOneScore = 0;
		PlayerTwoScore = 0;
		IsMatchFinished = false;
		GameStore.Instance?.SetScores(0, 0);
		GameStore.Instance?.ClearWinner();
		UpdateScoreLabels();
		EmitSignal(SignalName.ScoreChanged, PlayerOneScore, PlayerTwoScore);
	}

	private void BeginChallengeAfterTreeReady()
	{
		ResolvePlayerLabelsFromScene();
		ResolvePlayersFromScene();
		ResolveLightingManagerFromScene();
		ResolveScoreLabelsFromScene();
		WireChallengeLabels();
		ConnectPlayerSignals();
		ApplyPlayerColorsFromStore();
		GameStore.Instance?.SetScores(PlayerOneScore, PlayerTwoScore);
		UpdateScoreLabels();
		LightingManager?.DeactivateCeilingLights();

		if (_countdownChallenge == null)
		{
			GD.PushError("GameManager: CountdownChallengeManager is missing.");
			return;
		}

		_countdownChallenge.AutoStart = false;

		if (AutoStartChallenge)
		{
			_countdownChallenge.StartChallenge();
		}
		else
		{
			_countdownChallenge.ResetChallenge();
		}
	}

	private void ResolveChallengeManager()
	{
		if (_countdownChallenge != null)
		{
			return;
		}

		_countdownChallenge = GetNodeOrNull<CountdownChallengeManager>("CountdownChallengeManager");
	}

	private void ResolvePlayerLabelsFromScene()
	{
		PlayerOneValueLabel ??= GetNodeOrNull<Label3D>("../Player1/LockedTimeLabel");
		PlayerTwoValueLabel ??= GetNodeOrNull<Label3D>("../Player2/LockedTimeLabel");
		GlobalCountdownLabel ??= GetNodeOrNull<Label3D>("GlobalCountdownLabel");
	}

	private void ResolveScoreLabelsFromScene()
	{
		PlayerOneScoreLabel ??= GetNodeOrNull<Label3D>("PlayerOneScoreLabel");
		PlayerTwoScoreLabel ??= GetNodeOrNull<Label3D>("PlayerTwoScoreLabel");
	}

	private void ResolvePlayersFromScene()
	{
		PlayerOne ??= GetNodeOrNull<Player>("../Player1");
		PlayerTwo ??= GetNodeOrNull<Player>("../Player2");
	}

	private void ResolveLightingManagerFromScene()
	{
		LightingManager ??= GetNodeOrNull<LightingManager>("../LightingManager");
	}

	private void WireChallengeLabels()
	{
		if (_countdownChallenge == null)
		{
			return;
		}

		if (GlobalCountdownLabel != null)
		{
			_countdownChallenge.GlobalCountdownLabel = GlobalCountdownLabel;
		}

		if (PlayerOneValueLabel != null)
		{
			_countdownChallenge.PlayerOneValueLabel = PlayerOneValueLabel;
		}

		if (PlayerTwoValueLabel != null)
		{
			_countdownChallenge.PlayerTwoValueLabel = PlayerTwoValueLabel;
		}
	}

	private void ConnectChallengeSignals()
	{
		if (_countdownChallenge == null)
		{
			return;
		}

		_countdownChallenge.CountdownEvaluated += OnCountdownEvaluated;
		_countdownChallenge.BothPlayersTimedOut += OnBothPlayersTimedOut;
	}

	private void DisconnectChallengeSignals()
	{
		if (_countdownChallenge == null)
		{
			return;
		}

		_countdownChallenge.CountdownEvaluated -= OnCountdownEvaluated;
		_countdownChallenge.BothPlayersTimedOut -= OnBothPlayersTimedOut;
	}

	private void ConnectPlayerSignals()
	{
		if (_playerSignalsConnected)
		{
			return;
		}

		if (PlayerOne != null)
		{
			PlayerOne.PlayerDied += OnPlayerDied;
		}

		if (PlayerTwo != null)
		{
			PlayerTwo.PlayerDied += OnPlayerDied;
		}

		_playerSignalsConnected = PlayerOne != null || PlayerTwo != null;
	}

	private void DisconnectPlayerSignals()
	{
		if (!_playerSignalsConnected)
		{
			return;
		}

		if (PlayerOne != null)
		{
			PlayerOne.PlayerDied -= OnPlayerDied;
		}

		if (PlayerTwo != null)
		{
			PlayerTwo.PlayerDied -= OnPlayerDied;
		}

		_playerSignalsConnected = false;
	}

	private void OnPlayerDied(int victimPlayerId, int attackingPlayerId)
	{
		switch (victimPlayerId)
		{
			case 1: SoundManager?.PlayPlayer1DeathAudioStream(); break;
			case 2: SoundManager?.PlayPlayer2DeathAudioStream(); break;
		}
		
		if (IsMatchFinished)
		{
			return;
		}

		if (attackingPlayerId is not (1 or 2) || attackingPlayerId == victimPlayerId)
		{
			return;
		}

		AddScore(attackingPlayerId, 1);
	}

	private void AddScore(int playerId, int amount)
	{
		if (IsMatchFinished || amount <= 0)
		{
			return;
		}

		int maxScore = Mathf.Max(1, MaxScore);

		if (playerId == 1)
		{
			PlayerOneScore = Mathf.Min(maxScore, PlayerOneScore + amount);
		}
		else if (playerId == 2)
		{
			PlayerTwoScore = Mathf.Min(maxScore, PlayerTwoScore + amount);
		}
		else
		{
			return;
		}

		UpdateScoreLabels();
		EmitSignal(SignalName.ScoreChanged, PlayerOneScore, PlayerTwoScore);
		GameStore.Instance?.SetScores(PlayerOneScore, PlayerTwoScore);
		GD.Print($"Score P1={PlayerOneScore} P2={PlayerTwoScore}");

		if (PlayerOneScore >= maxScore)
		{
			FinishMatch(1);
		}
		else if (PlayerTwoScore >= maxScore)
		{
			FinishMatch(2);
		}
	}

	private void FinishMatch(int winnerPlayerId)
	{
		if (IsMatchFinished)
		{
			return;
		}

		IsMatchFinished = true;
		GameStore.Instance?.SetWinner(winnerPlayerId);
		GameStore.Instance?.SetScores(PlayerOneScore, PlayerTwoScore);
		GD.Print($"Match won by player {winnerPlayerId}. Final score P1={PlayerOneScore} P2={PlayerTwoScore}");
		EmitSignal(SignalName.MatchWon, winnerPlayerId, PlayerOneScore, PlayerTwoScore);
		EmitSignal(SignalName.ScoreLimitReached, winnerPlayerId, PlayerOneScore, PlayerTwoScore);
		CallDeferred(MethodName.GoToGameOverScene);
	}

	private void GoToGameOverScene()
	{
		GameStore.Instance?.GoToGameOver();
	}

	private void OnScoreLimitReached(int winnerPlayerId, int playerOneScore, int playerTwoScore)
	{
		GD.Print(
			$"Score limit {MaxScore} reached. Winner=Player {winnerPlayerId} ({playerOneScore}:{playerTwoScore})"
		);
	}

	private void UpdateScoreLabels()
	{
		if (PlayerOneScoreLabel != null)
		{
			PlayerOneScoreLabel.Text = PlayerOneScore.ToString();
			PlayerOneScoreLabel.Modulate = PlayerOneScoreColor;
		}

		if (PlayerTwoScoreLabel != null)
		{
			PlayerTwoScoreLabel.Text = PlayerTwoScore.ToString();
			PlayerTwoScoreLabel.Modulate = PlayerTwoScoreColor;
		}
	}

	private void ApplyPlayerColorsFromStore()
	{
		if (GameStore.Instance != null)
		{
			PlayerOneScoreColor = GameStore.Instance.PlayerOneColor;
			PlayerTwoScoreColor = GameStore.Instance.PlayerTwoColor;
		}
		else if (PlayerColorStore.Instance != null)
		{
			PlayerOneScoreColor = PlayerColorStore.Instance.PlayerOneColor;
			PlayerTwoScoreColor = PlayerColorStore.Instance.PlayerTwoColor;
		}
		else
		{
			return;
		}

		PlayerOne?.SetIndicatorColor(PlayerOneScoreColor);
		PlayerTwo?.SetIndicatorColor(PlayerTwoScoreColor);
	}

	private void OnCountdownEvaluated(
		int winnerPlayerId,
		float playerOneTime,
		float playerTwoTime,
		bool isTie
	)
	{
		if (isTie || winnerPlayerId is not (1 or 2))
		{
			GD.Print(
				$"Countdown challenge ended in a tie. P1={playerOneTime:0.00} P2={playerTwoTime:0.00}"
			);
			return;
		}

		GD.Print(
			$"Player {winnerPlayerId} won the countdown challenge. P1={playerOneTime:0.00} P2={playerTwoTime:0.00}"
		);

		ApplyLoserVulnerableDash(winnerPlayerId);
	}

	private void ApplyLoserVulnerableDash(int winnerPlayerId)
	{
		Player loser = winnerPlayerId == 1 ? PlayerTwo : PlayerOne;
		if (loser == null)
		{
			GD.PushWarning("GameManager: Loser player reference missing for vulnerable dash.");
			return;
		}

		loser.StartVulnerableDash();
	}

	private void OnBothPlayersTimedOut(float timeoutValue)
	{
		GD.Print(
			$"Both players allowed the countdown to reach {timeoutValue.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)}."
		);
		ResolveLightingManagerFromScene();
		LightingManager?.ActivateCeilingLights();
		SoundManager?.PlayLightsOnAudioStream();

		PlayerOne?.ApplyStun(TimeoutStunSeconds);
		PlayerTwo?.ApplyStun(TimeoutStunSeconds);

		ScheduleCeilingLightsDeactivation();
	}

	private async void ScheduleCeilingLightsDeactivation()
	{
		float delay = Mathf.Max(0.0f, CeilingLightsDelaySeconds);
		if (delay > 0.0f)
		{
			await ToSignal(GetTree().CreateTimer(delay), SceneTreeTimer.SignalName.Timeout);
		}

		if (!IsInstanceValid(this))
		{
			return;
		}

		ResolveLightingManagerFromScene();
		LightingManager?.DeactivateCeilingLights();
		SoundManager?.PlayLightsOffAudioStream();
	}
}
