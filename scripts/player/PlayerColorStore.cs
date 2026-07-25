using Godot;

/// <summary>
/// Kompatibilitäts-Wrapper um <see cref="GameStore"/> für Spielerfarben.
/// </summary>
public partial class PlayerColorStore : Node
{
	public static PlayerColorStore Instance { get; private set; }

	public const int ColorCount = GameStore.ColorCount;

	public static readonly Color Blue = GameStore.Blue;
	public static readonly Color Red = GameStore.Red;
	public static readonly Color Yellow = GameStore.Yellow;
	public static readonly Color Green = GameStore.Green;

	[Signal]
	public delegate void ColorsChangedEventHandler(int playerOneIndex, int playerTwoIndex);

	public int PlayerOneColorIndex => GameStore.Instance?.PlayerOneColorIndex ?? 0;
	public int PlayerTwoColorIndex => GameStore.Instance?.PlayerTwoColorIndex ?? 1;

	public Color PlayerOneColor => GameStore.Instance?.PlayerOneColor ?? Blue;
	public Color PlayerTwoColor => GameStore.Instance?.PlayerTwoColor ?? Red;

	public override void _EnterTree()
	{
		Instance = this;
	}

	public override void _Ready()
	{
		if (GameStore.Instance != null)
		{
			GameStore.Instance.ColorsChanged += OnGameStoreColorsChanged;
		}
	}

	public override void _ExitTree()
	{
		if (GameStore.Instance != null)
		{
			GameStore.Instance.ColorsChanged -= OnGameStoreColorsChanged;
		}

		if (Instance == this)
		{
			Instance = null;
		}
	}

	public static Color GetColorByIndex(int index) => GameStore.GetColorByIndex(index);

	public static string GetColorName(int index) => GameStore.GetColorName(index);

	public Color GetColor(int playerId) =>
		GameStore.Instance?.GetPlayerColor(playerId) ?? (playerId == 2 ? Red : Blue);

	public int GetColorIndex(int playerId) =>
		GameStore.Instance?.GetPlayerColorIndex(playerId) ?? (playerId == 2 ? 1 : 0);

	public void SetColorIndex(int playerId, int colorIndex)
	{
		GameStore.Instance?.SetPlayerColorIndex(playerId, colorIndex);
	}

	public int CycleColorIndex(int playerId, int direction, int? blockedIndex = null)
	{
		int current = GetColorIndex(playerId);
		int step = direction >= 0 ? 1 : -1;
		int next = current;

		for (int i = 0; i < ColorCount; i++)
		{
			next = (next + step + ColorCount) % ColorCount;
			if (blockedIndex.HasValue && next == blockedIndex.Value)
			{
				continue;
			}

			SetColorIndex(playerId, next);
			return next;
		}

		return current;
	}

	private void OnGameStoreColorsChanged(int playerOneIndex, int playerTwoIndex)
	{
		EmitSignal(SignalName.ColorsChanged, playerOneIndex, playerTwoIndex);
	}
}
