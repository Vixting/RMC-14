using Content.Shared.Examine;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;

namespace Content.Server._RMC14.Botany;

public sealed class FloraDataDiscSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<FloraDataDiscComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<FloraDataDiscComponent, GetVerbsEvent<ActivationVerb>>(OnGetVerbs);
    }

    private void OnExamined(Entity<FloraDataDiscComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        using (args.PushGroup(nameof(FloraDataDiscComponent)))
        {
            if (ent.Comp.Genes.Count == 0)
            {
                args.PushMarkup(Loc.GetString("rmc-flora-disc-empty"));
                return;
            }

            if (ent.Comp.GeneSource != null)
                args.PushMarkup(Loc.GetString("rmc-flora-disc-source", ("source", ent.Comp.GeneSource)));

            foreach (var gene in ent.Comp.Genes)
            {
                args.PushMarkup(Loc.GetString("rmc-flora-disc-gene-entry", ("gene", gene.GetLabel())));
            }
        }
    }

    private void OnGetVerbs(Entity<FloraDataDiscComponent> ent, ref GetVerbsEvent<ActivationVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        if (ent.Comp.Genes.Count == 0)
            return;

        var user = args.User;
        var uid = ent.Owner;
        args.Verbs.Add(new ActivationVerb
        {
            Text = Loc.GetString("rmc-flora-disc-wipe-verb"),
            Act = () => WipeDisc(uid, ent.Comp, user),
        });
    }

    private void WipeDisc(EntityUid uid, FloraDataDiscComponent comp, EntityUid user)
    {
        comp.Genes.Clear();
        comp.GeneSource = null;
        _popup.PopupEntity(Loc.GetString("rmc-flora-disc-wiped"), uid, user);
        _audio.PlayPvs("/Audio/Machines/twobeep.ogg", uid);
    }
}
