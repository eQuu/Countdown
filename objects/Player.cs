using Godot;
using System;
using System.Diagnostics;
using static Godot.TextServer;

public partial class Player : CharacterBody3D
{
    public const float FRICTION = 20f;

    [Export] private AnimationPlayer _animationPlayer;
    [Export] private int _playerNumber = 1;
    [Export] private Timer _timer;

    private Vector3 _VelocityPlayer = Vector3.Zero;
    private float _speed = 8f;
    private bool canDash = true;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {

    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {

    }

    public override void _PhysicsProcess(double delta)
    {
        HandleMovement(delta);
    }

    private void HandleMovement(double delta)
    {
        // TODO: Bei Kollision die Velocity entfernen. Sonst klebt man an Dingen
        Vector2 direction = Vector2.Zero;

        direction = Input.GetVector("MoveLeftPlayer" + _playerNumber, "MoveRightPlayer" + _playerNumber, "MoveUpPlayer" + _playerNumber, "MoveDownPlayer" + _playerNumber);

        if (canDash && Input.IsActionJustReleased("ActionPlayer" + _playerNumber))
        {
            canDash = false;
            _timer.Start();
            Vector3 forward = GlobalTransform.Basis.Z;
            _VelocityPlayer.X = forward.Normalized().X * _speed * 3;
            _VelocityPlayer.Z = forward.Normalized().Z * _speed * 3;
        }

        ApplySlippyness(direction, delta);
        AnimatePlayer(direction);

        Velocity = _VelocityPlayer;
        MoveAndSlide();
    }

    private void ApplySlippyness(Vector2 direction, double delta)
    {
        if (direction.X != 0)
        {
            _VelocityPlayer.X = (float)Mathf.MoveToward(_VelocityPlayer.X, (direction.X * _speed), FRICTION * delta);
        }
        else
        {
            _VelocityPlayer.X = (float)Mathf.MoveToward(_VelocityPlayer.X, 0, FRICTION * delta);
        }

        if (direction.Y != 0)
        {

            _VelocityPlayer.Z = (float)Mathf.MoveToward(_VelocityPlayer.Z, (direction.Y * _speed), FRICTION * delta);
        }
        else
        {
            _VelocityPlayer.Z = (float)Mathf.MoveToward(_VelocityPlayer.Z, 0, FRICTION * delta);
        }
    }

    private void AnimatePlayer(Vector2 direction)
    {
        if (direction != Vector2.Zero)
        {
            _animationPlayer.Play("walk");
            direction = direction.Normalized();
            Vector3 direction3 = new Vector3(direction.X, 0, direction.Y);
            LookAt(Transform.Origin - direction3);
        }
        else
        {
            _animationPlayer.Play("idle");
        }
    }

    private void OnTimerTimeout()
    {
        canDash = true;
    }
}
