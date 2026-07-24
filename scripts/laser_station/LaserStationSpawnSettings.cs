using Godot;

public struct LaserStationSpawnSettings
{
	public int? PatternIndex;
	public bool? EnableRotation;
	public bool? RotateClockwise;
	public float? RotationSpeedDegrees;
	public bool? EnableExpandOnActivate;
	public LaserRetractStyle? RetractStyle;
	public float? ExpandSpeed;
	public float? RetractSpeed;
	public float? ExpandStartFactor;
	public float? HubRadius;
	public float? LifetimeMinSeconds;
	public float? LifetimeMaxSeconds;

	public static LaserStationSpawnSettings FromRandom(
		RandomNumberGenerator rng,
		LaserPatternConfig patternConfig = null,
		float rotateChance = 1.0f,
		float expandChance = 0.65f,
		float speedMin = 30.0f,
		float speedMax = 90.0f,
		float expandSpeedMin = 2.5f,
		float expandSpeedMax = 5.5f,
		float expandStartMin = 0.05f,
		float expandStartMax = 0.45f
	)
	{
		LaserPatternConfig config = patternConfig ?? LaserPatternConfig.CreateDefault();
		float min = Mathf.Min(speedMin, speedMax);
		float max = Mathf.Max(speedMin, speedMax);
		float expandMin = Mathf.Min(expandSpeedMin, expandSpeedMax);
		float expandMax = Mathf.Max(expandSpeedMin, expandSpeedMax);
		float startMin = Mathf.Clamp(Mathf.Min(expandStartMin, expandStartMax), 0.01f, 1.0f);
		float startMax = Mathf.Clamp(Mathf.Max(expandStartMin, expandStartMax), 0.01f, 1.0f);

		return new LaserStationSpawnSettings
		{
			PatternIndex = config.RandomIndex(rng),
			EnableRotation = rng.Randf() <= rotateChance,
			RotateClockwise = rng.Randf() < 0.5f,
			RotationSpeedDegrees = rng.RandfRange(min, max),
			EnableExpandOnActivate = rng.Randf() <= expandChance,
			RetractStyle = rng.Randf() < 0.5f
				? LaserRetractStyle.TowardCenter
				: LaserRetractStyle.TowardOutside,
			ExpandSpeed = rng.RandfRange(expandMin, expandMax),
			ExpandStartFactor = rng.RandfRange(startMin, startMax)
		};
	}
}
