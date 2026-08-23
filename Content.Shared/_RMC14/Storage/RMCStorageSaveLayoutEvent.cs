using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Storage;

[Serializable, NetSerializable]
public sealed class RMCStorageSaveLayoutEvent : EntityEventArgs
{
    public readonly NetEntity Storage;
    public readonly bool Hard;

    public RMCStorageSaveLayoutEvent(NetEntity storage, bool hard = false)
    {
        Storage = storage;
        Hard = hard;
    }
}
