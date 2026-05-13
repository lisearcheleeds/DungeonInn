using System;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Application.GameLoop;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Combat;
using DungeonInn.Domain.Map;
using VContainer;

namespace DungeonInn.Application.Combat
{
    public sealed class CombatEffectExecutor
    {
        readonly IEventPublisher eventBus;
        readonly CombatDamageResolver damageResolver;

        [Inject]
        public CombatEffectExecutor(
            IEventPublisher eventBus,
            CombatDamageResolver damageResolver)
        {
            this.eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            this.damageResolver = damageResolver ?? throw new ArgumentNullException(nameof(damageResolver));
        }

        public bool ExecuteAttack(
            IGameWorldState worldState,
            Actor attacker,
            Actor target,
            WeaponAttackSpec attackSpec)
        {
            if (worldState == null)
            {
                throw new ArgumentNullException(nameof(worldState));
            }

            if (attacker == null)
            {
                throw new ArgumentNullException(nameof(attacker));
            }

            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (attackSpec == null)
            {
                throw new ArgumentNullException(nameof(attackSpec));
            }

            var executionId = CombatEffectExecutionId.New();
            var targetDefeated = false;
            foreach (var rootNodeId in attackSpec.RootNodeIds)
            {
                targetDefeated |= ExecuteNode(
                    worldState,
                    attacker,
                    target,
                    target.Position,
                    attackSpec,
                    FindNode(attackSpec, rootNodeId),
                    executionId);
            }

            return targetDefeated;
        }

        public bool ExecuteProjectileHit(
            IGameWorldState worldState,
            ProjectileInstance projectile,
            Actor target)
        {
            if (worldState == null)
            {
                throw new ArgumentNullException(nameof(worldState));
            }

            if (projectile == null)
            {
                throw new ArgumentNullException(nameof(projectile));
            }

            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            var attacker = worldState.FindActor(projectile.AttackerActorId);
            eventBus.Publish(new ProjectileHit(projectile.Id, projectile.AttackerActorId, target.Id, projectile.Damage));
            if (projectile.AttackSpec == null || projectile.SourceNodeId <= 0)
            {
                return damageResolver.ApplyDamage(
                    worldState,
                    projectile.AttackerActorId,
                    attacker,
                    target,
                    projectile.Damage);
            }

            return ExecuteLinks(
                worldState,
                attacker,
                projectile.AttackerActorId,
                target,
                projectile.Position,
                projectile.AttackSpec,
                FindNode(projectile.AttackSpec, projectile.SourceNodeId),
                CombatEffectTriggerType.OnHit,
                projectile.ExecutionId);
        }

        public bool ExecuteAreaHit(
            IGameWorldState worldState,
            AreaEffectInstance areaEffect,
            Actor target)
        {
            if (worldState == null)
            {
                throw new ArgumentNullException(nameof(worldState));
            }

            if (areaEffect == null)
            {
                throw new ArgumentNullException(nameof(areaEffect));
            }

            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            var attacker = worldState.FindActor(areaEffect.AttackerActorId);
            eventBus.Publish(new AreaEffectHit(areaEffect.Id, areaEffect.AttackerActorId, target.Id, areaEffect.Damage));
            if (areaEffect.AttackSpec == null || areaEffect.SourceNodeId <= 0)
            {
                return damageResolver.ApplyDamage(
                    worldState,
                    areaEffect.AttackerActorId,
                    attacker,
                    target,
                    areaEffect.Damage);
            }

            return ExecuteLinks(
                worldState,
                attacker,
                areaEffect.AttackerActorId,
                target,
                target.Position,
                areaEffect.AttackSpec,
                FindNode(areaEffect.AttackSpec, areaEffect.SourceNodeId),
                CombatEffectTriggerType.OnHit,
                areaEffect.ExecutionId);
        }

        bool ExecuteLinks(
            IGameWorldState worldState,
            Actor attacker,
            Guid attackerActorId,
            Actor target,
            LayerPosition effectPosition,
            WeaponAttackSpec attackSpec,
            CombatEffectNodeSpec sourceNode,
            CombatEffectTriggerType triggerType,
            CombatEffectExecutionId executionId)
        {
            var targetDefeated = false;
            foreach (var link in sourceNode.Links)
            {
                if (link.TriggerType != triggerType)
                {
                    continue;
                }

                targetDefeated |= ExecuteNode(
                    worldState,
                    attacker,
                    attackerActorId,
                    target,
                    effectPosition,
                    attackSpec,
                    FindNode(attackSpec, link.TargetNodeId),
                    executionId);
            }

            return targetDefeated;
        }

        bool ExecuteNode(
            IGameWorldState worldState,
            Actor attacker,
            Actor target,
            LayerPosition effectPosition,
            WeaponAttackSpec attackSpec,
            CombatEffectNodeSpec node,
            CombatEffectExecutionId executionId)
        {
            return ExecuteNode(worldState, attacker, attacker.Id, target, effectPosition, attackSpec, node, executionId);
        }

        bool ExecuteNode(
            IGameWorldState worldState,
            Actor attacker,
            Guid attackerActorId,
            Actor target,
            LayerPosition effectPosition,
            WeaponAttackSpec attackSpec,
            CombatEffectNodeSpec node,
            CombatEffectExecutionId executionId)
        {
            switch (node.Type)
            {
                case CombatEffectNodeType.DirectDamage:
                    return damageResolver.ApplyDamage(
                        worldState,
                        attackerActorId,
                        attacker,
                        target,
                        node.DamageSpec.Amount);
                case CombatEffectNodeType.Projectile:
                    CreateProjectile(worldState, attackerActorId, target, effectPosition, attackSpec, node, executionId);
                    return false;
                case CombatEffectNodeType.Area:
                    CreateAreaEffect(worldState, attackerActorId, attacker, target, effectPosition, attackSpec, node, executionId);
                    return false;
                case CombatEffectNodeType.ApplyStatus:
                    throw new InvalidOperationException("ApplyStatus combat effect node is not supported yet.");
                default:
                    throw new ArgumentOutOfRangeException(nameof(node));
            }
        }

        void CreateProjectile(
            IGameWorldState worldState,
            Guid attackerActorId,
            Actor target,
            LayerPosition effectPosition,
            WeaponAttackSpec attackSpec,
            CombatEffectNodeSpec node,
            CombatEffectExecutionId executionId)
        {
            var projectile = new ProjectileInstance(
                Guid.NewGuid(),
                attackerActorId,
                target.Id,
                effectPosition,
                target.Position,
                attackSpec,
                node.Id,
                executionId,
                CalculateLinkedDirectDamage(attackSpec, node, CombatEffectTriggerType.OnHit),
                node.ProjectileSpec.SpeedMetersPerSecond,
                node.ProjectileSpec.MaxDistanceMeters);
            worldState.AddProjectile(projectile);
            eventBus.Publish(new ProjectileFired(projectile.Id, attackerActorId, target.Id));
        }

        void CreateAreaEffect(
            IGameWorldState worldState,
            Guid attackerActorId,
            Actor attacker,
            Actor target,
            LayerPosition effectPosition,
            WeaponAttackSpec attackSpec,
            CombatEffectNodeSpec node,
            CombatEffectExecutionId executionId)
        {
            var sourceFactionId = attacker == null ? target.Faction.Id : attacker.Faction.Id;
            var areaEffect = new AreaEffectInstance(
                Guid.NewGuid(),
                attackerActorId,
                sourceFactionId,
                effectPosition,
                attackSpec,
                node.Id,
                executionId,
                node.AreaSpec,
                CalculateLinkedDirectDamage(attackSpec, node, CombatEffectTriggerType.OnHit));
            worldState.AddAreaEffect(areaEffect);
            eventBus.Publish(new AreaEffectCreated(
                areaEffect.Id,
                attackerActorId,
                areaEffect.CenterPosition,
                areaEffect.AreaSpec.RadiusMeters));
        }

        static int CalculateLinkedDirectDamage(
            WeaponAttackSpec attackSpec,
            CombatEffectNodeSpec sourceNode,
            CombatEffectTriggerType triggerType)
        {
            var damage = 0;
            foreach (var link in sourceNode.Links)
            {
                if (link.TriggerType != triggerType)
                {
                    continue;
                }

                var node = FindNode(attackSpec, link.TargetNodeId);
                if (node.Type == CombatEffectNodeType.DirectDamage)
                {
                    damage += node.DamageSpec.Amount;
                }
            }

            return damage;
        }

        static CombatEffectNodeSpec FindNode(WeaponAttackSpec attackSpec, int nodeId)
        {
            foreach (var node in attackSpec.Nodes)
            {
                if (node.Id == nodeId)
                {
                    return node;
                }
            }

            throw new InvalidOperationException("Combat effect node does not exist.");
        }
    }
}
