using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Storage;

[Serializable, NetSerializable]
public sealed class RMCStorageToggleHardReservedEvent : EntityEventArgs
{
    public readonly NetEntity Storage;
    public readonly NetEntity Item;

    public RMCStorageToggleHardReservedEvent(NetEntity storage, NetEntity item)
    {
        Storage = storage;
        Item = item;
    }
}
