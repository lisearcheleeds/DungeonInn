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

        [Inject]
        public CombatDamageResolver(IActorCombatService actorCombatService)
        {
            this.actorCombatService = actorCombatService ?? throw new ArgumentNullException(nameof(actorCombatService));
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
