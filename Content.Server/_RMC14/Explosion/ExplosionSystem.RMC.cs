using Content.Shared.Explosion;

// ReSharper disable CheckNamespace
namespace Content.Server.Explosion.EntitySystems;

public sealed partial class ExplosionSystem
{
    private bool RMCHidesShockwaveSmoke(string id)
    {
        return _prototypeManager.TryIndex<ExplosionPrototype>(id, out var type) && type.PowerPerIntensity > 0;
    }
}
