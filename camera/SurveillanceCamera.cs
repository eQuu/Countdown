using Countdown.camera;
using Godot;
using System;

public partial class SurveillanceCamera : Node3D
{
	[Export] private AnimationPlayer _animationPlayer;
	[Export] private CameraAnimations _chosenAnimation;
	[Export] private double _animationStartTime;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
        if (_animationStartTime > 0.0f)
        {
            _animationStartTime -= delta;
            if (_animationStartTime <= 0.0f)
            {
                _animationStartTime = 0.0f;
                _animationPlayer.Play(_chosenAnimation.ToString());
            }
        } else
        {
            _animationPlayer.Play(_chosenAnimation.ToString());
        }
    }
}
