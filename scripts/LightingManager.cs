using Countdown.scripts.decorations;
using Godot;

namespace Countdown.scripts;

public partial class LightingManager : Node
{
    [Export] private Node3D _ceilingLights;
    [Export] private Node3D _darkLights;
    [Export] private Painting[] _paintings = [];

    public void ActivateCeilingLights()
    {
        if (_ceilingLights == null || _darkLights == null || _paintings == null) return;

        foreach (var painting in _paintings) painting.DeactivateLighting();
        _darkLights.Visible = false;
        _ceilingLights.Visible = true;
    }

    public void DeactivateCeilingLights()
    {
        if (_ceilingLights == null || _darkLights == null || _paintings == null) return;

        _ceilingLights.Visible = false;
        _darkLights.Visible = true;
        foreach (var painting in _paintings) painting.ActivateLighting();
    }
}
