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
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Random;
using System.Linq;

namespace Content.Server._RMC14.Botany;

public sealed class GeneEditorSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly BotanySystem _botany = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<GeneEditorComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<GeneEditorComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<GeneEditorComponent, BeforeActivatableUIOpenEvent>(OnBeforeUIOpen);
        SubscribeLocalEvent<GeneEditorComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAltVerbs);

        Subs.BuiEvents<GeneEditorComponent>(GeneEditorUiKey.Key, subs =>
        {
            subs.Event<GeneEditorApplyBuiMsg>(OnApplyGenes);
            subs.Event<GeneEditorEjectBuiMsg>(OnEjectAll);
        });
    }

    private void OnInit(Entity<GeneEditorComponent> ent, ref ComponentInit args)
    {
        ent.Comp.DiscSlot = _container.EnsureContainer<ContainerSlot>(ent, ent.Comp.DiscSlotId);
        ent.Comp.SeedSlot = _container.EnsureContainer<ContainerSlot>(ent, ent.Comp.SeedSlotId);
    }

    private void OnInteractUsing(Entity<GeneEditorComponent> ent, ref InteractUsingEvent args)
    {
        if (!this.IsPowered(ent, EntityManager))
        {
            _popup.PopupCursor(Loc.GetString("rmc-gene-editor-no-power"), args.User);
            return;
        }

        if (HasComp<FloraDataDiscComponent>(args.Used))
        {
            if (ent.Comp.DiscSlot.ContainedEntity != null)
            {
                _popup.PopupCursor(Loc.GetString("rmc-gene-editor-disc-already-loaded"), args.User);
                return;
            }

            if (!_container.Insert(args.Used, ent.Comp.DiscSlot))
                return;

            UpdateState(ent);
            args.Handled = true;
            return;
        }

        if (HasComp<SeedComponent>(args.Used))
        {
            if (ent.Comp.DiscSlot.ContainedEntity == null)
            {
                _popup.PopupCursor(Loc.GetString("rmc-gene-editor-load-disc-first"), args.User);
                return;
            }

            if (ent.Comp.SeedSlot.ContainedEntity != null)
            {
                _popup.PopupCursor(Loc.GetString("rmc-gene-editor-seed-already-loaded"), args.User);
                return;
            }

            if (!_container.Insert(args.Used, ent.Comp.SeedSlot))
                return;

            UpdateState(ent);
            args.Handled = true;
        }
    }

    private void OnBeforeUIOpen(Entity<GeneEditorComponent> ent, ref BeforeActivatableUIOpenEvent args)
    {
        UpdateState(ent);
    }

    private void OnApplyGenes(Entity<GeneEditorComponent> ent, ref GeneEditorApplyBuiMsg args)
    {
        ApplyGenes(ent, args.Actor);
    }

    private void OnEjectAll(Entity<GeneEditorComponent> ent, ref GeneEditorEjectBuiMsg args)
    {
        EjectAll(ent, args.Actor);
    }

    private void OnGetAltVerbs(Entity<GeneEditorComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var user = args.User;
        var comp = ent.Comp;

        if (comp.DiscSlot.ContainedEntity != null && comp.SeedSlot.ContainedEntity != null)
        {
            args.Verbs.Add(new AlternativeVerb
            {
                Text = Loc.GetString("rmc-gene-editor-apply-verb"),
                Priority = 2,
                Act = () => ApplyGenes(ent, user),
            });
        }

        if (comp.DiscSlot.ContainedEntity != null || comp.SeedSlot.ContainedEntity != null)
        {
            args.Verbs.Add(new AlternativeVerb
            {
                Text = Loc.GetString("rmc-gene-editor-eject-verb"),
                Priority = 1,
                Act = () => EjectAll(ent, user),
            });
        }
    }

    private void ApplyGenes(Entity<GeneEditorComponent> ent, EntityUid user)
    {
        if (!this.IsPowered(ent, EntityManager))
        {
            _popup.PopupCursor(Loc.GetString("rmc-gene-editor-no-power"), user);
            return;
        }

        var comp = ent.Comp;
        var discEnt = comp.DiscSlot.ContainedEntity;
        var seedEnt = comp.SeedSlot.ContainedEntity;

        if (discEnt == null || seedEnt == null)
            return;

        if (!TryComp(discEnt, out FloraDataDiscComponent? disc) || disc.Genes.Count == 0)
        {
            _popup.PopupCursor(Loc.GetString("rmc-gene-editor-disc-empty"), user);
            return;
        }

        if (!TryComp(seedEnt, out SeedComponent? seedComp) || !_botany.TryGetSeed(seedComp, out var seed))
        {
            _popup.PopupCursor(Loc.GetString("rmc-gene-editor-no-seed-data"), user);
            return;
        }

        if (seed.GeneEditCount >= comp.MaxEditCount)
        {
            _popup.PopupCursor(Loc.GetString("rmc-gene-editor-seed-ruined"), user);
            EjectAll(ent, user);
            return;
        }

        if (!seed.Unique)
        {
            seed = seed.Clone();
            seedComp.Seed = seed;
            seedComp.SeedId = null;
        }

        var failureChance = seed.GeneEditCount / 100f;
        var failed = false;

        foreach (var gene in disc.Genes)
        {
            if (_random.Prob(failureChance))
            {
                failed = true;
                break;
            }

            gene.ApplyTo(seed);
        }

        if (failed)
        {
            seed.GeneEditCount = comp.MaxEditCount + 1;
            seed.Viable = false;
            _audio.PlayPvs(comp.FailSound, ent);
            _popup.PopupCursor(Loc.GetString("rmc-gene-editor-failed"), user);
        }
        else
        {
            seed.GeneEditCount += _random.Next(comp.EditCountAddMin, comp.EditCountAddMax + 1);
            _audio.PlayPvs(comp.ApplySound, ent);
        }

        var newName = Loc.GetString("botany-seed-packet-name",
            ("seedName", Loc.GetString(seed.Name)),
            ("seedNoun", Loc.GetString(seed.Noun)));
        _metaData.SetEntityName(seedEnt.Value, newName + "*");

        EjectAll(ent, user);
    }

    private void EjectAll(Entity<GeneEditorComponent> ent, EntityUid user)
    {
        var comp = ent.Comp;

        if (comp.SeedSlot.ContainedEntity is { } seedEnt)
        {
            _container.Remove(seedEnt, comp.SeedSlot);
            _hands.TryPickupAnyHand(user, seedEnt);
        }

        if (comp.DiscSlot.ContainedEntity is { } discEnt)
        {
            _container.Remove(discEnt, comp.DiscSlot);
            _hands.TryPickupAnyHand(user, discEnt);
        }

        _audio.PlayPvs(comp.EjectSound, ent);
        UpdateState(ent);
    }

    private void UpdateState(Entity<GeneEditorComponent> ent)
    {
        var comp = ent.Comp;
        var discEnt = comp.DiscSlot.ContainedEntity;
        var seedEnt = comp.SeedSlot.ContainedEntity;

        comp.HasDisc = discEnt != null;
        comp.DiscSource = null;
        comp.DiscGeneLabels = new List<string>();

        if (discEnt != null && TryComp(discEnt, out FloraDataDiscComponent? disc))
        {
            comp.DiscSource = disc.GeneSource;
            comp.DiscGeneLabels = disc.Genes.Select(g => g.GetLabel()).ToList();
        }

        comp.HasSeed = seedEnt != null;
        comp.SeedEntityNet = seedEnt != null ? GetNetEntity(seedEnt.Value) : null;
        comp.SeedName = null;
        comp.SeedEditCount = 0;

        if (seedEnt != null && TryComp(seedEnt, out SeedComponent? seedComp) && _botany.TryGetSeed(seedComp, out var seed))
        {
            comp.SeedName = Loc.GetString(seed.DisplayName);
            comp.SeedEditCount = seed.GeneEditCount;
        }

        Dirty(ent, comp);
    }
}
