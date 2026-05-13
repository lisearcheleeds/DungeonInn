using DungeonInn.Application.World;
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
        readonly IEventPublisher eventBus;

        [Inject]
        public CombatDamageResolver(
            IActorCombatService actorCombatService,
            IEventPublisher eventBus)
        {
            this.actorCombatService = actorCombatService ?? throw new ArgumentNullException(nameof(actorCombatService));
            this.eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        public bool ApplyDamage(IGameWorldState worldState, Actor attacker, Actor target, int damage)
        {
            if (worldState == null)
            {
                throw new ArgumentNullException(nameof(worldState));
            }

            if (attacker == null)
            {
                throw new ArgumentNullException(nameof(attacker));
            }

            return ApplyDamage(worldState, attacker.Id, attacker, target, damage, eventBus);
        }

        public bool ApplyDamage(IGameWorldState worldState, Guid attackerActorId, Actor attacker, Actor target, int damage)
        {
            return ApplyDamage(worldState, attackerActorId, attacker, target, damage, eventBus);
        }

        public bool ApplyDamage(
            IGameWorldState worldState,
            Guid attackerActorId,
            Actor attacker,
            Actor target,
            int damage,
            IEventPublisher eventPublisher)
        {
            if (worldState == null)
            {
                throw new ArgumentNullException(nameof(worldState));
            }

            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (eventPublisher == null)
            {
                throw new ArgumentNullException(nameof(eventPublisher));
            }

            var resolvedDamage = Math.Max(0, damage);
            target.ReceiveDamage(resolvedDamage);
            if (attacker != null)
            {
                actorCombatService.MarkCombatParticipation(attacker.Id);
            }

            actorCombatService.MarkCombatParticipation(target.Id);
            eventPublisher.Publish(new CombatAttackOccurred(
                attackerActorId,
                target.Id,
                resolvedDamage,
                target.Hp));

            return target.Hp <= 0;
        }
    }
}
