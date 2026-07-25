using Countdown.scripts.decorations;
using Godot;

namespace Countdown.scripts;

public partial class LightingManager : Node
{
    [Export] private Node3D _ceilingLights;
    [Export] private Painting[] _paintings = [];

    public void ActivateCeilingLights()
    {
        if (_ceilingLights == null || _paintings == null) return;
        _ceilingLights.Visible = true;
        foreach (var painting in _paintings)
        {
            painting.Visible = false;
        }
    }

    public void DeactivateCeilingLights()
    {
        if (_ceilingLights == null || _paintings == null) return;
        _ceilingLights.Visible = false;
        foreach (var painting in _paintings)
        {
            painting.Visible = true;
        }
    }
}
