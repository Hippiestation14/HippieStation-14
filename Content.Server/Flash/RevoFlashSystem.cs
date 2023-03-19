using Content.Server.Players;
using Content.Server.Mind.Components;
using Content.Server.Traitor;
using Content.Server.Administration.Commands;
using Content.Shared.Flash;
using Content.Shared.Roles;
using Content.Shared.Mobs;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
namespace Content.Server.Flash
{
    internal sealed class RevoHeadFlashSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        [Dependency] private readonly DamageableSystem _damageSystem = default!;

        private const string RevolutionaryPrototypeId = "Revolutionary";
        private const string RevolutionaryHeadPrototypeId = "RevolutionaryHead";

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<FlashableComponent,FlashEvent>(RevoFlash);
            SubscribeLocalEvent<MobStateChangedEvent>(OnDead);
        }

        public void RevoFlash(EntityUid target, FlashableComponent comp, FlashEvent ev)
        {
            if (!TryComp<MindComponent>(ev.User, out var usermindcomp) || usermindcomp.Mind is null )
                return;

            if (!TryComp<MobState>(ev.User, out var userMobState) || userMobState != MobState.Dead)
                return;

            foreach (var role in usermindcomp.Mind.AllRoles)
            {
                if (role is not TraitorRole traitor)
                    continue;
                if (traitor.Prototype.ID == RevolutionaryHeadPrototypeId)
                {
                    Convert(target, ev);
                }
            }
        }

        private void Convert(EntityUid target, FlashEvent ev)
        {
            if (!TryComp<MindComponent>(target, out var targetmindcomp) || targetmindcomp.Mind is null || targetmindcomp.Mind.CurrentJob is null)
                return;

            foreach (var department in targetmindcomp.Mind.CurrentJob.Prototype.Access)
            {
                if (targetmindcomp.Mind.HasRole<TraitorRole>())
                    return;
                if (department != "Command" || department != "Security")
                {
                    var antagPrototype = _prototypeManager.Index<AntagPrototype>(RevolutionaryPrototypeId);
                    var revoRole = new TraitorRole(targetmindcomp.Mind, antagPrototype);
                    targetmindcomp.Mind.AddRole(revoRole);
                    // Revive and heal the target with low health shitcode
                    if (HasComp<DamageableComponent>(target))
                    {
                        RejuvenateCommand.PerformRejuvenate(target);
                        var damage = new DamageSpecifier(_prototypeManager.Index<DamageTypePrototype>("Blunt"), 70);
                        _damageSystem.TryChangeDamage(target, damage, true);
                    }


                    SoundSystem.Play("/Audio/Magic/staff_chaos.ogg", Filter.Empty().AddWhere(s => ((IPlayerSession)s).Data.ContentData()?.Mind?.HasRole<TraitorRole>() ?? false), AudioParams.Default);
                }
            }
        }
        public void OnDead(MobStateChangedEvent ev)
        {
            if (!TryComp<MindComponent>(ev.Target, out var targetmindcomp) || targetmindcomp.Mind is null || targetmindcomp.Mind.CurrentJob is null)
                return;
            if (targetmindcomp.Mind.HasRole<TraitorRole>())
                return;
            if(ev.NewMobState!=MobState.Dead)
                return;
            foreach (var role in targetmindcomp.Mind.AllRoles)
            {
                if (role is not TraitorRole traitor)
                    continue;
                if (traitor.Prototype.ID == RevolutionaryPrototypeId)
                {
                    targetmindcomp.Mind.RemoveRole(role);
                    // Revive and heal the target with low health shitcode
                    if (HasComp<DamageableComponent>(ev.Target))
                    {
                        RejuvenateCommand.PerformRejuvenate(ev.Target);
                        var damage = new DamageSpecifier(_prototypeManager.Index<DamageTypePrototype>("Blunt"), 70);
                        _damageSystem.TryChangeDamage(ev.Target, damage, true);
                    }
                }
            }
        }
    }
}
