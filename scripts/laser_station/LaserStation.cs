using System.Collections.Generic;
using Godot;

public enum LaserOwner
{
	Neutral = 0,
	PlayerOne = 1,
	PlayerTwo = 2
}

public enum LaserRetractStyle
{
	TowardCenter = 0,
	TowardOutside = 1
}

public partial class LaserStation : Node3D
{
	[Signal]
	public delegate void StationCapturedEventHandler(
		int playerId,
		int previousOwner,
		int newOwner
	);

	[Signal]
	public delegate void PlayerHitEventHandler(
		int attackingPlayerId,
		int hitPlayerId
	);

	[ExportCategory("Pattern")]
	[Export] public LaserPatternConfig PatternConfig { get; set; }
	[Export] public int PatternIndex { get; set; }

	[ExportCategory("Laser")]
	[Export] public bool EnableRotation { get; set; } = true;
	[Export] public float RotationSpeedDegrees { get; set; } = 60.0f;
	[Export] public bool RotateClockwise { get; set; } = true;
	[Export] public float MaxLaserLength { get; set; } = 12.0f;
	[Export] public float MinimumLaserLength { get; set; } = 0.1f;
	[Export] public float WallDistanceOffset { get; set; } = 0.22f;
	[Export] public float StripeWidth { get; set; } = 0.18f;
	[Export(PropertyHint.Layers3DPhysics)]
	public uint WorldCollisionMask { get; set; } = 1;
	[Export] public bool StartActive { get; set; } = false;

	[ExportCategory("Hub")]
	[Export] public float HubRadius { get; set; } = 0.75f;
	[Export] public bool ScaleCaptureAreaWithHub { get; set; } = true;
	[Export] public float CaptureRadiusPadding { get; set; } = 0.9f;

	[ExportCategory("Expansion")]
	[Export] public bool EnableExpandOnActivate { get; set; } = true;
	[Export] public LaserRetractStyle RetractStyle { get; set; } = LaserRetractStyle.TowardCenter;
	[Export(PropertyHint.Range, "0.01,1,0.01")]
	public float ExpandStartFactor { get; set; } = 0.12f;
	[Export] public float ExpandSpeed { get; set; } = 4.0f;
	[Export] public float MinimumExpandLength { get; set; } = 0.55f;
	[Export] public float RetractSpeed { get; set; } = 2.2f;
	[Export] public float RetractFadeSeconds { get; set; } = 1.0f;

	[ExportCategory("Lifetime")]
	[Export] public bool EnableLifetime { get; set; } = true;
	[Export] public float LifetimeMinSeconds { get; set; } = 5.0f;
	[Export] public float LifetimeMaxSeconds { get; set; } = 15.0f;
	[Export] public bool RandomizeLifetime { get; set; } = true;

	[ExportCategory("Pattern Transition")]
	[Export] public bool RandomizePatternOnExpire { get; set; } = true;
	[Export] public float PatternFadeInSeconds { get; set; } = 1.5f;
	[Export] public float PatternSwapDelaySeconds { get; set; } = 0.3f;

	[ExportCategory("Spawn Randomization")]
	[Export] public bool RandomizeOnReady { get; set; } = true;
	[Export] public bool RandomizePattern { get; set; } = true;
	[Export] public bool RandomizeHubRadius { get; set; } = true;
	[Export] public bool RandomizeRotationEnabled { get; set; } = false;
	[Export] public bool RandomizeRotationDirection { get; set; } = true;
	[Export] public bool RandomizeRotationSpeed { get; set; } = true;
	[Export] public bool RandomizeExpandOnActivate { get; set; } = false;
	[Export] public bool RandomizeRetractStyle { get; set; } = true;
	[Export] public bool RandomizeExpandSpeed { get; set; } = true;
	[Export] public bool RandomizeRetractSpeed { get; set; } = true;
	[Export] public bool RandomizeExpandStartFactor { get; set; } = true;
	[Export] public bool RandomizeLifetimeRange { get; set; } = false;
	[Export(PropertyHint.Range, "0,1,0.05")]
	public float RandomRotateChance { get; set; } = 1.0f;
	[Export(PropertyHint.Range, "0,1,0.05")]
	public float RandomExpandChance { get; set; } = 1.0f;
	[Export(PropertyHint.Range, "0,1,0.05")]
	public float RandomRetractTowardOutsideChance { get; set; } = 0.5f;
	[Export] public float RandomSpeedMin { get; set; } = 30.0f;
	[Export] public float RandomSpeedMax { get; set; } = 90.0f;
	[Export] public float RandomExpandSpeedMin { get; set; } = 2.5f;
	[Export] public float RandomExpandSpeedMax { get; set; } = 5.5f;
	[Export] public float RandomRetractSpeedMin { get; set; } = 1.5f;
	[Export] public float RandomRetractSpeedMax { get; set; } = 3.5f;
	[Export(PropertyHint.Range, "0.01,1,0.01")]
	public float RandomExpandStartFactorMin { get; set; } = 0.1f;
	[Export(PropertyHint.Range, "0.01,1,0.01")]
	public float RandomExpandStartFactorMax { get; set; } = 0.35f;
	[Export] public float RandomHubRadiusMin { get; set; } = 0.55f;
	[Export] public float RandomHubRadiusMax { get; set; } = 1.45f;
	[Export] public float RandomLifetimeMinSeconds { get; set; } = 5.0f;
	[Export] public float RandomLifetimeMaxSeconds { get; set; } = 15.0f;

	[ExportCategory("Capture")]
	[Export] public float CaptureCooldownSeconds { get; set; } = 0.25f;
	[Export] public float CaptureLockoutSeconds { get; set; } = 2.5f;
	[Export] public bool ResetPlayerTimerOnCapture { get; set; } = true;

	[ExportCategory("Colors")]
	[Export] public Color PlaceholderColor { get; set; } = Colors.White;
	[Export] public Color PlayerOneColor { get; set; } = new Color(0.1f, 0.4f, 1.0f);
	[Export] public Color PlayerTwoColor { get; set; } = new Color(1.0f, 0.15f, 0.1f);
	[Export] public float EmissionEnergy { get; set; } = 4.0f;

	[ExportCategory("Combat")]
	[Export] public float HitCooldownSeconds { get; set; } = 0.75f;
	[Export] public float LaserWallHeight { get; set; } = 3.1f;
	[Export] public float LaserWallThickness { get; set; } = 0.02f;
	[Export] public float WallGroundEmbed { get; set; } = 0.18f;
	[Export(PropertyHint.Range, "0.05,1,0.01")]
	public float WallFillAlpha { get; set; } = 0.22f;
	[Export(PropertyHint.Range, "0.05,1,0.01")]
	public float WallGlowAlpha { get; set; } = 0.3f;
	[Export] public float WallGlowThicknessScale { get; set; } = 4.0f;
	[Export] public float WallGlowHeightScale { get; set; } = 1.08f;
	[Export] public float WallGlowEnergy { get; set; } = 7.5f;

	public LaserOwner CurrentOwner { get; private set; } = LaserOwner.Neutral;

	public int OwnerPlayerId => (int)CurrentOwner;

	public bool IsActive { get; private set; }

	public bool IsNeutral => CurrentOwner == LaserOwner.Neutral;

	public int CurrentPatternIndex => PatternIndex;

	public string CurrentPatternName => EnsurePatternConfig().GetDisplayName(PatternIndex);

	public float CurrentLifetimeSeconds { get; private set; }

	public float LifetimeRemainingSeconds
	{
		get
		{
			if (_lifetimeTimer == null || _lifetimeTimer.IsStopped())
			{
				return 0.0f;
			}

			return (float)_lifetimeTimer.TimeLeft;
		}
	}

	private Node3D _laserPivot;
	private Area3D _captureArea;
	private CollisionShape3D _captureCollision;
	private GpuParticles3D _activationParticles;
	private AudioStreamPlayer3D _activationAudio;
	private Timer _captureCooldownTimer;
	private Timer _lifetimeTimer;

	private readonly List<Node3D> _laserArms = new();
	private readonly List<Node3D> _floorMarkRoots = new();
	private readonly List<MeshInstance3D> _placeholderMeshes = new();
	private readonly List<MeshInstance3D> _laserWalls = new();
	private readonly List<MeshInstance3D> _laserWallGlows = new();
	private readonly List<Area3D> _laserHitAreas = new();
	private readonly List<CollisionShape3D> _laserHitCollisions = new();
	private readonly List<RayCast3D> _laserRayCasts = new();

	private StandardMaterial3D _stripeMaterial;
	private StandardMaterial3D _wallMaterial;
	private StandardMaterial3D _wallGlowMaterial;

	private bool _captureLocked;
	private bool _nodesReady;
	private bool _isExpanding;
	private bool _isRetracting;
	private bool _isFadingPlaceholders;
	private bool _placeholderFadeIn;
	private bool _waitingPatternSwap;
	private bool _hitboxesWantedEnabled;
	private float _lengthFactor = 1.0f;
	private float _placeholderAlpha = 1.0f;
	private float _wallAlpha = 1.0f;
	private float _fadeProgress;
	private float _patternSwapWait;
	private float _retractProgress;
	private float _retractFadeProgress;
	private float _retractDuration = 1.5f;
	private bool _retractShrinkDone;
	private float _expandProgress;
	private float _expandDuration = 1.0f;
	private readonly List<float> _lockedArmReach = new();

	private readonly Dictionary<int, double> _playerHitCooldowns = new();

	public override void _Ready()
	{
		if (!ResolveCoreNodes())
		{
			SetPhysicsProcess(false);
			return;
		}

		ConnectCoreSignals();
		ConfigureCaptureTimer();
		EnsureLifetimeTimer();
		CreateMaterials();
		ApplyHubLayout();

		if (RandomizeOnReady)
		{
			RandomizeSpawnSettings();
		}
		else
		{
			RebuildPattern();
		}

		_nodesReady = true;
		ResetStation();

		if (StartActive && CurrentOwner != LaserOwner.Neutral)
		{
			Activate();
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!_nodesReady)
		{
			return;
		}

		UpdateHitCooldowns(delta);
		UpdateExpansion(delta);
		UpdatePlaceholderFade(delta);
		RotateLaser(delta);
		UpdateLaserLength();
		ProcessLaserOverlaps();
	}

	public void SetPattern(int patternIndex)
	{
		ClearPlaceholderFade();
		LaserPatternConfig config = EnsurePatternConfig();
		PatternIndex = config.Count > 0
			? Mathf.Clamp(patternIndex, 0, config.Count - 1)
			: 0;

		if (!_nodesReady)
		{
			return;
		}

		bool wasActive = IsActive;
		RebuildPattern();
		UpdateVisuals();

		if (wasActive && CurrentOwner != LaserOwner.Neutral)
		{
			Activate();
		}
		else
		{
			Deactivate();
		}
	}

	public void SetPatternById(string patternId)
	{
		SetPattern(EnsurePatternConfig().FindIndexById(patternId));
	}

	public void CyclePattern()
	{
		SetPattern(EnsurePatternConfig().NextIndex(PatternIndex));
	}

	public void Activate()
	{
		if (!_nodesReady || CurrentOwner == LaserOwner.Neutral)
		{
			return;
		}

		IsActive = true;
		ClearPlaceholderFade();
		_wallAlpha = 1.0f;
		RandomizeExpandSpeedForActivation();
		BeginExpansion();
		SetLaserHitboxesEnabled(true);
		ApplyModeVisuals();
		UpdateLaserLength();
		StartLifetime();
		CallDeferred(MethodName.EnsureActiveWallsReady);
	}

	public void Deactivate()
	{
		if (!_nodesReady)
		{
			return;
		}

		IsActive = false;
		StopLifetime();
		_isExpanding = false;
		_isRetracting = false;
		_lengthFactor = EnableExpandOnActivate
			? GetExpandStartFactor()
			: 1.0f;
		UpdateLaserLength();
		SetLaserHitboxesEnabled(false);
		ApplyModeVisuals();
	}

	public void SetOwner(LaserOwner newOwner)
	{
		if (!_nodesReady)
		{
			CurrentOwner = newOwner;
			return;
		}

		CurrentOwner = newOwner;
		UpdateVisuals();

		if (newOwner == LaserOwner.Neutral)
		{
			Deactivate();
		}
		else
		{
			Activate();
		}
	}

	public void ResetStation()
	{
		CurrentOwner = LaserOwner.Neutral;
		IsActive = false;
		_captureLocked = false;
		_playerHitCooldowns.Clear();
		StopLifetime();
		CurrentLifetimeSeconds = 0.0f;
		_isExpanding = false;
		_isRetracting = false;
		_retractShrinkDone = false;
		_retractProgress = 0.0f;
		_retractFadeProgress = 0.0f;
		_wallAlpha = 1.0f;
		ClearPlaceholderFade();

		if (_captureCooldownTimer != null && !_captureCooldownTimer.IsStopped())
		{
			_captureCooldownTimer.Stop();
		}

		if (_laserPivot != null && !EnableRotation)
		{
			_laserPivot.Rotation = Vector3.Zero;
		}

		if (_activationParticles != null)
		{
			_activationParticles.Emitting = false;
		}

		if (_nodesReady)
		{
			UpdateVisuals();
			Deactivate();
			CallDeferred(MethodName.TryCaptureOverlappingPlayers);
		}
		else
		{
			SetLaserHitboxesEnabled(false);
		}
	}

	public void ForceCaptureCheck()
	{
		TryCaptureOverlappingPlayers();
	}

	public void SetRotationSpeed(float degreesPerSecond)
	{
		RotationSpeedDegrees = Mathf.Max(0.0f, degreesPerSecond);
	}

	public void SetRotationDirection(bool clockwise)
	{
		RotateClockwise = clockwise;
	}

	public void SetRotationEnabled(bool enabled)
	{
		EnableRotation = enabled;
	}

	public void SetExpandOnActivate(bool enabled)
	{
		EnableExpandOnActivate = enabled;
	}

	public void SetExpandSpeed(float worldUnitsPerSecond)
	{
		ExpandSpeed = Mathf.Max(0.1f, worldUnitsPerSecond);
	}

	public void ApplySpawnSettings(LaserStationSpawnSettings settings)
	{
		if (settings.PatternIndex.HasValue)
		{
			PatternIndex = settings.PatternIndex.Value;
		}

		if (settings.EnableRotation.HasValue)
		{
			EnableRotation = settings.EnableRotation.Value;
		}

		if (settings.RotateClockwise.HasValue)
		{
			RotateClockwise = settings.RotateClockwise.Value;
		}

		if (settings.RotationSpeedDegrees.HasValue)
		{
			RotationSpeedDegrees = Mathf.Max(0.0f, settings.RotationSpeedDegrees.Value);
		}

		if (settings.EnableExpandOnActivate.HasValue)
		{
			EnableExpandOnActivate = settings.EnableExpandOnActivate.Value;
		}

		if (settings.RetractStyle.HasValue)
		{
			RetractStyle = settings.RetractStyle.Value;
		}

		if (settings.ExpandSpeed.HasValue)
		{
			ExpandSpeed = Mathf.Max(0.1f, settings.ExpandSpeed.Value);
		}

		if (settings.RetractSpeed.HasValue)
		{
			RetractSpeed = Mathf.Max(0.1f, settings.RetractSpeed.Value);
		}

		if (settings.ExpandStartFactor.HasValue)
		{
			ExpandStartFactor = Mathf.Clamp(settings.ExpandStartFactor.Value, 0.01f, 1.0f);
		}

		if (settings.HubRadius.HasValue)
		{
			HubRadius = Mathf.Max(0.05f, settings.HubRadius.Value);
		}

		if (settings.LifetimeMinSeconds.HasValue)
		{
			LifetimeMinSeconds = Mathf.Max(0.1f, settings.LifetimeMinSeconds.Value);
		}

		if (settings.LifetimeMaxSeconds.HasValue)
		{
			LifetimeMaxSeconds = Mathf.Max(0.1f, settings.LifetimeMaxSeconds.Value);
		}

		if (_nodesReady || _laserPivot != null)
		{
			ApplyHubLayout();
			RebuildPattern();
			UpdateVisuals();
		}
	}

	public void RandomizeSpawnSettings(RandomNumberGenerator rng = null)
	{
		rng ??= new RandomNumberGenerator();
		rng.Randomize();

		LaserStationSpawnSettings settings = new();
		settings.EnableRotation = true;

		if (RandomizePattern)
		{
			settings.PatternIndex = EnsurePatternConfig().RandomIndex(rng);
		}

		if (RandomizeHubRadius)
		{
			float min = Mathf.Max(0.05f, Mathf.Min(RandomHubRadiusMin, RandomHubRadiusMax));
			float max = Mathf.Max(0.05f, Mathf.Max(RandomHubRadiusMin, RandomHubRadiusMax));
			settings.HubRadius = rng.RandfRange(min, max);
		}

		if (RandomizeRotationEnabled)
		{
			settings.EnableRotation = rng.Randf() <= Mathf.Clamp(RandomRotateChance, 0.0f, 1.0f);
		}

		if (RandomizeRotationDirection)
		{
			settings.RotateClockwise = rng.Randf() < 0.5f;
		}

		if (RandomizeRotationSpeed)
		{
			float min = Mathf.Min(RandomSpeedMin, RandomSpeedMax);
			float max = Mathf.Max(RandomSpeedMin, RandomSpeedMax);
			settings.RotationSpeedDegrees = rng.RandfRange(min, max);
		}

		if (RandomizeExpandOnActivate)
		{
			settings.EnableExpandOnActivate =
				rng.Randf() <= Mathf.Clamp(RandomExpandChance, 0.0f, 1.0f);
		}

		if (RandomizeRetractStyle)
		{
			settings.RetractStyle = rng.Randf() <= Mathf.Clamp(RandomRetractTowardOutsideChance, 0.0f, 1.0f)
				? LaserRetractStyle.TowardOutside
				: LaserRetractStyle.TowardCenter;
		}

		if (RandomizeExpandSpeed)
		{
			float min = Mathf.Min(RandomExpandSpeedMin, RandomExpandSpeedMax);
			float max = Mathf.Max(RandomExpandSpeedMin, RandomExpandSpeedMax);
			settings.ExpandSpeed = rng.RandfRange(min, max);
		}

		if (RandomizeRetractSpeed)
		{
			float min = Mathf.Min(RandomRetractSpeedMin, RandomRetractSpeedMax);
			float max = Mathf.Max(RandomRetractSpeedMin, RandomRetractSpeedMax);
			settings.RetractSpeed = rng.RandfRange(min, max);
		}

		if (RandomizeExpandStartFactor)
		{
			float min = Mathf.Clamp(
				Mathf.Min(RandomExpandStartFactorMin, RandomExpandStartFactorMax),
				0.01f,
				1.0f
			);
			float max = Mathf.Clamp(
				Mathf.Max(RandomExpandStartFactorMin, RandomExpandStartFactorMax),
				0.01f,
				1.0f
			);
			settings.ExpandStartFactor = rng.RandfRange(min, max);
		}

		if (RandomizeLifetimeRange)
		{
			float min = Mathf.Max(0.1f, Mathf.Min(RandomLifetimeMinSeconds, RandomLifetimeMaxSeconds));
			float max = Mathf.Max(0.1f, Mathf.Max(RandomLifetimeMinSeconds, RandomLifetimeMaxSeconds));
			settings.LifetimeMinSeconds = min;
			settings.LifetimeMaxSeconds = max;
		}

		ApplySpawnSettings(settings);
	}

	private void OnCaptureAreaBodyEntered(Node3D body)
	{
		TryCaptureBody(body);
	}

	private void TryCaptureOverlappingPlayers()
	{
		if (_captureArea == null || _captureLocked || !_nodesReady || _isFadingPlaceholders)
		{
			return;
		}

		foreach (Node3D body in _captureArea.GetOverlappingBodies())
		{
			TryCaptureBody(body);
			if (_captureLocked)
			{
				return;
			}
		}
	}

	private void TryCaptureBody(Node3D body)
	{
		if (_captureLocked || !_nodesReady || _isFadingPlaceholders)
		{
			return;
		}

		if (body is not ILaserPlayer player)
		{
			return;
		}

		if (player.PlayerId is not 1 and not 2)
		{
			GD.PushWarning(
				$"LaserStation received invalid PlayerId {player.PlayerId}."
			);
			return;
		}

		if (!player.IsAlive)
		{
			return;
		}

		LaserOwner newOwner = player.PlayerId == 1
			? LaserOwner.PlayerOne
			: LaserOwner.PlayerTwo;

		if (CurrentOwner == newOwner)
		{
			return;
		}

		LaserOwner previousOwner = CurrentOwner;

		SetOwner(newOwner);
		PlayCaptureEffects();

		if (ResetPlayerTimerOnCapture)
		{
			player.ResetPersonalCountdown();
		}

		_captureLocked = true;
		float lockout = Mathf.Max(CaptureCooldownSeconds, CaptureLockoutSeconds);
		_captureCooldownTimer?.Start(Mathf.Max(0.05f, lockout));

		EmitSignal(
			SignalName.StationCaptured,
			player.PlayerId,
			(int)previousOwner,
			(int)newOwner
		);
	}

	private void OnCaptureCooldownTimeout()
	{
		_captureLocked = false;
		TryCaptureOverlappingPlayers();
	}

	private void PlayCaptureEffects()
	{
		if (_activationParticles != null)
		{
			_activationParticles.Restart();
			_activationParticles.Emitting = true;
		}

		if (_activationAudio?.Stream != null)
		{
			_activationAudio.Play();
		}
	}

	private void OnLaserHitAreaBodyEntered(Node3D body)
	{
		TryHitBody(body);
	}

	private void ProcessLaserOverlaps()
	{
		if (!_nodesReady || !IsActive || CurrentOwner == LaserOwner.Neutral)
		{
			return;
		}

		foreach (Area3D hitArea in _laserHitAreas)
		{
			if (!hitArea.Monitoring)
			{
				continue;
			}

			foreach (Node3D body in hitArea.GetOverlappingBodies())
			{
				TryHitBody(body);
			}
		}
	}

	private void TryHitBody(Node3D body)
	{
		if (!_nodesReady || !IsActive || CurrentOwner == LaserOwner.Neutral)
		{
			return;
		}

		if (body is not ILaserPlayer player)
		{
			return;
		}

		if (!player.IsAlive || player.IsInvulnerable)
		{
			return;
		}

		if (player.PlayerId == OwnerPlayerId)
		{
			return;
		}

		if (IsPlayerOnHitCooldown(player.PlayerId))
		{
			return;
		}

		player.HitByLaser(OwnerPlayerId);
		_playerHitCooldowns[player.PlayerId] = HitCooldownSeconds;

		EmitSignal(
			SignalName.PlayerHit,
			OwnerPlayerId,
			player.PlayerId
		);
	}

	private bool IsPlayerOnHitCooldown(int playerId)
	{
		return _playerHitCooldowns.TryGetValue(playerId, out double remaining)
			&& remaining > 0.0;
	}

	private void UpdateHitCooldowns(double delta)
	{
		if (_playerHitCooldowns.Count == 0)
		{
			return;
		}

		List<int> playerIds = new(_playerHitCooldowns.Keys);

		foreach (int playerId in playerIds)
		{
			double remaining = _playerHitCooldowns[playerId] - delta;
			if (remaining <= 0.0)
			{
				_playerHitCooldowns.Remove(playerId);
			}
			else
			{
				_playerHitCooldowns[playerId] = remaining;
			}
		}
	}

	private void RotateLaser(double delta)
	{
		if (!EnableRotation || _laserPivot == null || RotationSpeedDegrees <= 0.0f)
		{
			return;
		}

		float direction = RotateClockwise ? -1.0f : 1.0f;
		float radiansPerSecond = Mathf.DegToRad(RotationSpeedDegrees);
		_laserPivot.RotateY(direction * radiansPerSecond * (float)delta);
	}

	private void BeginExpansion()
	{
		_isRetracting = false;
		CaptureLockedArmReach();

		if (EnableExpandOnActivate)
		{
			_lengthFactor = GetExpandStartFactor();
			_isExpanding = true;
			_expandProgress = 0.0f;

			float avgReach = GetAverageLockedReach();
			float travel = Mathf.Max(0.1f, avgReach * (1.0f - _lengthFactor));
			_expandDuration = Mathf.Max(0.85f, travel / Mathf.Max(0.1f, ExpandSpeed));
			_expandDuration *= 1.1f;
		}
		else
		{
			_lengthFactor = 1.0f;
			_isExpanding = false;
			_expandProgress = 1.0f;
		}

		UpdateLaserLength();
	}

	private void RandomizeExpandSpeedForActivation()
	{
		if (!RandomizeExpandSpeed)
		{
			return;
		}

		float min = Mathf.Min(RandomExpandSpeedMin, RandomExpandSpeedMax);
		float max = Mathf.Max(RandomExpandSpeedMin, RandomExpandSpeedMax);
		ExpandSpeed = (float)GD.RandRange(min, max);
	}

	private void RandomizeRetractStyleForExpire()
	{
		if (!RandomizeRetractStyle)
		{
			return;
		}

		RetractStyle = GD.Randf() <= Mathf.Clamp(RandomRetractTowardOutsideChance, 0.0f, 1.0f)
			? LaserRetractStyle.TowardOutside
			: LaserRetractStyle.TowardCenter;
	}

	private void BeginRetract()
	{
		if (!_nodesReady || !IsActive || CurrentOwner == LaserOwner.Neutral)
		{
			StartExpiredPatternTransition();
			return;
		}

		_isExpanding = false;
		_isRetracting = true;
		RandomizeRetractStyleForExpire();
		CaptureLockedArmReach();

		_retractProgress = 0.0f;
		_retractFadeProgress = 0.0f;
		_retractShrinkDone = false;
		_wallAlpha = 1.0f;

		float avgReach = GetAverageLockedReach();
		float placeholderLen = GetUniformPlaceholderLength();
		float travel = Mathf.Max(0.15f, avgReach - Mathf.Min(avgReach, placeholderLen));
		_retractDuration = Mathf.Max(1.1f, travel / Mathf.Max(0.1f, RetractSpeed));
		_retractDuration *= 1.15f;

		SetLaserHitboxesEnabled(false);
		ApplyModeVisuals();
		UpdateLaserLength();
	}

	private void UpdateExpansion(double delta)
	{
		if (!IsActive || CurrentOwner == LaserOwner.Neutral)
		{
			return;
		}

		if (_isRetracting)
		{
			UpdateRetract(delta);
			return;
		}

		if (!_isExpanding)
		{
			return;
		}

		_expandProgress = Mathf.MoveToward(
			_expandProgress,
			1.0f,
			(float)delta / Mathf.Max(0.2f, _expandDuration)
		);

		float startFactor = GetExpandStartFactor();
		float eased = EaseOutCubic(_expandProgress);
		_lengthFactor = Mathf.Lerp(startFactor, 1.0f, eased);

		if (_expandProgress >= 0.999f)
		{
			_lengthFactor = 1.0f;
			_expandProgress = 1.0f;
			_isExpanding = false;
		}
	}

	private void UpdateRetract(double delta)
	{
		if (!_retractShrinkDone)
		{
			_retractProgress = Mathf.MoveToward(
				_retractProgress,
				1.0f,
				(float)delta / Mathf.Max(0.25f, _retractDuration)
			);

			float eased = EaseInOutCubic(_retractProgress);
			_lengthFactor = Mathf.Lerp(1.0f, GetRetractTargetFactor(), eased);
			_wallAlpha = 1.0f;
			ApplyWallFadeVisuals();

			if (_retractProgress >= 0.999f)
			{
				_retractProgress = 1.0f;
				_lengthFactor = GetRetractTargetFactor();
				_retractShrinkDone = true;
				_retractFadeProgress = 0.0f;
				_wallAlpha = 1.0f;
				ApplyWallFadeVisuals();
			}

			return;
		}

		_retractFadeProgress = Mathf.MoveToward(
			_retractFadeProgress,
			1.0f,
			(float)delta / Mathf.Max(0.1f, RetractFadeSeconds)
		);

		float fadeT = EaseInOutCubic(_retractFadeProgress);
		_wallAlpha = Mathf.Lerp(1.0f, 0.0f, fadeT);
		ApplyWallFadeVisuals();

		if (_retractFadeProgress >= 0.999f)
		{
			_wallAlpha = 0.0f;
			_isRetracting = false;
			_retractShrinkDone = false;
			ApplyWallFadeVisuals();
			StartExpiredPatternTransition();
		}
	}

	private void CaptureLockedArmReach()
	{
		_lockedArmReach.Clear();

		float maxLength = Mathf.Max(0.5f, MaxLaserLength);
		for (int i = 0; i < _laserRayCasts.Count; i++)
		{
			_lockedArmReach.Add(MeasureWorldReach(i, maxLength));
		}
	}

	private float GetAverageLockedReach()
	{
		if (_lockedArmReach.Count == 0)
		{
			return Mathf.Max(0.5f, MaxLaserLength);
		}

		float sum = 0.0f;
		foreach (float reach in _lockedArmReach)
		{
			sum += reach;
		}

		return sum / _lockedArmReach.Count;
	}

	private float GetLockedArmReach(int armIndex, float fallback)
	{
		if (armIndex >= 0 && armIndex < _lockedArmReach.Count)
		{
			return _lockedArmReach[armIndex];
		}

		return fallback;
	}

	private float GetRetractTargetFactor()
	{
		return GetExpandStartFactor();
	}

	private float GetHubRadius()
	{
		return Mathf.Max(0.05f, HubRadius);
	}

	private float GetExpandStartFactor()
	{
		float factor = Mathf.Clamp(ExpandStartFactor, 0.01f, 1.0f);
		float maxLength = Mathf.Max(0.5f, MaxLaserLength);
		float minFactor = Mathf.Clamp(
			MinimumExpandLength / maxLength,
			0.01f,
			1.0f
		);
		return Mathf.Max(factor, minFactor);
	}

	private void UpdateLaserLength()
	{
		if (!_nodesReady || _laserRayCasts.Count == 0)
		{
			return;
		}

		float maxLength = Mathf.Max(0.5f, MaxLaserLength);
		bool showWalls = IsActive && CurrentOwner != LaserOwner.Neutral;
		float uniformPlaceholderLength = GetUniformPlaceholderLength();

		if (!showWalls)
		{
			for (int i = 0; i < _laserRayCasts.Count; i++)
			{
				ApplyPlaceholderLength(i, uniformPlaceholderLength);
			}

			return;
		}

		int armCount = _laserRayCasts.Count;
		float[] reaches = new float[armCount];
		float sharedMaxReach = 0.0f;

		for (int i = 0; i < armCount; i++)
		{
			reaches[i] = MeasureWorldReach(i, maxLength);
			if (reaches[i] > sharedMaxReach)
			{
				sharedMaxReach = reaches[i];
			}
		}

		sharedMaxReach = Mathf.Max(sharedMaxReach, MinimumLaserLength);
		float sharedStartLength = Mathf.Min(uniformPlaceholderLength, sharedMaxReach);
		sharedStartLength = Mathf.Max(0.05f, sharedStartLength);

		float sharedExpandLength = Mathf.Lerp(
			sharedStartLength,
			sharedMaxReach,
			EaseOutCubic(_expandProgress)
		);
		float sharedRetractLength = Mathf.Lerp(
			sharedMaxReach,
			sharedStartLength,
			EaseInOutCubic(_retractProgress)
		);

		for (int i = 0; i < armCount; i++)
		{
			float fullReachLength = reaches[i];
			float wallLength;

			if (_isRetracting)
			{
				wallLength = Mathf.Min(fullReachLength, sharedRetractLength);
			}
			else if (_isExpanding)
			{
				wallLength = Mathf.Min(fullReachLength, sharedExpandLength);
			}
			else
			{
				wallLength = fullReachLength;
			}

			wallLength = Mathf.Clamp(wallLength, 0.02f, fullReachLength);
			ApplyCurrentLaserLength(i, wallLength, fullReachLength);
		}
	}

	private float GetMaxLockedReach(float fallback)
	{
		if (_lockedArmReach.Count == 0)
		{
			return fallback;
		}

		float max = 0.0f;
		foreach (float reach in _lockedArmReach)
		{
			if (reach > max)
			{
				max = reach;
			}
		}

		return Mathf.Max(fallback * 0.1f, max);
	}

	private float MeasureWorldReach(int armIndex, float maxLength)
	{
		if (armIndex < 0 || armIndex >= _laserRayCasts.Count)
		{
			return maxLength;
		}

		RayCast3D ray = _laserRayCasts[armIndex];
		ConfigureReachRay(ray);
		ray.TargetPosition = new Vector3(maxLength, 0.0f, 0.0f);
		ray.ForceRaycastUpdate();

		float fullReachLength = maxLength;
		if (ray.IsColliding() && IsWorldReachCollider(ray.GetCollider()))
		{
			fullReachLength =
				ray.GlobalPosition.DistanceTo(ray.GetCollisionPoint()) - WallDistanceOffset;
		}

		return Mathf.Clamp(fullReachLength, MinimumLaserLength, maxLength);
	}

	private float GetUniformPlaceholderLength()
	{
		float maxLength = Mathf.Max(0.5f, MaxLaserLength);
		if (EnableExpandOnActivate)
		{
			return Mathf.Max(0.35f, maxLength * GetExpandStartFactor());
		}

		return maxLength;
	}

	private void ApplyCurrentLaserLength(int armIndex, float length, float fullReachLength)
	{
		if (armIndex < 0 || armIndex >= _laserWalls.Count)
		{
			return;
		}

		float clampedLength = Mathf.Min(
			Mathf.Max(0.02f, length),
			Mathf.Max(0.02f, fullReachLength)
		);
		float height = Mathf.Max(0.5f, LaserWallHeight);
		float thickness = Mathf.Max(0.02f, LaserWallThickness);
		float hub = GetHubRadius();
		float safeReach = Mathf.Max(clampedLength, fullReachLength);
		float halfLength = clampedLength * 0.5f;

		bool anchorOutside =
			_isRetracting && RetractStyle == LaserRetractStyle.TowardOutside;

		float centerX = anchorOutside
			? hub + safeReach - halfLength
			: hub + halfLength;

		if (anchorOutside)
		{
			float outerEdge = centerX + halfLength;
			float maxOuter = hub + fullReachLength;
			if (outerEdge > maxOuter)
			{
				centerX -= outerEdge - maxOuter;
			}
		}
		else
		{
			float outerEdge = centerX + halfLength;
			float maxOuter = hub + fullReachLength;
			if (outerEdge > maxOuter)
			{
				clampedLength = Mathf.Max(0.02f, clampedLength - (outerEdge - maxOuter));
				halfLength = clampedLength * 0.5f;
				centerX = hub + halfLength;
			}
		}

		Vector3 size = new Vector3(clampedLength, height, thickness);
		float centerY = height * 0.5f - Mathf.Max(0.0f, WallGroundEmbed);
		Vector3 center = new Vector3(centerX, centerY, 0.0f);

		MeshInstance3D wall = _laserWalls[armIndex];
		if (wall.Mesh is BoxMesh boxMesh)
		{
			boxMesh.Size = size;
		}

		wall.Position = center;

		if (armIndex < _laserWallGlows.Count)
		{
			float glowThickness = thickness * Mathf.Max(1.2f, WallGlowThicknessScale);
			float glowHeight = height * Mathf.Max(1.0f, WallGlowHeightScale);
			float glowLength = Mathf.Max(0.02f, clampedLength * 0.98f);
			float glowHalf = glowLength * 0.5f;
			float glowCenterX = anchorOutside
				? hub + fullReachLength - glowHalf
				: hub + glowHalf;
			float glowCenterY = glowHeight * 0.5f - Mathf.Max(0.0f, WallGroundEmbed);
			Vector3 glowSize = new Vector3(glowLength, glowHeight, glowThickness);
			Vector3 glowCenter = new Vector3(glowCenterX, glowCenterY, 0.0f);

			MeshInstance3D glow = _laserWallGlows[armIndex];
			if (glow.Mesh is BoxMesh glowMesh)
			{
				glowMesh.Size = glowSize;
			}

			glow.Position = glowCenter;
		}

		if (!_hitboxesWantedEnabled)
		{
			return;
		}

		if (armIndex < _laserHitAreas.Count)
		{
			_laserHitAreas[armIndex].Position = center;
		}

		if (armIndex < _laserHitCollisions.Count
			&& _laserHitCollisions[armIndex].Shape is BoxShape3D boxShape)
		{
			boxShape.Size = size;
		}
	}

	private void ApplyPlaceholderLength(int armIndex, float length)
	{
		if (armIndex < 0 || armIndex >= _placeholderMeshes.Count)
		{
			return;
		}

		float clampedLength = Mathf.Max(MinimumLaserLength, length);
		float thickness = Mathf.Max(0.02f, LaserWallThickness);
		float width = Mathf.Max(thickness, StripeWidth);
		float start = GetHubRadius();
		float halfLength = clampedLength * 0.5f;
		const float stripeY = 0.03f;

		MeshInstance3D placeholder = _placeholderMeshes[armIndex];
		if (placeholder.Mesh is BoxMesh boxMesh)
		{
			boxMesh.Size = new Vector3(clampedLength, 0.025f, width);
		}

		placeholder.Position = new Vector3(start + halfLength, stripeY, 0.0f);
	}

	private void CreateMaterials()
	{
		_stripeMaterial = new StandardMaterial3D
		{
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			AlbedoColor = PlaceholderColor,
			Roughness = 1.0f,
			Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
			CullMode = BaseMaterial3D.CullModeEnum.Disabled
		};

		_wallMaterial = new StandardMaterial3D
		{
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			Roughness = 1.0f,
			Metallic = 0.0f,
			EmissionEnabled = true,
			EmissionEnergyMultiplier = EmissionEnergy,
			Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
			BlendMode = BaseMaterial3D.BlendModeEnum.Mix,
			CullMode = BaseMaterial3D.CullModeEnum.Disabled,
			AlbedoColor = new Color(1f, 1f, 1f, WallFillAlpha)
		};

		_wallGlowMaterial = new StandardMaterial3D
		{
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			Roughness = 1.0f,
			EmissionEnabled = true,
			EmissionEnergyMultiplier = WallGlowEnergy,
			Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
			BlendMode = BaseMaterial3D.BlendModeEnum.Add,
			CullMode = BaseMaterial3D.CullModeEnum.Disabled,
			DisableReceiveShadows = true,
			AlbedoColor = new Color(1f, 1f, 1f, WallGlowAlpha)
		};
	}

	private void ApplyHubLayout()
	{
		float radius = GetHubRadius();
		float rayHeight = Mathf.Max(0.5f, LaserWallHeight) * 0.5f;

		foreach (RayCast3D ray in _laserRayCasts)
		{
			ray.Position = new Vector3(radius, rayHeight, 0.0f);
		}

		if (ScaleCaptureAreaWithHub && _captureCollision?.Shape is CylinderShape3D captureShape)
		{
			captureShape.Radius = radius + Mathf.Max(0.1f, CaptureRadiusPadding);
		}
	}

	private void ApplyModeVisuals()
	{
		bool showWalls = IsActive && CurrentOwner != LaserOwner.Neutral;
		bool showPlaceholders = !showWalls
			&& !_isRetracting
			&& !_waitingPatternSwap
			&& (_placeholderFadeIn || !_isFadingPlaceholders);

		foreach (Node3D floorMarks in _floorMarkRoots)
		{
			floorMarks.Visible = showPlaceholders;
		}

		foreach (MeshInstance3D wall in _laserWalls)
		{
			wall.Visible = showWalls;
		}

		foreach (MeshInstance3D glow in _laserWallGlows)
		{
			glow.Visible = showWalls;
		}

		if (_stripeMaterial != null)
		{
			_stripeMaterial.AlbedoColor = new Color(
				PlaceholderColor.R,
				PlaceholderColor.G,
				PlaceholderColor.B,
				_placeholderAlpha
			);
		}

		ApplyWallFadeVisuals();
	}

	private void ApplyWallFadeVisuals()
	{
		Color ownerColor = CurrentOwner switch
		{
			LaserOwner.PlayerOne => PlayerOneColor,
			LaserOwner.PlayerTwo => PlayerTwoColor,
			_ => PlaceholderColor
		};

		float visibility = Mathf.Clamp(_wallAlpha, 0.0f, 1.0f);
		bool lit = visibility > 0.02f && CurrentOwner != LaserOwner.Neutral;

		if (_wallMaterial != null)
		{
			float fillAlpha = WallFillAlpha * visibility;
			_wallMaterial.AlbedoColor = new Color(ownerColor.R, ownerColor.G, ownerColor.B, fillAlpha);
			_wallMaterial.Emission = ownerColor;
			_wallMaterial.EmissionEnabled = lit;
			_wallMaterial.EmissionEnergyMultiplier = EmissionEnergy * visibility;
		}

		if (_wallGlowMaterial != null)
		{
			Color glowColor = ownerColor.Lightened(0.35f);
			float glowAlpha = WallGlowAlpha * visibility;
			_wallGlowMaterial.AlbedoColor = new Color(glowColor.R, glowColor.G, glowColor.B, glowAlpha);
			_wallGlowMaterial.Emission = glowColor;
			_wallGlowMaterial.EmissionEnabled = lit;
			_wallGlowMaterial.EmissionEnergyMultiplier = WallGlowEnergy * visibility;
		}
	}

	private void UpdateVisuals()
	{
		ApplyModeVisuals();
	}

	private LaserPatternConfig EnsurePatternConfig()
	{
		if (PatternConfig == null || PatternConfig.Count <= 0)
		{
			PatternConfig = LaserPatternConfig.CreateDefault();
		}

		return PatternConfig;
	}

	private void RebuildPattern()
	{
		ClearArms();

		float[] angles = EnsurePatternConfig().GetArmAnglesDegrees(PatternIndex);
		for (int i = 0; i < angles.Length; i++)
		{
			SpawnArm(i, angles[i]);
		}

		_isExpanding = false;
		_isRetracting = false;
		_lengthFactor = EnableExpandOnActivate
			? GetExpandStartFactor()
			: 1.0f;
		ApplyHubLayout();
		CaptureLockedArmReach();
		UpdateLaserLength();
		ApplyModeVisuals();
	}

	private void ClearArms()
	{
		foreach (Area3D hitArea in _laserHitAreas)
		{
			hitArea.BodyEntered -= OnLaserHitAreaBodyEntered;
		}

		_laserArms.Clear();
		_floorMarkRoots.Clear();
		_placeholderMeshes.Clear();
		_laserWalls.Clear();
		_laserWallGlows.Clear();
		_laserHitAreas.Clear();
		_laserHitCollisions.Clear();
		_laserRayCasts.Clear();
		_lockedArmReach.Clear();

		if (_laserPivot == null)
		{
			return;
		}

		while (_laserPivot.GetChildCount() > 0)
		{
			_laserPivot.GetChild(0).Free();
		}
	}

	private void SpawnArm(int index, float yawDegrees)
	{
		Node3D arm = new Node3D
		{
			Name = $"LaserArm{index}",
			RotationDegrees = new Vector3(0.0f, yawDegrees, 0.0f)
		};

		Node3D floorMarks = new Node3D { Name = "FloorMarks" };
		arm.AddChild(floorMarks);
		MeshInstance3D placeholder = CreatePlaceholderStripe();
		floorMarks.AddChild(placeholder);

		RayCast3D ray = CreateLaserRayCast();
		arm.AddChild(ray);

		MeshInstance3D wall = CreateLaserWall();
		arm.AddChild(wall);

		MeshInstance3D glow = CreateLaserWallGlow();
		arm.AddChild(glow);

		Area3D hitArea = new Area3D
		{
			Name = "LaserHitArea",
			CollisionLayer = 8,
			CollisionMask = 2,
			Monitoring = false,
			Monitorable = true
		};

		CollisionShape3D hitCollision = new CollisionShape3D
		{
			Name = "LaserHitCollision",
			Disabled = true,
			Shape = new BoxShape3D()
		};
		hitArea.AddChild(hitCollision);
		arm.AddChild(hitArea);

		_laserPivot.AddChild(arm);

		hitArea.BodyEntered += OnLaserHitAreaBodyEntered;

		_laserArms.Add(arm);
		_floorMarkRoots.Add(floorMarks);
		_placeholderMeshes.Add(placeholder);
		_laserRayCasts.Add(ray);
		_laserWalls.Add(wall);
		_laserWallGlows.Add(glow);
		_laserHitAreas.Add(hitArea);
		_laserHitCollisions.Add(hitCollision);
	}

	private RayCast3D CreateLaserRayCast()
	{
		float start = GetHubRadius();
		float rayHeight = Mathf.Max(0.5f, LaserWallHeight) * 0.5f;

		RayCast3D ray = new RayCast3D
		{
			Name = "LaserRayCast",
			Enabled = true,
			CollisionMask = WorldCollisionMask == 0 ? 1u : WorldCollisionMask,
			CollideWithAreas = false,
			CollideWithBodies = true,
			HitFromInside = false,
			Position = new Vector3(start, rayHeight, 0.0f),
			TargetPosition = new Vector3(Mathf.Max(0.5f, MaxLaserLength), 0.0f, 0.0f)
		};
		ConfigureReachRay(ray);
		return ray;
	}

	private void ConfigureReachRay(RayCast3D ray)
	{
		if (ray == null)
		{
			return;
		}

		ray.CollisionMask = WorldCollisionMask == 0 ? 1u : WorldCollisionMask;
		ray.CollideWithAreas = false;
		ray.CollideWithBodies = true;
		ray.HitFromInside = false;
		ray.ClearExceptions();
		AddPlayerExceptions(ray);
	}

	private void AddPlayerExceptions(RayCast3D ray)
	{
		SceneTree tree = GetTree();
		if (tree == null)
		{
			return;
		}

		AddPlayerExceptionsRecursive(ray, tree.Root);
	}

	private void AddPlayerExceptionsRecursive(RayCast3D ray, Node node)
	{
		if (node is ILaserPlayer && node is CollisionObject3D body)
		{
			ray.AddException(body);
		}

		foreach (Node child in node.GetChildren())
		{
			AddPlayerExceptionsRecursive(ray, child);
		}
	}

	private bool IsWorldReachCollider(GodotObject collider)
	{
		if (collider == null)
		{
			return false;
		}

		if (collider is ILaserPlayer)
		{
			return false;
		}

		if (collider is Node node)
		{
			Node current = node;
			while (current != null)
			{
				if (current is ILaserPlayer || current is CharacterBody3D)
				{
					return false;
				}

				current = current.GetParent();
			}
		}

		return true;
	}

	private MeshInstance3D CreatePlaceholderStripe()
	{
		float thickness = Mathf.Max(0.02f, LaserWallThickness);
		float width = Mathf.Max(thickness, StripeWidth);
		float length = Mathf.Max(MinimumLaserLength, MaxLaserLength);
		float start = GetHubRadius();

		return new MeshInstance3D
		{
			Name = "PlaceholderStripe",
			Mesh = new BoxMesh
			{
				Size = new Vector3(length, 0.025f, width)
			},
			MaterialOverride = _stripeMaterial,
			Position = new Vector3(start + length * 0.5f, 0.03f, 0.0f)
		};
	}

	private MeshInstance3D CreateLaserWall()
	{
		float height = Mathf.Max(0.5f, LaserWallHeight);
		float thickness = Mathf.Max(0.02f, LaserWallThickness);
		float startFactor = EnableExpandOnActivate
			? GetExpandStartFactor()
			: 1.0f;
		float length = Mathf.Max(MinimumLaserLength, MaxLaserLength * startFactor);
		float start = GetHubRadius();
		float centerY = height * 0.5f - Mathf.Max(0.0f, WallGroundEmbed);

		return new MeshInstance3D
		{
			Name = "LaserWall",
			Visible = false,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			Mesh = new BoxMesh
			{
				Size = new Vector3(length, height, thickness)
			},
			MaterialOverride = _wallMaterial,
			Position = new Vector3(start + length * 0.5f, centerY, 0.0f)
		};
	}

	private MeshInstance3D CreateLaserWallGlow()
	{
		float height = Mathf.Max(0.5f, LaserWallHeight) * Mathf.Max(1.0f, WallGlowHeightScale);
		float thickness = Mathf.Max(0.02f, LaserWallThickness) * Mathf.Max(1.2f, WallGlowThicknessScale);
		float startFactor = EnableExpandOnActivate
			? GetExpandStartFactor()
			: 1.0f;
		float length = Mathf.Max(MinimumLaserLength, MaxLaserLength * startFactor);
		float start = GetHubRadius();
		float centerY = height * 0.5f - Mathf.Max(0.0f, WallGroundEmbed);

		return new MeshInstance3D
		{
			Name = "LaserWallGlow",
			Visible = false,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			Mesh = new BoxMesh
			{
				Size = new Vector3(length, height, thickness)
			},
			MaterialOverride = _wallGlowMaterial,
			Position = new Vector3(start + length * 0.5f, centerY, 0.0f)
		};
	}

	private void SetLaserHitboxesEnabled(bool enabled)
	{
		_hitboxesWantedEnabled = enabled;
		CallDeferred(MethodName.ApplyHitboxesEnabledState);
	}

	private void ApplyHitboxesEnabledState()
	{
		foreach (Area3D hitArea in _laserHitAreas)
		{
			hitArea.Monitoring = _hitboxesWantedEnabled;
		}

		foreach (CollisionShape3D collision in _laserHitCollisions)
		{
			collision.Disabled = !_hitboxesWantedEnabled;
		}
	}

	private void EnsureActiveWallsReady()
	{
		if (!_nodesReady || !IsActive || CurrentOwner == LaserOwner.Neutral)
		{
			return;
		}

		ApplyHitboxesEnabledState();
		ApplyModeVisuals();
		UpdateLaserLength();
		ProcessLaserOverlaps();
	}

	private bool ResolveCoreNodes()
	{
		_laserPivot = GetNodeOrNull<Node3D>("LaserPivot");
		_captureArea = GetNodeOrNull<Area3D>("CaptureArea");
		_captureCollision = GetNodeOrNull<CollisionShape3D>("CaptureArea/CaptureCollision");
		_activationParticles = GetNodeOrNull<GpuParticles3D>("ActivationParticles");
		_activationAudio = GetNodeOrNull<AudioStreamPlayer3D>("ActivationAudio");
		_captureCooldownTimer = GetNodeOrNull<Timer>("CaptureCooldownTimer");

		bool ok = true;

		if (_laserPivot == null)
		{
			GD.PushError("LaserStation: LaserPivot node was not found.");
			ok = false;
		}

		if (_captureArea == null)
		{
			GD.PushError("LaserStation: CaptureArea node was not found.");
			ok = false;
		}

		if (_captureCooldownTimer == null)
		{
			GD.PushError("LaserStation: CaptureCooldownTimer node was not found.");
			ok = false;
		}

		return ok;
	}

	private void ConnectCoreSignals()
	{
		if (_captureArea != null)
		{
			_captureArea.BodyEntered += OnCaptureAreaBodyEntered;
		}

		if (_captureCooldownTimer != null)
		{
			_captureCooldownTimer.Timeout += OnCaptureCooldownTimeout;
		}
	}

	private void ConfigureCaptureTimer()
	{
		if (_captureCooldownTimer == null)
		{
			return;
		}

		_captureCooldownTimer.OneShot = true;
		_captureCooldownTimer.Autostart = false;
		_captureCooldownTimer.WaitTime = Mathf.Max(
			0.05f,
			Mathf.Max(CaptureCooldownSeconds, CaptureLockoutSeconds)
		);
	}

	private void EnsureLifetimeTimer()
	{
		if (_lifetimeTimer != null)
		{
			return;
		}

		_lifetimeTimer = new Timer
		{
			Name = "LifetimeTimer",
			OneShot = true,
			Autostart = false
		};
		AddChild(_lifetimeTimer);
		_lifetimeTimer.Timeout += OnLifetimeTimeout;
	}

	private void StartLifetime()
	{
		StopLifetime();

		if (!EnableLifetime)
		{
			CurrentLifetimeSeconds = 0.0f;
			return;
		}

		EnsureLifetimeTimer();

		float min = Mathf.Max(0.1f, Mathf.Min(LifetimeMinSeconds, LifetimeMaxSeconds));
		float max = Mathf.Max(0.1f, Mathf.Max(LifetimeMinSeconds, LifetimeMaxSeconds));
		CurrentLifetimeSeconds = RandomizeLifetime
			? (float)GD.RandRange(min, max)
			: max;

		_lifetimeTimer.Start(CurrentLifetimeSeconds);
	}

	private void StopLifetime()
	{
		if (_lifetimeTimer != null && !_lifetimeTimer.IsStopped())
		{
			_lifetimeTimer.Stop();
		}
	}

	private void StartExpiredPatternTransition()
	{
		CurrentOwner = LaserOwner.Neutral;
		IsActive = false;
		_captureLocked = false;
		_playerHitCooldowns.Clear();
		StopLifetime();
		CurrentLifetimeSeconds = 0.0f;
		_isExpanding = false;
		_isRetracting = false;
		_lengthFactor = EnableExpandOnActivate
			? GetExpandStartFactor()
			: 1.0f;

		if (_captureCooldownTimer != null && !_captureCooldownTimer.IsStopped())
		{
			_captureCooldownTimer.Stop();
		}

		_placeholderAlpha = 0.0f;
		_fadeProgress = 0.0f;
		_patternSwapWait = 0.0f;
		_placeholderFadeIn = false;
		_waitingPatternSwap = true;
		_isFadingPlaceholders = true;
		ApplyPlaceholderAlpha();

		SetLaserHitboxesEnabled(false);
		UpdateLaserLength();
		ApplyModeVisuals();
	}

	private void BeginPlaceholderFadeIn()
	{
		_isFadingPlaceholders = true;
		_placeholderFadeIn = true;
		_waitingPatternSwap = false;
		_fadeProgress = 0.0f;
		_patternSwapWait = 0.0f;
		_placeholderAlpha = 0.0f;
		ApplyPlaceholderAlpha();
		ApplyModeVisuals();
	}

	private void ClearPlaceholderFade()
	{
		_isFadingPlaceholders = false;
		_placeholderFadeIn = false;
		_waitingPatternSwap = false;
		_fadeProgress = 0.0f;
		_patternSwapWait = 0.0f;
		_placeholderAlpha = 1.0f;
		ApplyPlaceholderAlpha();
	}

	private void UpdatePlaceholderFade(double delta)
	{
		if (!_isFadingPlaceholders)
		{
			return;
		}

		if (_waitingPatternSwap)
		{
			_patternSwapWait += (float)delta;
			if (_patternSwapWait >= Mathf.Max(0.0f, PatternSwapDelaySeconds))
			{
				_waitingPatternSwap = false;
				SwitchToNextExpiredPattern();
			}

			return;
		}

		if (!_placeholderFadeIn)
		{
			return;
		}

		float duration = Mathf.Max(0.05f, PatternFadeInSeconds);
		_fadeProgress = Mathf.MoveToward(_fadeProgress, 1.0f, (float)delta / duration);
		_placeholderAlpha = EaseInOutCubic(_fadeProgress);
		ApplyPlaceholderAlpha();

		if (_fadeProgress >= 0.999f)
		{
			_placeholderAlpha = 1.0f;
			_isFadingPlaceholders = false;
			_placeholderFadeIn = false;
			_fadeProgress = 1.0f;
			ApplyPlaceholderAlpha();
			CaptureLockedArmReach();
			UpdateLaserLength();
			ApplyModeVisuals();
			TryCaptureOverlappingPlayers();
		}
	}

	private static float EaseInOutCubic(float t)
	{
		t = Mathf.Clamp(t, 0.0f, 1.0f);
		if (t < 0.5f)
		{
			return 4.0f * t * t * t;
		}

		float f = -2.0f * t + 2.0f;
		return 1.0f - f * f * f * 0.5f;
	}

	private static float EaseOutCubic(float t)
	{
		t = Mathf.Clamp(t, 0.0f, 1.0f);
		float inv = 1.0f - t;
		return 1.0f - inv * inv * inv;
	}

	private void SwitchToNextExpiredPattern()
	{
		LaserPatternConfig config = EnsurePatternConfig();
		RandomNumberGenerator rng = new();
		rng.Randomize();

		if (RandomizePatternOnExpire && config.Count > 0)
		{
			int nextIndex = config.RandomIndex(rng);
			if (config.Count > 1)
			{
				int guard = 0;
				while (nextIndex == PatternIndex && guard < 8)
				{
					nextIndex = config.RandomIndex(rng);
					guard++;
				}
			}

			PatternIndex = nextIndex;
		}
		else
		{
			PatternIndex = config.NextIndex(PatternIndex);
		}

		if (RandomizeHubRadius)
		{
			float min = Mathf.Max(0.05f, Mathf.Min(RandomHubRadiusMin, RandomHubRadiusMax));
			float max = Mathf.Max(0.05f, Mathf.Max(RandomHubRadiusMin, RandomHubRadiusMax));
			HubRadius = rng.RandfRange(min, max);
		}

		if (RandomizeExpandSpeed)
		{
			float min = Mathf.Min(RandomExpandSpeedMin, RandomExpandSpeedMax);
			float max = Mathf.Max(RandomExpandSpeedMin, RandomExpandSpeedMax);
			ExpandSpeed = rng.RandfRange(min, max);
		}

		if (RandomizeRetractSpeed)
		{
			float min = Mathf.Min(RandomRetractSpeedMin, RandomRetractSpeedMax);
			float max = Mathf.Max(RandomRetractSpeedMin, RandomRetractSpeedMax);
			RetractSpeed = rng.RandfRange(min, max);
		}

		if (RandomizeExpandStartFactor)
		{
			float min = Mathf.Clamp(
				Mathf.Min(RandomExpandStartFactorMin, RandomExpandStartFactorMax),
				0.01f,
				1.0f
			);
			float max = Mathf.Clamp(
				Mathf.Max(RandomExpandStartFactorMin, RandomExpandStartFactorMax),
				0.01f,
				1.0f
			);
			ExpandStartFactor = rng.RandfRange(min, max);
		}

		if (RandomizeRetractStyle)
		{
			RetractStyle = rng.Randf() <= Mathf.Clamp(RandomRetractTowardOutsideChance, 0.0f, 1.0f)
				? LaserRetractStyle.TowardOutside
				: LaserRetractStyle.TowardCenter;
		}

		RebuildPattern();
		BeginPlaceholderFadeIn();
	}

	private void ApplyPlaceholderAlpha()
	{
		if (_stripeMaterial == null)
		{
			return;
		}

		_stripeMaterial.AlbedoColor = new Color(
			PlaceholderColor.R,
			PlaceholderColor.G,
			PlaceholderColor.B,
			_placeholderAlpha
		);
	}

	private void OnLifetimeTimeout()
	{
		BeginRetract();
	}
}
