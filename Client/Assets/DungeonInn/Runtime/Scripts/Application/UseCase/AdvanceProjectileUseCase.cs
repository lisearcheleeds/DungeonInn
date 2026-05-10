using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DungeonInn.Application.Combat;
using DungeonInn.Application.GameLoop;
using DungeonInn.Domain.Combat;
using DungeonInn.Domain.Common;
using VContainer;

namespace DungeonInn.Application.UseCase
{
    public sealed class AdvanceProjectileUseCase
    {
        readonly CombatEffectExecutor combatEffectExecutor;

        [Inject]
        public AdvanceProjectileUseCase(CombatEffectExecutor combatEffectExecutor)
        {
            this.combatEffectExecutor = combatEffectExecutor ?? throw new ArgumentNullException(nameof(combatEffectExecutor));
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

            combatEffectExecutor.ExecuteProjectileHit(worldState, projectile, target);
            return false;
        }
    }
}
