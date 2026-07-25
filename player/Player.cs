using Godot;

public partial class Player : CharacterBody3D, ILaserPlayer
{
	[Export] private AnimationPlayer _animationPlayer;
	[Export] public int PlayerId { get; set; } = 1;

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

	public bool IsInvulnerable { get; private set; }
	public bool IsAlive { get; private set; } = true;

	private float _dashTimeLeft;
	private float _dashCooldownLeft;
	private float _dashAnimationDurationLeft;
	private Vector3 _dashDirection = Vector3.Forward;
	private string _currentAnim = string.Empty;
	private bool _isDashing;
	private float _respawnTimeLeft;
	private float _invulnTimeLeft;
	private Vector3 _spawnPosition;

	public override void _Ready()
	{
		CollisionLayer = 2;
		CollisionMask = 1;
		AddToGroup("laser_players");
		_spawnPosition = GlobalPosition;
	}

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;
		UpdateStatusTimers(dt);

		if (!IsAlive)
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

		IsAlive = false;
		_dashTimeLeft = 0.0f;
		_respawnTimeLeft = RespawnDelaySeconds;
		Velocity = Vector3.Zero;
		GD.Print($"Player {PlayerId} hit by laser of player {attackingPlayerId}");
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
		IsAlive = true;
		IsInvulnerable = true;
		_invulnTimeLeft = SpawnProtectionSeconds;
		_dashTimeLeft = 0.0f;
		_dashCooldownLeft = 0.0f;
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
				IsInvulnerable = false;
				_dashAnimationDurationLeft = 0.0f;
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
			direction = GlobalTransform.Basis.Z;
			direction.Y = 0.0f;
			direction = direction.Normalized();
		}

		_dashDirection = direction;
		_dashTimeLeft = DashDuration;
		_dashAnimationDurationLeft = DashAnimationDuration;
		_isDashing = true;
		IsInvulnerable = true;
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
