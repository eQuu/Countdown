using System.Collections.Generic;
using Godot;

public partial class CameraTourController : Node3D
{
	[Signal]
	public delegate void WaypointReachedEventHandler(int waypointIndex);

	[Signal]
	public delegate void TourCompletedEventHandler();

	[Signal]
	public delegate void LookAtChangedEventHandler(Node3D lookAtTarget);

	[ExportCategory("References")]
	[Export] public Camera3D Camera { get; set; }
	[Export] public Node3D WaypointRoot { get; set; }
	[Export] public Node3D DefaultLookAt { get; set; }

	[ExportCategory("Movement")]
	[Export] public float MoveSpeed { get; set; } = 0.5f;
	[Export] public float MoveSmooth { get; set; } = 20.0f;
	[Export] public float LookSmooth { get; set; } = 20.0f;
	[Export] public float ArriveThreshold { get; set; } = 0.4f;
	[Export] public bool UseConstantSpeed { get; set; } = true;

	[ExportCategory("Tour")]
	[Export] public bool AutoStart { get; set; } = true;
	[Export] public bool Loop { get; set; } = true;
	[Export] public bool MakeCurrentOnStart { get; set; } = true;
	[Export] public bool SnapToFirstWaypointOnStart { get; set; } = true;

	public bool IsPlaying { get; private set; }
	public int CurrentWaypointIndex { get; private set; } = -1;
	public Node3D CurrentLookAt { get; private set; }

	private readonly List<Node3D> _waypoints = new();
	private float _waitTimer;
	private bool _waiting;

	public override void _Ready()
	{
		CallDeferred(MethodName.InitializeTour);
	}

	public override void _Process(double delta)
	{
		if (!IsPlaying || Camera == null)
		{
			return;
		}

		float dt = (float)delta;
		UpdateLookAt(dt);

		if (_waiting)
		{
			_waitTimer -= dt;
			if (_waitTimer > 0.0f)
			{
				return;
			}

			_waiting = false;
			AdvanceToNextWaypoint();
			return;
		}

		UpdateMovement(dt);
	}

	public void StartTour()
	{
		RebuildWaypointList();
		if (_waypoints.Count == 0 || Camera == null)
		{
			GD.PushWarning("CameraTourController: Missing camera or waypoints.");
			IsPlaying = false;
			return;
		}

		if (MakeCurrentOnStart)
		{
			Camera.MakeCurrent();
		}

		CurrentLookAt = DefaultLookAt;
		CurrentWaypointIndex = 0;
		_waiting = false;
		_waitTimer = 0.0f;
		IsPlaying = true;

		if (SnapToFirstWaypointOnStart)
		{
			Camera.GlobalPosition = _waypoints[0].GlobalPosition;
			ApplyWaypointLookAt(_waypoints[0], force: true);
			EmitSignal(SignalName.WaypointReached, 0);

			CameraTourWaypoint first = AsWaypoint(_waypoints[0]);
			float wait = first?.ArriveWaitSeconds ?? 0.0f;
			if (wait > 0.0f)
			{
				_waiting = true;
				_waitTimer = wait;
			}
			else if (_waypoints.Count > 1)
			{
				CurrentWaypointIndex = 1;
			}
		}
	}

	public void StopTour()
	{
		IsPlaying = false;
		_waiting = false;
	}

	public void RestartTour()
	{
		StopTour();
		StartTour();
	}

	public void SetLookAt(Node3D target)
	{
		if (target == null || target == CurrentLookAt)
		{
			return;
		}

		CurrentLookAt = target;
		EmitSignal(SignalName.LookAtChanged, target);
	}

	private void InitializeTour()
	{
		ResolveReferences();
		RebuildWaypointList();

		if (AutoStart)
		{
			StartTour();
		}
	}

	private void ResolveReferences()
	{
		Camera ??= GetNodeOrNull<Camera3D>("../MainCamera") ?? GetNodeOrNull<Camera3D>("MainCamera");
		WaypointRoot ??= GetNodeOrNull<Node3D>("Waypoints");
		DefaultLookAt ??= GetNodeOrNull<Node3D>("LookTargets/Default")
			?? GetNodeOrNull<Node3D>("../LookAtDefault");
	}

	private void RebuildWaypointList()
	{
		_waypoints.Clear();
		if (WaypointRoot == null)
		{
			return;
		}

		foreach (Node child in WaypointRoot.GetChildren())
		{
			if (child is Node3D marker)
			{
				_waypoints.Add(marker);
			}
		}
	}

	private void UpdateMovement(float delta)
	{
		if (CurrentWaypointIndex < 0 || CurrentWaypointIndex >= _waypoints.Count)
		{
			return;
		}

		Node3D target = _waypoints[CurrentWaypointIndex];
		float speed = ResolveMoveSpeed(target);
		float threshold = ResolveArriveThreshold(target);

		Vector3 current = Camera.GlobalPosition;
		Vector3 destination = target.GlobalPosition;
		float distance = current.DistanceTo(destination);

		if (distance <= threshold)
		{
			OnArrivedAtWaypoint(CurrentWaypointIndex, target);
			return;
		}

		if (UseConstantSpeed)
		{
			Camera.GlobalPosition = current.MoveToward(destination, speed * delta);
		}
		else
		{
			float t = 1.0f - Mathf.Exp(-Mathf.Max(0.01f, MoveSmooth) * delta);
			Camera.GlobalPosition = current.Lerp(destination, t);
		}
	}

	private void UpdateLookAt(float delta)
	{
		if (CurrentLookAt == null || !GodotObject.IsInstanceValid(CurrentLookAt))
		{
			return;
		}

		Vector3 from = Camera.GlobalPosition;
		Vector3 to = CurrentLookAt.GlobalPosition;
		if (from.DistanceSquaredTo(to) < 0.0001f)
		{
			return;
		}

		Vector3 direction = (to - from).Normalized();
		if (Mathf.Abs(direction.Dot(Vector3.Up)) > 0.98f)
		{
			return;
		}

		Basis targetBasis = Basis.LookingAt(direction, Vector3.Up);
		float t = 1.0f - Mathf.Exp(-Mathf.Max(0.01f, LookSmooth) * delta);
		Camera.GlobalBasis = Camera.GlobalBasis.Slerp(targetBasis, t).Orthonormalized();
	}

	private void OnArrivedAtWaypoint(int index, Node3D waypoint)
	{
		EmitSignal(SignalName.WaypointReached, index);
		ApplyWaypointLookAt(waypoint, force: false);

		CameraTourWaypoint typed = AsWaypoint(waypoint);
		float wait = typed?.ArriveWaitSeconds ?? 0.0f;
		if (wait > 0.0f)
		{
			_waiting = true;
			_waitTimer = wait;
			return;
		}

		AdvanceToNextWaypoint();
	}

	private void AdvanceToNextWaypoint()
	{
		if (_waypoints.Count == 0)
		{
			IsPlaying = false;
			return;
		}

		int next = CurrentWaypointIndex + 1;
		if (next >= _waypoints.Count)
		{
			EmitSignal(SignalName.TourCompleted);
			if (!Loop)
			{
				IsPlaying = false;
				return;
			}

			next = 0;
		}

		CurrentWaypointIndex = next;
	}

	private void ApplyWaypointLookAt(Node3D waypoint, bool force)
	{
		CameraTourWaypoint typed = AsWaypoint(waypoint);
		if (typed == null)
		{
			if (force && DefaultLookAt != null)
			{
				SetLookAt(DefaultLookAt);
			}

			return;
		}

		if (!typed.ApplyLookAtOnArrive && !force)
		{
			return;
		}

		Node3D lookAt = typed.LookAtTarget != null ? typed.LookAtTarget : DefaultLookAt;
		if (lookAt != null)
		{
			SetLookAt(lookAt);
		}
	}

	private float ResolveMoveSpeed(Node3D waypoint)
	{
		CameraTourWaypoint typed = AsWaypoint(waypoint);
		if (typed != null && typed.SpeedOverride > 0.0f)
		{
			return typed.SpeedOverride;
		}

		return Mathf.Max(0.01f, MoveSpeed);
	}

	private float ResolveArriveThreshold(Node3D waypoint)
	{
		CameraTourWaypoint typed = AsWaypoint(waypoint);
		if (typed != null && typed.ArriveThresholdOverride > 0.0f)
		{
			return typed.ArriveThresholdOverride;
		}

		return Mathf.Max(0.05f, ArriveThreshold);
	}

	private static CameraTourWaypoint AsWaypoint(Node3D node)
	{
		return node as CameraTourWaypoint;
	}
}
