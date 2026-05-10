using System;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Application.GameLoop;
using DungeonInn.Domain.Actor;
using VContainer;

namespace DungeonInn.Application.Combat
{
    public sealed class CombatDamageResolver
    {
        readonly IActorCombatService actorCombatService;
        readonly IGameEventBus eventBus;
        readonly CombatDefeatResolver defeatResolver;

        [Inject]
        public CombatDamageResolver(
            IActorCombatService actorCombatService,
            IGameEventBus eventBus,
            CombatDefeatResolver defeatResolver)
        {
            this.actorCombatService = actorCombatService ?? throw new ArgumentNullException(nameof(actorCombatService));
            this.eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            this.defeatResolver = defeatResolver ?? throw new ArgumentNullException(nameof(defeatResolver));
        }

        public void ApplyDamage(IGameWorldState worldState, Actor attacker, Actor target, int damage)
        {
            if (worldState == null)
            {
                throw new ArgumentNullException(nameof(worldState));
            }

            if (attacker == null)
            {
                throw new ArgumentNullException(nameof(attacker));
            }

            ApplyDamage(worldState, attacker.Id, attacker, target, damage);
        }

        public void ApplyDamage(IGameWorldState worldState, Guid attackerActorId, Actor attacker, Actor target, int damage)
        {
            if (worldState == null)
            {
                throw new ArgumentNullException(nameof(worldState));
            }

            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            var resolvedDamage = Math.Max(0, damage);
            target.ReceiveDamage(resolvedDamage);
            if (attacker != null)
            {
                actorCombatService.MarkCombatParticipation(attacker.Id);
            }

            actorCombatService.MarkCombatParticipation(target.Id);
            eventBus.Publish(new CombatAttackOccurred(
                attackerActorId,
                target.Id,
                resolvedDamage,
                target.Hp));

            if (target.Hp <= 0)
            {
                defeatResolver.Resolve(worldState, attacker, target);
            }
        }
    }
}
