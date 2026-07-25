using Godot;

public partial class PlayerMainMenuAnimation : CharacterBody3D
{
	[Export] private AnimationPlayer _animationPlayer;
	[Export] private double _animationStartTime;

	public override void _Ready()
	{
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_animationPlayer == null)
		{
			return;
		}

		if (_animationStartTime > 0.0)
		{
			_animationStartTime -= delta;
			if (_animationStartTime <= 0.0)
			{
				_animationStartTime = 0.0;
				_animationPlayer.Play("idle");
			}
		}
		else
		{
			_animationPlayer.Play("idle");
		}
	}
}
