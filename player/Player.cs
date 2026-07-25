using Godot;

public partial class Player : CharacterBody3D, ILaserPlayer
{
	[Signal]
	public delegate void PlayerDiedEventHandler(int victimPlayerId, int attackingPlayerId);

	[Export] private AnimationPlayer _animationPlayer;
	[Export] private Label3D _lockedTimeLabel;
	[Export] private CpuParticles3D _particlesSkin;
	[Export] private CpuParticles3D _particlesShoes;
	[Export] private Node3D _meshRoot;
	[Export] private MeshInstance3D _indicatorRing;
	[Export] public int PlayerId { get; set; } = 1;

	[Export] public Color IndicatorColor { get; set; } = new Color(0.1f, 0.4f, 1.0f);
	[Export] public float IndicatorRingSize { get; set; } = 1.35f;
	[Export] public float WalkSpeed { get; set; } = 8.0f;
	[Export] public float Acceleration { get; set; } = 40.0f;
	[Export] public float Friction { get; set; } = 30.0f;
	[Export] public float DashSpeed { get; set; } = 24.0f;
	[Export] public float DashDuration { get; set; } = 0.2f;
	[Export] public float DashAnimationDuration { get; set; } = 1f;
	[Export] public float DashCooldown { get; set; } = 1.5f;
	[Export] public float RespawnDelaySeconds { get; set; } = 1.5f;
	[Export] public float SpawnProtectionSeconds { get; set; } = 1.0f;
	[Export] public float Gravity { get; set; } = 20.0f;
	[Export] public float LockedTimeLabelHeight { get; set; } = 1.35f;
	[Export] public float LockedTimeLabelNorthOffset { get; set; } = 0.45f;
	[Export] public float DefaultStunSeconds { get; set; } = 2.5f;

	public bool IsInvulnerable { get; private set; }
	public bool IsAlive { get; private set; } = true;
	public bool IsStunned => _stunTimeLeft > 0.0f;
	public Label3D LockedTimeLabel => _lockedTimeLabel;

	private float _dashTimeLeft;
	private float _dashCooldownLeft;
	private float _dashAnimationDurationLeft;
	private Vector3 _dashDirection = Vector3.Forward;
	private string _currentAnim = string.Empty;
	private bool _isDashing;
	private bool _dashGrantsInvulnerability;
	private float _respawnTimeLeft;
	private float _invulnTimeLeft;
	private float _stunTimeLeft;
	private Vector3 _spawnPosition;
	private ShaderMaterial _indicatorRingMaterial;

	public override void _Ready()
	{
		CollisionLayer = 2;
		CollisionMask = 1;
		AddToGroup("laser_players");
		_spawnPosition = GlobalPosition;
		ApplyColorFromStoreOrDefault();
		SetupIndicatorRing();

		if (_lockedTimeLabel != null)
		{
			_lockedTimeLabel.TopLevel = true;
			_lockedTimeLabel.Billboard = BaseMaterial3D.BillboardModeEnum.Enabled;
			_lockedTimeLabel.Visible = false;
		}
	}

	public override void _Process(double delta)
	{
		UpdateLockedTimeLabelTransform();
	}

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;
		UpdateStatusTimers(dt);

		if (!IsAlive || IsStunned)
		{
			ApplyGravity(dt);
			Velocity = new Vector3(0.0f, Velocity.Y, 0.0f);
			MoveAndSlide();
			UpdateAnimation(Vector2.Zero);
			return;
		}

		HandleMovement(dt);
	}

	public void HitByLaser(int attackingPlayerId)
	{
		if (!IsAlive || IsInvulnerable)
		{
			return;
		}

		_particlesSkin.Emitting = true;
		_particlesShoes.Emitting = true;
		_meshRoot.Visible = false;
		SetIndicatorRingVisible(false);
		IsAlive = false;
		_dashTimeLeft = 0.0f;
		_dashGrantsInvulnerability = false;
		_stunTimeLeft = 0.0f;
		_respawnTimeLeft = RespawnDelaySeconds;
		Velocity = Vector3.Zero;
		GD.Print($"Player {PlayerId} hit by laser of player {attackingPlayerId}");
		EmitSignal(SignalName.PlayerDied, PlayerId, attackingPlayerId);
	}

	public void ApplyStun(float durationSeconds = -1.0f)
	{
		if (!IsAlive)
		{
			return;
		}

		float duration = durationSeconds > 0.0f ? durationSeconds : DefaultStunSeconds;
		_stunTimeLeft = Mathf.Max(_stunTimeLeft, duration);
		_dashTimeLeft = 0.0f;
		_isDashing = false;
		if (_dashGrantsInvulnerability && _invulnTimeLeft <= 0.0f)
		{
			IsInvulnerable = false;
		}

		_dashGrantsInvulnerability = false;
		Velocity = new Vector3(0.0f, Velocity.Y, 0.0f);
	}

	public void StartVulnerableDash(Vector3 direction = default)
	{
		if (!IsAlive || IsStunned)
		{
			return;
		}

		StartDash(ResolveDashDirection(direction), grantInvulnerability: false, ignoreCooldown: true);
	}

	public void StartInvulnerableDash(Vector3 direction = default)
	{
		if (!IsAlive || IsStunned)
		{
			return;
		}

		StartDash(ResolveDashDirection(direction), grantInvulnerability: true, ignoreCooldown: true);
	}

	private void UpdateLockedTimeLabelTransform()
	{
		if (_lockedTimeLabel == null)
		{
			return;
		}

		_lockedTimeLabel.GlobalPosition = GlobalPosition
			+ Vector3.Up * LockedTimeLabelHeight
			+ Vector3.Forward * LockedTimeLabelNorthOffset;
	}

	private void UpdateStatusTimers(float delta)
	{
		if (_invulnTimeLeft > 0.0f)
		{
			_invulnTimeLeft -= delta;
			if (_invulnTimeLeft <= 0.0f)
			{
				_invulnTimeLeft = 0.0f;
				IsInvulnerable = false;
			}
		}

		if (_stunTimeLeft > 0.0f)
		{
			_stunTimeLeft = Mathf.Max(0.0f, _stunTimeLeft - delta);
		}

		if (!IsAlive)
		{
			_respawnTimeLeft -= delta;
			if (_respawnTimeLeft <= 0.0f)
			{
				Respawn();
			}
		}
	}

	private void Respawn()
	{
		GlobalPosition = _spawnPosition;
		Velocity = Vector3.Zero;
		_meshRoot.Visible = true;
		SetIndicatorRingVisible(true);
		IsAlive = true;
		IsInvulnerable = true;
		_invulnTimeLeft = SpawnProtectionSeconds;
		_dashTimeLeft = 0.0f;
		_dashCooldownLeft = 0.0f;
		_stunTimeLeft = 0.0f;
	}

	public void SetIndicatorColor(Color color)
	{
		IndicatorColor = color;
		ApplyIndicatorRingColor();
	}

	private void ApplyColorFromStoreOrDefault()
	{
		if (GameStore.Instance != null)
		{
			IndicatorColor = GameStore.Instance.GetPlayerColor(PlayerId);
			return;
		}

		if (PlayerColorStore.Instance != null)
		{
			IndicatorColor = PlayerColorStore.Instance.GetColor(PlayerId);
			return;
		}

		IndicatorColor = PlayerId switch
		{
			2 => GameStore.Red,
			_ => GameStore.Blue
		};
	}

	private void SetupIndicatorRing()
	{
		if (_indicatorRing == null)
		{
			return;
		}

		Shader shader = GD.Load<Shader>("res://resources/player/indicator_ring.gdshader");
		_indicatorRingMaterial = new ShaderMaterial
		{
			Shader = shader
		};
		_indicatorRing.MaterialOverride = _indicatorRingMaterial;
		_indicatorRing.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;

		if (_indicatorRing.Mesh is PlaneMesh planeMesh)
		{
			float size = Mathf.Max(0.4f, IndicatorRingSize);
			planeMesh.Size = new Vector2(size, size);
		}

		ApplyIndicatorRingColor();
		SetIndicatorRingVisible(true);
	}

	private void ApplyIndicatorRingColor()
	{
		if (_indicatorRingMaterial == null)
		{
			return;
		}

		_indicatorRingMaterial.SetShaderParameter("ring_color", IndicatorColor);
	}

	private void SetIndicatorRingVisible(bool visible)
	{
		if (_indicatorRing == null)
		{
			return;
		}

		_indicatorRing.Visible = visible;
	}

	private void HandleMovement(float delta)
	{
		Vector2 input = Input.GetVector(
			"MoveLeftPlayer" + PlayerId,
			"MoveRightPlayer" + PlayerId,
			"MoveUpPlayer" + PlayerId,
			"MoveDownPlayer" + PlayerId
		);

		if (input != Vector2.Zero)
		{
			input = input.Normalized();
		}

		UpdateDashTimers(delta);
		TryStartDash(input);
		ApplyGravity(delta);

		Vector3 horizontal;
		if (_dashTimeLeft > 0.0f)
		{
			horizontal = _dashDirection * DashSpeed;
		}
		else
		{
			horizontal = ApplyWalkVelocity(input, delta);
		}

		Velocity = new Vector3(horizontal.X, Velocity.Y, horizontal.Z);
		MoveAndSlide();

		UpdateFacing(input);
		UpdateAnimation(input);
	}

	private void ApplyGravity(float delta)
	{
		if (!IsOnFloor())
		{
			Velocity = new Vector3(Velocity.X, Velocity.Y - Gravity * delta, Velocity.Z);
		}
		else if (Velocity.Y < 0.0f)
		{
			Velocity = new Vector3(Velocity.X, 0.0f, Velocity.Z);
		}
	}

	private void UpdateDashTimers(float delta)
	{
		if (_dashTimeLeft > 0.0f)
		{
			_dashTimeLeft -= delta;
			if (_dashTimeLeft <= 0.0f)
			{
				_dashTimeLeft = 0.0f;
				_dashCooldownLeft = DashCooldown;
				_isDashing = false;
			}
		}
		else if (_dashCooldownLeft > 0.0f)
		{
			_dashCooldownLeft = Mathf.Max(0.0f, _dashCooldownLeft - delta);
		}

		if (_dashAnimationDurationLeft > 0.0f)
		{
			_dashAnimationDurationLeft -= delta;
			if (_dashAnimationDurationLeft <= 0.0f)
			{
				_dashAnimationDurationLeft = 0.0f;
				if (_dashGrantsInvulnerability)
				{
					_dashGrantsInvulnerability = false;
					if (_invulnTimeLeft <= 0.0f)
					{
						IsInvulnerable = false;
					}
				}
			}
		}
	}

	private void TryStartDash(Vector2 input)
	{
		if (_dashCooldownLeft > 0.0f || _dashTimeLeft > 0.0f)
		{
			return;
		}

		if (!Input.IsActionJustPressed("ActionPlayer" + PlayerId))
		{
			return;
		}

		Vector3 direction;
		if (input != Vector2.Zero)
		{
			direction = new Vector3(input.X, 0.0f, input.Y).Normalized();
		}
		else
		{
			direction = ResolveFacingDirection();
		}

		StartDash(direction, grantInvulnerability: true, ignoreCooldown: false);
	}

	private void StartDash(Vector3 direction, bool grantInvulnerability, bool ignoreCooldown)
	{
		if (!ignoreCooldown && (_dashCooldownLeft > 0.0f || _dashTimeLeft > 0.0f))
		{
			return;
		}

		if (direction.LengthSquared() < 0.0001f)
		{
			direction = ResolveFacingDirection();
		}
		else
		{
			direction.Y = 0.0f;
			if (direction.LengthSquared() < 0.0001f)
			{
				direction = ResolveFacingDirection();
			}
			else
			{
				direction = direction.Normalized();
			}
		}

		if (_dashGrantsInvulnerability && !grantInvulnerability && _invulnTimeLeft <= 0.0f)
		{
			IsInvulnerable = false;
		}

		_dashDirection = direction;
		_dashTimeLeft = DashDuration;
		_dashAnimationDurationLeft = DashAnimationDuration;
		_isDashing = true;
		_dashGrantsInvulnerability = grantInvulnerability;

		if (grantInvulnerability)
		{
			IsInvulnerable = true;
		}
	}

	private Vector3 ResolveDashDirection(Vector3 direction)
	{
		if (direction == default || direction.LengthSquared() < 0.0001f)
		{
			return ResolveFacingDirection();
		}

		direction.Y = 0.0f;
		return direction.LengthSquared() < 0.0001f
			? ResolveFacingDirection()
			: direction.Normalized();
	}

	private Vector3 ResolveFacingDirection()
	{
		Vector3 direction = GlobalTransform.Basis.Z;
		direction.Y = 0.0f;
		if (direction.LengthSquared() < 0.0001f)
		{
			return Vector3.Forward;
		}

		return direction.Normalized();
	}

	private Vector3 ApplyWalkVelocity(Vector2 input, float delta)
	{
		Vector3 current = new Vector3(Velocity.X, 0.0f, Velocity.Z);
		Vector3 target = input != Vector2.Zero
			? new Vector3(input.X, 0.0f, input.Y) * WalkSpeed
			: Vector3.Zero;

		float rate = input != Vector2.Zero ? Acceleration : Friction;
		return current.MoveToward(target, rate * delta);
	}

	private void UpdateFacing(Vector2 input)
	{
		if (input == Vector2.Zero)
		{
			return;
		}

		Vector2 direction = input.Normalized();
		Vector3 direction3 = new Vector3(direction.X, 0.0f, direction.Y);
		LookAt(Transform.Origin - direction3);
	}

	private void UpdateAnimation(Vector2 input)
	{
		if (_animationPlayer == null)
		{
			return;
		}

		string next;
		if (_dashAnimationDurationLeft > 0f)
		{
			next = "dash";
		}
		else
		{
			next = input != Vector2.Zero ? "walk" : "idle";
		}

		if (next == _currentAnim)
		{
			return;
		}

		_currentAnim = next;
		_animationPlayer.Play(next);
	}
}
