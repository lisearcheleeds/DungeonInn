using System;
using Cysharp.Threading.Tasks;
using DungeonInn.Application.Combat;
using DungeonInn.Application.Event;
using DungeonInn.Application.GameLoop;
using DungeonInn.Application.Actors.Ai;
using DungeonInn.Application.Actors.Lifecycle;
using DungeonInn.Application.Actors.Movement;
using DungeonInn.Application.Actors.Profiles;
using DungeonInn.Application.Actors.Spawn;

using DungeonInn.Application.Dungeons;
using DungeonInn.Application.Economy;
using DungeonInn.Application.Facilities;
using DungeonInn.Application.Items;
using DungeonInn.Application.World;
using DungeonInn.Domain.Combat;
using VContainer;

namespace DungeonInn.Application.Combat
{
    public sealed class AdvanceProjectileUseCase
    {
        readonly CombatEffectExecutor combatEffectExecutor;
        readonly ActorDefeatOrchestrator actorDefeatOrchestrator;
        readonly IEventPublisher eventPublisher;
        readonly IWorldGameSettingsRepository worldGameSettingsRepository;

        [Inject]
        public AdvanceProjectileUseCase(
            CombatEffectExecutor combatEffectExecutor,
            ActorDefeatOrchestrator actorDefeatOrchestrator,
            IEventPublisher eventPublisher,
            IWorldGameSettingsRepository worldGameSettingsRepository)
        {
            this.combatEffectExecutor = combatEffectExecutor ?? throw new ArgumentNullException(nameof(combatEffectExecutor));
            this.actorDefeatOrchestrator = actorDefeatOrchestrator
                ?? throw new ArgumentNullException(nameof(actorDefeatOrchestrator));
            this.eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
            this.worldGameSettingsRepository = worldGameSettingsRepository
                ?? throw new ArgumentNullException(nameof(worldGameSettingsRepository));
        }

        public UniTask ExecuteAsync(IGameWorldState worldState, float deltaGameSeconds)
        {
            if (worldState == null)
            {
                throw new ArgumentNullException(nameof(worldState));
            }

            var bufferedEventPublisher = new BufferedEventPublisher(eventPublisher);
            for (var i = worldState.Projectiles.Count - 1; 0 <= i; i--)
            {
                var projectile = worldState.Projectiles[i];
                if (!AdvanceProjectile(worldState, projectile, deltaGameSeconds, bufferedEventPublisher))
                {
                    worldState.RemoveProjectile(projectile.Id);
                }
            }

            bufferedEventPublisher.Flush();
            return UniTask.CompletedTask;
        }

        bool AdvanceProjectile(
            IGameWorldState worldState,
            ProjectileInstance projectile,
            float deltaGameSeconds,
            IEventPublisher eventPublisher)
        {
            var target = worldState.FindActor(projectile.TargetActorId);
            if (target == null || target.Hp <= 0)
            {
                return false;
            }

            var combatBalanceSettings = worldGameSettingsRepository.GetCombatBalanceSettings();
            projectile.Advance(deltaGameSeconds);
            if (combatBalanceSettings.ProjectileHitRadiusMeters * combatBalanceSettings.ProjectileHitRadiusMeters
                < projectile.Position.DistanceSquaredTo(target.Position))
            {
                return 0f < projectile.RemainingDistanceMeters;
            }

            var targetDefeated = combatEffectExecutor.ExecuteProjectileHit(worldState, projectile, target, eventPublisher);
            if (targetDefeated && worldState.FindActor(target.Id) != null)
            {
                actorDefeatOrchestrator.Execute(
                    worldState,
                    worldState.FindActor(projectile.AttackerActorId),
                    target,
                    eventPublisher);
            }

            return false;
        }
    }
}
