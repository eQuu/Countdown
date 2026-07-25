using Godot;
using System;

public partial class PlayerCheerScript : CharacterBody3D
{
	[Export] private AnimationPlayer _animationPlayer;

    public override void _Ready()
    {
        _animationPlayer.Play("cheer");
    }
}
