using Content.Server._RMC14.Rules.DistressSignal;
using Content.Server.GameTicking;
using Content.Shared._RMC14.Rules;
using Content.Shared._RMC14.Xenonids.Hive;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Xenonids.Hive;

public sealed class RMCXenoHiveSlotSystem : EntitySystem
{
    private static readonly EntProtoId[] SlotProtos =
    [
        "CMXenoHiveCorrupted",
        "CMXenoHiveAlpha",
        "CMXenoHiveBravo",
        "CMXenoHiveCharlie",
        "CMXenoHiveDelta",
    ];

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RulePlayerSpawningEvent>(OnRulePlayerSpawning, after: [typeof(CMDistressSignalRuleSystem)]);
    }

    private void OnRulePlayerSpawning(RulePlayerSpawningEvent ev)
    {
        GenerateSlots();
    }

    public List<EntityUid> GenerateSlots()
    {
        var created = new List<EntityUid>();

        var existing = EntityQueryEnumerator<HiveSlotComponent>();
        if (existing.MoveNext(out _, out _))
            return created;

        var rules = EntityQueryEnumerator<CMDistressSignalRuleComponent>();
        if (rules.MoveNext(out _, out var rule) && Exists(rule.Hive))
        {
            var normal = EnsureComp<HiveSlotComponent>(rule.Hive);
            normal.Position = HiveSlots.Normal;
            Dirty(rule.Hive, normal);
            created.Add(rule.Hive);
        }

        foreach (var proto in SlotProtos)
            created.Add(Spawn(proto));

        return created;
    }
}
