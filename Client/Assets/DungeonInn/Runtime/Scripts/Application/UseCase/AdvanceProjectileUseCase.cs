using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DungeonInn.Application.Combat;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Application.GameLoop;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Combat;
using DungeonInn.Domain.Common;
using VContainer;

namespace DungeonInn.Application.UseCase
{
    public sealed class AdvanceProjectileUseCase
    {
        readonly IActorCombatService actorCombatService;
        readonly IGameEventBus eventBus;
        readonly GrantExperienceUseCase grantExperienceUseCase;
        readonly DropItemUseCase dropItemUseCase;

        [Inject]
        public AdvanceProjectileUseCase(
            IActorCombatService actorCombatService,
            IGameEventBus eventBus,
            GrantExperienceUseCase grantExperienceUseCase,
            DropItemUseCase dropItemUseCase)
        {
            this.actorCombatService = actorCombatService ?? throw new ArgumentNullException(nameof(actorCombatService));
            this.eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            this.grantExperienceUseCase = grantExperienceUseCase ?? throw new ArgumentNullException(nameof(grantExperienceUseCase));
            this.dropItemUseCase = dropItemUseCase ?? throw new ArgumentNullException(nameof(dropItemUseCase));
        }

        public UniTask ExecuteAsync(IGameWorldState worldState, float deltaGameSeconds)
        {
            if (worldState == null)
            {
                throw new ArgumentNullException(nameof(worldState));
            }

            var projectiles = new List<ProjectileInstance>(worldState.Projectiles);
            foreach (var projectile in projectiles)
            {
                if (!AdvanceProjectile(worldState, projectile, deltaGameSeconds))
                {
                    worldState.RemoveProjectile(projectile.Id);
                }
            }

            return UniTask.CompletedTask;
        }

        bool AdvanceProjectile(IGameWorldState worldState, ProjectileInstance projectile, float deltaGameSeconds)
        {
            var target = worldState.FindActor(projectile.TargetActorId);
            if (target == null || target.Hp <= 0)
            {
                return false;
            }

            projectile.Advance(deltaGameSeconds);
            if (GameConstants.ProjectileHitRadiusMeters * GameConstants.ProjectileHitRadiusMeters
                < projectile.Position.DistanceSquaredTo(target.Position))
            {
                return 0f < projectile.RemainingDistanceMeters;
            }

            var attacker = worldState.FindActor(projectile.AttackerActorId);
            target.ReceiveDamage(projectile.Damage);
            eventBus.Publish(new ProjectileHit(projectile.Id, projectile.AttackerActorId, target.Id, projectile.Damage));
            eventBus.Publish(new CombatAttackOccurred(
                projectile.AttackerActorId,
                target.Id,
                projectile.Damage,
                target.Hp));

            if (attacker != null)
            {
                actorCombatService.MarkCombatParticipation(attacker.Id);
            }

            actorCombatService.MarkCombatParticipation(target.Id);
            if (target.Hp <= 0)
            {
                ResolveDefeat(worldState, attacker, target);
            }

            return false;
        }

        void ResolveDefeat(IGameWorldState worldState, Actor attacker, Actor target)
        {
            foreach (var attackerId in actorCombatService.GetAttackers(target.Id))
            {
                eventBus.Publish(new CombatEncounterEnded(attackerId));
            }

            if (attacker != null)
            {
                grantExperienceUseCase.Execute(attacker, target);
            }

            dropItemUseCase.Execute(target, worldState);
            worldState.RemoveActor(target.Id);
            actorCombatService.ClearTargetsReferencing(target.Id);
            actorCombatService.RemoveState(target.Id);
            eventBus.Publish(new ActorDefeated(target.Id, attacker?.Id, DeathCause.Combat));
        }
    }
}
