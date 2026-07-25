using Countdown.scripts;
using Godot;
using Countdown.Scripts.Game;

public partial class GameManager : Node3D
{
	[ExportCategory("Challenge")]
	[Export] private CountdownChallengeManager _countdownChallenge;
	[Export] public bool AutoStartChallenge { get; set; } = true;

	[ExportCategory("Labels")]
	[Export] public Label3D GlobalCountdownLabel { get; set; }
	[Export] public Label3D PlayerOneValueLabel { get; set; }
	[Export] public Label3D PlayerTwoValueLabel { get; set; }

	[ExportCategory("Players")]
	[Export] public Player PlayerOne { get; set; }
	[Export] public Player PlayerTwo { get; set; }

	[ExportCategory("Managers")]
	[Export] public LightingManager LightingManager { get; set; }

	public CountdownChallengeManager Challenge => _countdownChallenge;

	public override void _Ready()
	{
		ResolveChallengeManager();
		ConnectChallengeSignals();
		CallDeferred(MethodName.BeginChallengeAfterTreeReady);
		LightingManager.DeactivateCeilingLights(); // Licht wird zum Spielstart ausgemacht
	}

	public override void _ExitTree()
	{
		DisconnectChallengeSignals();
	}

	public void StartChallenge()
	{
		_countdownChallenge?.StartChallenge();
	}

	public void ResetChallenge()
	{
		_countdownChallenge?.ResetChallenge();
	}

	private void BeginChallengeAfterTreeReady()
	{
		ResolvePlayerLabelsFromScene();
		ResolvePlayersFromScene();
		WireChallengeLabels();

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

	private void ResolvePlayersFromScene()
	{
		PlayerOne ??= GetNodeOrNull<Player>("../Player1");
		PlayerTwo ??= GetNodeOrNull<Player>("../Player2");
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

		LightingManager.ActivateCeilingLights();
	}
}
