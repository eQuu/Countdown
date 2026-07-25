using Godot;

[GlobalClass]
public partial class LaserPatternDefinition : Resource
{
	[Export] public string Id { get; set; } = "pattern";
	[Export] public string DisplayName { get; set; } = "Pattern";
	[Export] public float[] ArmAnglesDegrees { get; set; } = { 0f };
}
