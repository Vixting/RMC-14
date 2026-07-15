using Content.Server.Botany.Components;
using Content.Shared.Botany;
using Content.Shared.Botany.Components;
using Content.Server.Botany.Systems;
using Content.Server.Power.EntitySystems;
using Content.Shared._RMC14.Botany;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.UserInterface;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Random;
using System.Linq;

namespace Content.Server._RMC14.Botany;

public sealed class LysisCentrifugeSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly BotanySystem _botany = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<LysisCentrifugeComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<LysisCentrifugeComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<LysisCentrifugeComponent, BeforeActivatableUIOpenEvent>(OnBeforeUIOpen);
        SubscribeLocalEvent<LysisCentrifugeComponent, GetVerbsEvent<ActivationVerb>>(OnGetVerbs);

        Subs.BuiEvents<LysisCentrifugeComponent>(LysisCentrifugeUiKey.Key, subs =>
        {
            subs.Event<LysisCentrifugeProcessSeedBuiMsg>(OnProcessSeed);
            subs.Event<LysisCentrifugeEjectSeedBuiMsg>(OnEjectSeed);
            subs.Event<LysisCentrifugeExtractGeneBuiMsg>(OnExtractGene);
            subs.Event<LysisCentrifugeClearBufferBuiMsg>(OnClearBuffer);
            subs.Event<LysisCentrifugeEjectDiscBuiMsg>(OnEjectDisc);
        });
    }

    private void OnInit(Entity<LysisCentrifugeComponent> ent, ref ComponentInit args)
    {
        EnsureComp<LysisCentrifugeServerComponent>(ent);
        ent.Comp.DiscSlot = _container.EnsureContainer<ContainerSlot>(ent, ent.Comp.DiscSlotId);
        ent.Comp.SeedSlot = _container.EnsureContainer<ContainerSlot>(ent, ent.Comp.SeedSlotId);

        foreach (var type in Enum.GetValues<PlantGeneType>())
        {
            ent.Comp.ObfuscationCodes[type] = $"{_random.Next(0, 256):X2} {_random.Next(0, 256):X2}";
        }
    }

    private void OnInteractUsing(Entity<LysisCentrifugeComponent> ent, ref InteractUsingEvent args)
    {
        if (!this.IsPowered(ent, EntityManager))
        {
            _popup.PopupCursor("The centrifuge has no power.", args.User);
            return;
        }

        if (HasComp<FloraDataDiscComponent>(args.Used))
        {
            if (ent.Comp.DiscSlot.ContainedEntity != null)
            {
                _popup.PopupCursor("A disc is already loaded. Eject it first via the interface.", args.User);
                return;
            }

            if (!_container.Insert(args.Used, ent.Comp.DiscSlot))
                return;

            UpdateState(ent);
            args.Handled = true;
            return;
        }

        if (TryComp(args.Used, out SeedComponent? seedComp))
        {
            if (!_botany.TryGetSeed(seedComp, out var seed))
            {
                _popup.PopupCursor("This seed packet contains no genetic data.", args.User);
                return;
            }

            if (seed.Seedless)
            {
                _popup.PopupCursor("This plant is seedless — its genome cannot be extracted.", args.User);
                return;
            }

            if (ent.Comp.SeedSlot.ContainedEntity != null)
            {
                _popup.PopupCursor("A seed packet is already loaded. Eject it first.", args.User);
                return;
            }

            if (!_container.Insert(args.Used, ent.Comp.SeedSlot))
                return;

            UpdateState(ent);
            args.Handled = true;
        }
    }

    private void OnBeforeUIOpen(Entity<LysisCentrifugeComponent> ent, ref BeforeActivatableUIOpenEvent args)
    {
        UpdateState(ent);
    }

    private void OnProcessSeed(Entity<LysisCentrifugeComponent> ent, ref LysisCentrifugeProcessSeedBuiMsg args)
    {
        var comp = ent.Comp;

        if (!this.IsPowered(ent, EntityManager))
        {
            _popup.PopupCursor("The centrifuge has no power.", args.Actor);
            return;
        }

        if (comp.SeedSlot.ContainedEntity is not { } seedEnt)
        {
            _popup.PopupCursor("No seed packet is loaded.", args.Actor);
            return;
        }

        if (comp.GenomeName != null)
        {
            _popup.PopupCursor("A genome is already loaded. Clear the buffer first.", args.Actor);
            return;
        }

        if (!TryComp(seedEnt, out SeedComponent? seedComp) || !_botany.TryGetSeed(seedComp, out var seed))
        {
            _popup.PopupCursor("This seed packet contains no genetic data.", args.Actor);
            _container.Remove(seedEnt, comp.SeedSlot);
            UpdateState(ent);
            return;
        }

        var server = EnsureComp<LysisCentrifugeServerComponent>(ent);
        server.LoadedSeed = seed.Clone();
        comp.GenomeName = Loc.GetString(seed.DisplayName);
        comp.Degradation = 0;

        _container.Remove(seedEnt, comp.SeedSlot);
        QueueDel(seedEnt);

        _audio.PlayPvs(comp.ScanSound, ent);
        UpdateState(ent);
    }

    private void OnEjectSeed(Entity<LysisCentrifugeComponent> ent, ref LysisCentrifugeEjectSeedBuiMsg args)
    {
        if (ent.Comp.SeedSlot.ContainedEntity is not { } seedEnt)
            return;

        _container.Remove(seedEnt, ent.Comp.SeedSlot);
        _hands.TryPickupAnyHand(args.Actor, seedEnt);
        UpdateState(ent);
    }

    private void OnExtractGene(Entity<LysisCentrifugeComponent> ent, ref LysisCentrifugeExtractGeneBuiMsg args)
    {
        var comp = ent.Comp;

        if (!this.IsPowered(ent, EntityManager))
        {
            _popup.PopupCursor("The centrifuge has no power.", args.Actor);
            return;
        }

        if (!TryComp(ent, out LysisCentrifugeServerComponent? server) || server.LoadedSeed == null)
        {
            _popup.PopupCursor("No genome is loaded. Insert a seed packet first.", args.Actor);
            return;
        }

        if (comp.Degradation >= comp.MaxDegradation)
        {
            _popup.PopupCursor("Buffer fully degraded. Genome wiped.", args.Actor);
            WipeBuffer(comp, server);
            UpdateState(ent);
            return;
        }

        var discEnt = comp.DiscSlot.ContainedEntity;
        if (discEnt == null || !TryComp(discEnt, out FloraDataDiscComponent? disc))
        {
            _popup.PopupCursor("No disc loaded. Insert a flora data disc first.", args.Actor);
            return;
        }

        if (disc.Genes.Count >= 1)
        {
            _popup.PopupCursor("The disc already contains a gene sequence.", args.Actor);
            return;
        }

        var geneType = args.GeneType;
        if (disc.Genes.Any(g => g.Type == geneType))
        {
            _popup.PopupCursor("This sequence is already stored on the disc.", args.Actor);
            return;
        }

        var gene = PlantGene.FromSeed(server.LoadedSeed, geneType);
        gene.DisplayLabel = comp.ObfuscationCodes.GetValueOrDefault(geneType);
        disc.Genes.Add(gene);
        disc.GeneSource ??= comp.GenomeName;

        comp.Degradation += _random.Next(comp.DegradationMin, comp.DegradationMax + 1);

        if (comp.Degradation >= comp.MaxDegradation)
        {
            _audio.PlayPvs(comp.FailSound, ent);
            WipeBuffer(comp, server);
            _container.Remove(discEnt.Value, comp.DiscSlot);
            _hands.TryPickupAnyHand(args.Actor, discEnt.Value);
            UpdateState(ent);
            return;
        }

        _audio.PlayPvs(comp.ExtractSound, ent);

        _container.Remove(discEnt.Value, comp.DiscSlot);
        _hands.TryPickupAnyHand(args.Actor, discEnt.Value);

        UpdateState(ent);
    }

    private void OnClearBuffer(Entity<LysisCentrifugeComponent> ent, ref LysisCentrifugeClearBufferBuiMsg args)
    {
        if (!this.IsPowered(ent, EntityManager))
            return;

        if (!TryComp(ent, out LysisCentrifugeServerComponent? server))
            return;

        WipeBuffer(ent.Comp, server);
        UpdateState(ent);
    }

    private void OnEjectDisc(Entity<LysisCentrifugeComponent> ent, ref LysisCentrifugeEjectDiscBuiMsg args)
    {
        if (ent.Comp.DiscSlot.ContainedEntity is not { } discEnt)
            return;

        _container.Remove(discEnt, ent.Comp.DiscSlot);
        _hands.TryPickupAnyHand(args.Actor, discEnt);
        UpdateState(ent);
    }

    private void OnGetVerbs(Entity<LysisCentrifugeComponent> ent, ref GetVerbsEvent<ActivationVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess || ent.Comp.GenomeName == null)
            return;

        var user = args.User;
        args.Verbs.Add(new ActivationVerb
        {
            Text = "Clear buffer",
            Act = () =>
            {
                if (!TryComp(ent, out LysisCentrifugeServerComponent? server))
                    return;
                WipeBuffer(ent.Comp, server);
                _popup.PopupEntity("Genetic buffer cleared.", ent, user);
                UpdateState(ent);
            },
        });
    }

    private void UpdateState(Entity<LysisCentrifugeComponent> ent)
    {
        var comp = ent.Comp;
        var discEnt = comp.DiscSlot.ContainedEntity;
        var seedEnt = comp.SeedSlot.ContainedEntity;

        FloraDataDiscComponent? disc = null;
        if (discEnt != null)
            TryComp(discEnt, out disc);

        var onDiscTypes = disc != null
            ? new HashSet<PlantGeneType>(disc.Genes.Select(g => g.Type))
            : new HashSet<PlantGeneType>();

        comp.GeneSlots = Enum.GetValues<PlantGeneType>()
            .Select(t => new LysisCentrifugeGeneSlot(
                t,
                comp.ObfuscationCodes.GetValueOrDefault(t, "??"),
                onDiscTypes.Contains(t)))
            .ToList();
        comp.HasDisc = disc != null;
        comp.DiscFull = disc != null && disc.Genes.Count >= 1;
        comp.HasSeed = seedEnt != null;
        comp.SeedPacketName = seedEnt != null ? Name(seedEnt.Value) : null;
        comp.SeedEntityNet = seedEnt != null ? GetNetEntity(seedEnt.Value) : null;

        Dirty(ent, comp);
    }

    private static void WipeBuffer(LysisCentrifugeComponent comp, LysisCentrifugeServerComponent server)
    {
        server.LoadedSeed = null;
        comp.GenomeName = null;
        comp.Degradation = 0;
    }
}
