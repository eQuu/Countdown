using Godot;

[GlobalClass]
public partial class LaserPatternConfig : Resource
{
	[Export]
	public Godot.Collections.Array<LaserPatternDefinition> Patterns { get; set; } = new();

	public static LaserPatternConfig CreateDefault()
	{
		LaserPatternConfig config = new();
		config.Patterns =
		[
			Make("cross_four", "Cross (4x 90 deg)", 0f, 90f, 180f, 270f),
			Make("single", "Single", 0f),
			Make("triple", "Triple (120 deg)", 0f, 120f, 240f),
			Make("line_two", "Line (2x)", 0f, 180f)
		];
		return config;
	}

	public int Count => Patterns?.Count ?? 0;

	public LaserPatternDefinition GetPattern(int index)
	{
		if (Patterns == null || Patterns.Count == 0)
		{
			return Make("cross_four", "Cross (4x 90 deg)", 0f, 90f, 180f, 270f);
		}

		int clamped = Mathf.Clamp(index, 0, Patterns.Count - 1);
		LaserPatternDefinition pattern = Patterns[clamped];
		if (pattern == null || pattern.ArmAnglesDegrees == null || pattern.ArmAnglesDegrees.Length == 0)
		{
			return Make("cross_four", "Cross (4x 90 deg)", 0f, 90f, 180f, 270f);
		}

		return pattern;
	}

	public float[] GetArmAnglesDegrees(int index)
	{
		return GetPattern(index).ArmAnglesDegrees;
	}

	public string GetDisplayName(int index)
	{
		LaserPatternDefinition pattern = GetPattern(index);
		return string.IsNullOrEmpty(pattern.DisplayName) ? pattern.Id : pattern.DisplayName;
	}

	public int FindIndexById(string id)
	{
		if (Patterns == null || string.IsNullOrEmpty(id))
		{
			return 0;
		}

		for (int i = 0; i < Patterns.Count; i++)
		{
			if (Patterns[i] != null && Patterns[i].Id == id)
			{
				return i;
			}
		}

		return 0;
	}

	public int NextIndex(int currentIndex)
	{
		if (Count <= 0)
		{
			return 0;
		}

		return (currentIndex + 1) % Count;
	}

	public int RandomIndex(RandomNumberGenerator rng)
	{
		if (Count <= 0)
		{
			return 0;
		}

		return rng.RandiRange(0, Count - 1);
	}

	private static LaserPatternDefinition Make(string id, string displayName, params float[] angles)
	{
		return new LaserPatternDefinition
		{
			Id = id,
			DisplayName = displayName,
			ArmAnglesDegrees = angles
		};
	}
}
