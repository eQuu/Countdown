using System.Collections.Generic;
using Godot;

public partial class GameStore : Node
{
	public static GameStore Instance { get; private set; }

	public const int ColorCount = 4;

	public static readonly Color Blue = new(0.1f, 0.4f, 1.0f);
	public static readonly Color Red = new(1.0f, 0.15f, 0.1f);
	public static readonly Color Yellow = new(1.0f, 0.82f, 0.12f);
	public static readonly Color Green = new(0.18f, 0.85f, 0.28f);

	private static readonly Color[] Palette = [Blue, Red, Yellow, Green];
	private static readonly string[] PaletteNames = ["Blue", "Red", "Yellow", "Green"];

	[Signal]
	public delegate void ValueChangedEventHandler(string key, Variant value);

	[Signal]
	public delegate void StoreClearedEventHandler();

	[Signal]
	public delegate void ColorsChangedEventHandler(int playerOneIndex, int playerTwoIndex);

	[Signal]
	public delegate void ScoreChangedEventHandler(int playerOneScore, int playerTwoScore);

	[Signal]
	public delegate void WinnerChangedEventHandler(int winnerPlayerId);

	private readonly Dictionary<string, Variant> _values = new();

	public int PlayerOneColorIndex { get; private set; }
	public int PlayerTwoColorIndex { get; private set; } = 1;

	public Color PlayerOneColor => GetColorByIndex(PlayerOneColorIndex);
	public Color PlayerTwoColor => GetColorByIndex(PlayerTwoColorIndex);

	public int PlayerOneScore { get; private set; }
	public int PlayerTwoScore { get; private set; }

	/// <summary>1 oder 2 = Gewinner, 0 = noch keiner / Remis / zurückgesetzt.</summary>
	public int WinnerPlayerId { get; private set; }

	public const string MainMenuScenePath = "res://main_menu.tscn";
	public const string IngameScenePath = "res://ingame.tscn";
	public const string GameOverScenePath = "res://game_over.tscn";

	public override void _EnterTree()
	{
		Instance = this;
		SyncColorKeys();
		SyncScoreKeys();
		SyncWinnerKey();
	}

	public override void _ExitTree()
	{
		if (Instance == this)
		{
			Instance = null;
		}
	}

	public static Color GetColorByIndex(int index)
	{
		int clamped = Mathf.Clamp(index, 0, ColorCount - 1);
		return Palette[clamped];
	}

	public static string GetColorName(int index)
	{
		int clamped = Mathf.Clamp(index, 0, ColorCount - 1);
		return PaletteNames[clamped];
	}

	public Color GetPlayerColor(int playerId)
	{
		return playerId switch
		{
			2 => PlayerTwoColor,
			_ => PlayerOneColor
		};
	}

	public int GetPlayerColorIndex(int playerId)
	{
		return playerId switch
		{
			2 => PlayerTwoColorIndex,
			_ => PlayerOneColorIndex
		};
	}

	public void SetPlayerColorIndex(int playerId, int colorIndex)
	{
		int clamped = Mathf.Clamp(colorIndex, 0, ColorCount - 1);
		if (playerId == 2)
		{
			if (PlayerTwoColorIndex == clamped)
			{
				return;
			}

			PlayerTwoColorIndex = clamped;
		}
		else
		{
			if (PlayerOneColorIndex == clamped)
			{
				return;
			}

			PlayerOneColorIndex = clamped;
		}

		SyncColorKeys();
		EmitSignal(SignalName.ColorsChanged, PlayerOneColorIndex, PlayerTwoColorIndex);
	}

	public void SetPlayerColors(int playerOneIndex, int playerTwoIndex)
	{
		PlayerOneColorIndex = Mathf.Clamp(playerOneIndex, 0, ColorCount - 1);
		PlayerTwoColorIndex = Mathf.Clamp(playerTwoIndex, 0, ColorCount - 1);
		SyncColorKeys();
		EmitSignal(SignalName.ColorsChanged, PlayerOneColorIndex, PlayerTwoColorIndex);
	}

	public int GetScore(int playerId)
	{
		return playerId switch
		{
			2 => PlayerTwoScore,
			_ => PlayerOneScore
		};
	}

	public void SetScore(int playerId, int score)
	{
		int clamped = Mathf.Max(0, score);
		if (playerId == 2)
		{
			if (PlayerTwoScore == clamped)
			{
				return;
			}

			PlayerTwoScore = clamped;
		}
		else if (playerId == 1)
		{
			if (PlayerOneScore == clamped)
			{
				return;
			}

			PlayerOneScore = clamped;
		}
		else
		{
			return;
		}

		SyncScoreKeys();
		EmitSignal(SignalName.ScoreChanged, PlayerOneScore, PlayerTwoScore);
	}

	public void SetScores(int playerOneScore, int playerTwoScore)
	{
		PlayerOneScore = Mathf.Max(0, playerOneScore);
		PlayerTwoScore = Mathf.Max(0, playerTwoScore);
		SyncScoreKeys();
		EmitSignal(SignalName.ScoreChanged, PlayerOneScore, PlayerTwoScore);
	}

	public void AddScore(int playerId, int amount)
	{
		if (amount == 0 || playerId is not (1 or 2))
		{
			return;
		}

		SetScore(playerId, GetScore(playerId) + amount);
	}

	public void ResetScores()
	{
		SetScores(0, 0);
		ClearWinner();
	}

	public void SetWinner(int winnerPlayerId)
	{
		int clamped = winnerPlayerId is 1 or 2 ? winnerPlayerId : 0;
		if (WinnerPlayerId == clamped)
		{
			return;
		}

		WinnerPlayerId = clamped;
		SyncWinnerKey();
		EmitSignal(SignalName.WinnerChanged, WinnerPlayerId);
	}

	public void ClearWinner()
	{
		SetWinner(0);
	}

	public void GoToMainMenu() => ChangeToScene(MainMenuScenePath);

	public void GoToIngame()
	{
		ClearWinner();
		ChangeToScene(IngameScenePath);
	}

	public void GoToGameOver() => ChangeToScene(GameOverScenePath);

	public void ChangeToScene(string scenePath)
	{
		if (string.IsNullOrEmpty(scenePath))
		{
			GD.PushError("GameStore.ChangeToScene: scene path is empty.");
			return;
		}

		Error error = GetTree().ChangeSceneToFile(scenePath);
		if (error != Error.Ok)
		{
			GD.PushError($"GameStore.ChangeToScene failed for '{scenePath}': {error}");
		}
	}

	public bool Has(string key) => _values.ContainsKey(key);

	public void Set(string key, Variant value)
	{
		_values[key] = value;
		EmitSignal(SignalName.ValueChanged, key, value);
	}

	public Variant Get(string key, Variant defaultValue = default)
	{
		return _values.TryGetValue(key, out Variant value) ? value : defaultValue;
	}

	public T Get<[MustBeVariant] T>(string key, T defaultValue = default)
	{
		if (!_values.TryGetValue(key, out Variant value))
		{
			return defaultValue;
		}

		return value.As<T>();
	}

	public bool TryGet(string key, out Variant value) => _values.TryGetValue(key, out value);

	public bool Remove(string key)
	{
		if (!_values.Remove(key))
		{
			return false;
		}

		EmitSignal(SignalName.ValueChanged, key, default(Variant));
		return true;
	}

	public void Clear()
	{
		_values.Clear();
		PlayerOneColorIndex = 0;
		PlayerTwoColorIndex = 1;
		PlayerOneScore = 0;
		PlayerTwoScore = 0;
		WinnerPlayerId = 0;
		SyncColorKeys();
		SyncScoreKeys();
		SyncWinnerKey();
		EmitSignal(SignalName.StoreCleared);
		EmitSignal(SignalName.ColorsChanged, PlayerOneColorIndex, PlayerTwoColorIndex);
		EmitSignal(SignalName.ScoreChanged, PlayerOneScore, PlayerTwoScore);
		EmitSignal(SignalName.WinnerChanged, WinnerPlayerId);
	}

	public IReadOnlyDictionary<string, Variant> All => _values;

	public void SetBool(string key, bool value) => Set(key, value);
	public bool GetBool(string key, bool defaultValue = false) => Get(key, defaultValue);
	public void SetInt(string key, int value) => Set(key, value);
	public int GetInt(string key, int defaultValue = 0) => Get(key, defaultValue);
	public void SetFloat(string key, float value) => Set(key, value);
	public float GetFloat(string key, float defaultValue = 0.0f) => Get(key, defaultValue);
	public void SetString(string key, string value) => Set(key, value);
	public string GetString(string key, string defaultValue = "") => Get(key, defaultValue);
	public void SetColor(string key, Color value) => Set(key, value);
	public Color GetColor(string key, Color defaultValue = default) => Get(key, defaultValue);

	private void SyncColorKeys()
	{
		_values["player_one_color_index"] = PlayerOneColorIndex;
		_values["player_two_color_index"] = PlayerTwoColorIndex;
		_values["player_one_color"] = PlayerOneColor;
		_values["player_two_color"] = PlayerTwoColor;
	}

	private void SyncScoreKeys()
	{
		_values["player_one_score"] = PlayerOneScore;
		_values["player_two_score"] = PlayerTwoScore;
	}

	private void SyncWinnerKey()
	{
		_values["winner_player_id"] = WinnerPlayerId;
	}
}
