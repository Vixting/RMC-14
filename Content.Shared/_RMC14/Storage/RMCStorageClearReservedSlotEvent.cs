using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Storage;

[Serializable, NetSerializable]
public sealed class RMCStorageClearReservedSlotEvent : EntityEventArgs
{
    public readonly NetEntity Storage;
    public readonly NetEntity Item;

    public RMCStorageClearReservedSlotEvent(NetEntity storage, NetEntity item)
    {
        Storage = storage;
        Item = item;
    }
}
