using Godot;

public partial class CameraTourWaypoint : Marker3D
{
	[Export] public Node3D LookAtTarget { get; set; }
	[Export] public bool ApplyLookAtOnArrive { get; set; } = false;
	[Export] public float SpeedOverride { get; set; } = -1.0f;
	[Export] public float ArriveWaitSeconds { get; set; } = 0.0f;
	[Export] public float ArriveThresholdOverride { get; set; } = -1.0f;
}
