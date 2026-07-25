using Godot;
using System;

public partial class PlayerCheerScript : CharacterBody3D
{
    [Export] public float IndicatorRingSize { get; set; } = 1.35f;
    [Export] public Color IndicatorColor { get; set; } = new Color(0.1f, 0.4f, 1.0f);
    [Export] public bool IsWinner = false;

    [Export] private AnimationPlayer _animationPlayer;
    [Export] private MeshInstance3D _indicatorRing;

    private ShaderMaterial _indicatorRingMaterial;

    public override void _Ready()
    {
        if (IsWinner)
        {
            _animationPlayer.Play("cheer");
        }
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
    }

    private void ApplyIndicatorRingColor()
    {
        if (_indicatorRingMaterial == null)
        {
            return;
        }

        _indicatorRingMaterial.SetShaderParameter("ring_color", IndicatorColor);
    }
}
