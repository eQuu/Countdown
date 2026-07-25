using Godot;

namespace Countdown.scripts;

public partial class LightingManager : Node
{
    [Export] private Node3D _ceilingLights;

    public void ActivateCeilingLights()
    {
        if (_ceilingLights == null) return;
        _ceilingLights.Visible = true;
    }

    public void DeactivateCeilingLights()
    {
        if (_ceilingLights == null) return;
        _ceilingLights.Visible = false;
    }
}
