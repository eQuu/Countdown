using Godot;

namespace Countdown.scripts.decorations;

[Tool]
public partial class Painting : Node3D
{
    [Export] public StandardMaterial3D _defaultPaintingMaterial;
    [Export] public SpotLight3D[] _spotLights = [];
    [Export] public MeshInstance3D _paintingPlane;
    private Texture2D _paintingTexture;

    [Export]
    public Texture2D PaintingTexture
    {
        get => GetPaintingTexture();
        set => SetPaintingTexture(value);
    }

    private Texture2D GetPaintingTexture()
    {
        return _paintingTexture;
    }

    private void SetPaintingTexture(Texture2D texture2D)
    {
        if (_defaultPaintingMaterial == null || _paintingPlane == null) return;

        var customStandardMaterial3D = (StandardMaterial3D)_defaultPaintingMaterial.Duplicate();
        customStandardMaterial3D.AlbedoTexture = texture2D;
        
        _paintingPlane.SetSurfaceOverrideMaterial(0, customStandardMaterial3D);
        _paintingTexture = texture2D;
    }

    public void ActivateLighting()
    {
        if (_spotLights == null) return;
        foreach (var spotLight in _spotLights)
        {
            spotLight.Visible = true;
        }
    }

    public void DeactivateLighting()
    {
        if (_spotLights == null) return;
        foreach (var spotLight in _spotLights)
        {
            spotLight.Visible = false;
        }
    }
}
