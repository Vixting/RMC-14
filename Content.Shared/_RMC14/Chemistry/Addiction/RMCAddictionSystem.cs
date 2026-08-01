using System.Linq;
using Content.Shared._RMC14.Body;
using Content.Shared._RMC14.BlurredVision;
using Content.Shared._RMC14.Movement;
using Content.Shared._RMC14.Stamina;
using Content.Shared.FixedPoint;
using Content.Shared.Jittering;
using Content.Shared.Popups;
using Content.Shared.StatusEffect;
using Robust.Shared.Network;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Chemistry.Addiction;

/// <summary>
/// Ports cmss13's <c>/datum/disease/addiction</c> (<c>code/datums/diseases/addiction.dm</c>) - a
/// per-reagent, stage-based addiction/withdrawal tracker. Runs on its own 2-second cadence
/// independent of the metabolism tick system, matching cmss13's own <c>SSdisease</c>. Compiles into
/// Content.Shared (rather than Content.Server) and self-guards on <see cref="INetManager.IsClient"/>
/// so it can be called directly from the Shared <c>Addictive</c>/<c>Antiaddictive</c> chem effects
/// without needing an event-indirection hop - the same pattern <see cref="RMCStaminaSystem"/> itself
/// already uses.
/// </summary>
public sealed class RMCAddictionSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedRMCBloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly SharedJitteringSystem _jitter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly RMCStaminaSystem _stamina = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;
    [Dependency] private readonly TemporarySpeedModifiersSystem _speed = default!;

    private static readonly string[] CravingMessages =
    [
        "You could really use another hit right about now.",
        "Your hands won't stop trembling.",
        "You can't stop thinking about it.",
        "Your skin is crawling. You need something to take the edge off.",
        "A wave of nausea reminds you what you're missing.",
        "You feel like something is missing.",
    ];

    private static readonly string[] LateStageCravingMessages =
    [
        "YOUR BODY IS SCREAMING FOR IT!",
        "YOU CAN'T TAKE THIS ANYMORE!",
        "EVERYTHING HURTS! YOU NEED IT NOW!",
        "YOU'RE GOING TO LOSE YOUR MIND!",
    ];

    private static readonly string[] HighMessages =
    [
        "You feel a wave of relief wash over you.",
        "You feel great.",
        "Everything feels a little easier right now.",
        "A pleasant warmth spreads through you.",
        "You feel relaxed and at ease.",
    ];

    private const float ProgressionThreshold = 300f;
    private const int MaxStage = 4;

    // 0.383 per 2s tick while clean, ~30 minutes to fall a stage
    private const float DecayPerTick = 0.383f;

    private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(2);
    private TimeSpan _nextUpdate;

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        if (_timing.CurTime < _nextUpdate)
            return;

        _nextUpdate = _timing.CurTime + UpdateInterval;

        var query = EntityQueryEnumerator<RMCAddictedComponent>();
        while (query.MoveNext(out var uid, out var addicted))
        {
            ProcessMob(uid, addicted);
        }
    }

    private void ProcessMob(EntityUid uid, RMCAddictedComponent addicted)
    {
        _bloodstream.TryGetChemicalSolution(uid, out _, out var solution);

        for (var i = addicted.Addictions.Count - 1; i >= 0; i--)
        {
            var record = addicted.Addictions[i];

            if (solution != null && solution.GetTotalPrototypeQuantity(record.ReagentId) > FixedPoint2.Zero)
            {
                record.WithdrawalProgression = 0;

                var highChance = record.Stage switch
                {
                    1 => 0.05f,
                    2 => 0.1f,
                    3 => 0.15f,
                    _ => 0.2f,
                };
                if (_random.Prob(highChance))
                    _popup.PopupEntity(_random.Pick(HighMessages), uid, uid);

                continue;
            }

            record.AddictionProgression = MathF.Max(0, record.AddictionProgression - DecayPerTick);
            record.WithdrawalProgression += record.Multiplier;

            ApplyWithdrawalSymptoms(uid, record);
        }

        if (addicted.Addictions.Count == 0)
            RemComp<RMCAddictedComponent>(uid);
        else
            Dirty(uid, addicted);
    }

    private void ApplyWithdrawalSymptoms(EntityUid uid, RMCAddictionRecord record)
    {
        if (record.Stage >= 3 && _random.Prob(0.2f))
        {
            double amount = record.Stage switch
            {
                3 => 6,
                _ => 8,
            };
            _stamina.DoStaminaDamage(uid, amount);
        }

        var cravingChance = record.Stage switch
        {
            1 => 0.15f,
            2 => 0.3f,
            3 => 0.5f,
            _ => 0.65f,
        };
        if (_random.Prob(cravingChance))
        {
            if (record.Stage >= 3 && _random.Prob(0.3f))
                _popup.PopupEntity(_random.Pick(LateStageCravingMessages), uid, uid, PopupType.LargeCaution);
            else
                _popup.PopupEntity(_random.Pick(CravingMessages), uid, uid);
        }

        var slowdown = record.Stage switch
        {
            1 => 0.95f,
            2 => 0.85f,
            3 => 0.75f,
            _ => 0.65f,
        };
        var modifiers = new List<TemporarySpeedModifierSet> { new(TimeSpan.FromSeconds(3), slowdown, slowdown) };
        _speed.ModifySpeed(uid, modifiers);

        if (record.Stage >= 2)
            _jitter.DoJitter(uid, TimeSpan.FromSeconds(3), true);

        var dimmedVisionChance = record.Stage switch
        {
            3 => 0.08f,
            >= 4 => 0.12f,
            _ => 0f,
        };
        if (dimmedVisionChance > 0 && _random.Prob(dimmedVisionChance))
            _status.TryAddStatusEffect<RMCBlindedComponent>(uid, "Blinded", TimeSpan.FromSeconds(10), true);
    }

    public void TryExposeToAddictive(EntityUid uid, string reagentId, float potency)
    {
        var addicted = EnsureComp<RMCAddictedComponent>(uid);
        var record = addicted.Addictions.FirstOrDefault(r => r.ReagentId == reagentId);

        if (record == null)
        {
            record = new RMCAddictionRecord
            {
                ReagentId = reagentId,
                Stage = 1,
                AddictionProgression = potency,
                Multiplier = potency,
            };
            addicted.Addictions.Add(record);
        }
        else
        {
            record.AddictionProgression += potency;
        }

        if (record.Stage < MaxStage && record.AddictionProgression >= ProgressionThreshold * record.Stage)
            record.Stage++;

        Dirty(uid, addicted);
    }

    public void TryReduceAddictions(EntityUid uid, float potency)
    {
        if (!TryComp<RMCAddictedComponent>(uid, out var addicted))
            return;

        if (potency > 3f)
        {
            RemComp<RMCAddictedComponent>(uid);
            return;
        }

        var reduction = potency * 2f;
        for (var i = addicted.Addictions.Count - 1; i >= 0; i--)
        {
            var record = addicted.Addictions[i];
            record.AddictionProgression = MathF.Max(0, record.AddictionProgression - reduction);
            record.WithdrawalProgression = MathF.Max(0, record.WithdrawalProgression - reduction);

            if (record.AddictionProgression <= 0 && record.WithdrawalProgression <= 0)
                addicted.Addictions.RemoveAt(i);
        }

        if (addicted.Addictions.Count == 0)
            RemComp<RMCAddictedComponent>(uid);
        else
            Dirty(uid, addicted);
    }
}
