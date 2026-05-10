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
using DungeonInn.Domain.Map;
using VContainer;

namespace DungeonInn.Application.UseCase
{
    public sealed class AdvanceCombatUseCase
    {
        readonly IActorCombatService actorCombatService;
        readonly IGameClock gameClock;
        readonly IGameEventBus eventBus;
        readonly GrantExperienceUseCase grantExperienceUseCase;
        readonly DropItemUseCase dropItemUseCase;

        [Inject]
        public AdvanceCombatUseCase(
            IActorCombatService actorCombatService,
            IGameClock gameClock,
            IGameEventBus eventBus,
            GrantExperienceUseCase grantExperienceUseCase,
            DropItemUseCase dropItemUseCase)
        {
            this.actorCombatService = actorCombatService
                ?? throw new ArgumentNullException(nameof(actorCombatService));
            this.gameClock = gameClock
                ?? throw new ArgumentNullException(nameof(gameClock));
            this.eventBus = eventBus
                ?? throw new ArgumentNullException(nameof(eventBus));
            this.grantExperienceUseCase = grantExperienceUseCase
                ?? throw new ArgumentNullException(nameof(grantExperienceUseCase));
            this.dropItemUseCase = dropItemUseCase
                ?? throw new ArgumentNullException(nameof(dropItemUseCase));
        }

        public UniTask ExecuteAsync(IGameWorldState worldState, float deltaGameSeconds)
        {
            if (worldState == null)
            {
                throw new ArgumentNullException(nameof(worldState));
            }

            var currentGameTimeSeconds = gameClock.ElapsedGameTimeSeconds;
            var actors = new List<Actor>(worldState.Actors);

            foreach (var actor in actors)
            {
                if (actor.Hp <= 0 || worldState.FindActor(actor.Id) == null)
                {
                    continue;
                }

                var combatState = actorCombatService.GetOrCreateCombatState(actor.Id);
                if (!combatState.TargetActorId.HasValue)
                {
                    continue;
                }

                var target = worldState.FindActor(combatState.TargetActorId.Value);
                if (target == null || target.Hp <= 0)
                {
                    actorCombatService.ClearTarget(actor.Id);
                    continue;
                }

                if (!IsWithinWeaponRange(actor, target))
                {
                    MoveTowardTarget(actor, target, deltaGameSeconds);
                    continue;
                }

                if (!combatState.IsAttackReady(currentGameTimeSeconds))
                {
                    continue;
                }

                if (TryCreateProjectile(actor, target, actor.WeaponCombatParams.AttackSpec, out var projectile))
                {
                    worldState.AddProjectile(projectile);
                    combatState.RecordAttack(currentGameTimeSeconds, actor.WeaponCombatParams.AttackIntervalSeconds);
                    actorCombatService.MarkCombatParticipation(actor.Id);
                    actorCombatService.MarkCombatParticipation(target.Id);
                    eventBus.Publish(new ProjectileFired(projectile.Id, actor.Id, target.Id));
                    continue;
                }

                if (TryCreateAreaEffect(actor, target, actor.WeaponCombatParams.AttackSpec, out var areaEffect))
                {
                    worldState.AddAreaEffect(areaEffect);
                    combatState.RecordAttack(currentGameTimeSeconds, actor.WeaponCombatParams.AttackIntervalSeconds);
                    actorCombatService.MarkCombatParticipation(actor.Id);
                    eventBus.Publish(new AreaEffectCreated(
                        areaEffect.Id,
                        actor.Id,
                        areaEffect.CenterPosition,
                        areaEffect.AreaSpec.RadiusMeters));
                    continue;
                }

                var damage = CalculateDirectDamage(actor.WeaponCombatParams.AttackSpec);
                target.ReceiveDamage(damage);
                combatState.RecordAttack(currentGameTimeSeconds, actor.WeaponCombatParams.AttackIntervalSeconds);
                actorCombatService.MarkCombatParticipation(actor.Id);
                actorCombatService.MarkCombatParticipation(target.Id);

                eventBus.Publish(new CombatAttackOccurred(
                    actor.Id,
                    target.Id,
                    damage,
                    target.Hp));

                if (target.Hp <= 0)
                {
                    foreach (var attackerId in actorCombatService.GetAttackers(target.Id))
                    {
                        eventBus.Publish(new CombatEncounterEnded(attackerId));
                    }

                    grantExperienceUseCase.Execute(actor, target);
                    dropItemUseCase.Execute(target, worldState);
                    worldState.RemoveActor(target.Id);
                    actorCombatService.ClearTargetsReferencing(target.Id);
                    actorCombatService.RemoveState(target.Id);
                    eventBus.Publish(new ActorDefeated(target.Id, actor.Id, DeathCause.Combat));
                }
            }

            return UniTask.CompletedTask;
        }

        static bool TryCreateProjectile(
            Actor actor,
            Actor target,
            WeaponAttackSpec attackSpec,
            out ProjectileInstance projectile)
        {
            foreach (var rootNodeId in attackSpec.RootNodeIds)
            {
                var node = FindNode(attackSpec, rootNodeId);
                if (node.Type != CombatEffectNodeType.Projectile)
                {
                    continue;
                }

                var damage = CalculateLinkedDirectDamage(attackSpec, node, CombatEffectTriggerType.OnHit);
                projectile = new ProjectileInstance(
                    Guid.NewGuid(),
                    actor.Id,
                    target.Id,
                    actor.Position,
                    target.Position,
                    damage,
                    node.ProjectileSpec.SpeedMetersPerSecond,
                    node.ProjectileSpec.MaxDistanceMeters);
                return true;
            }

            projectile = null;
            return false;
        }

        static bool TryCreateAreaEffect(
            Actor actor,
            Actor target,
            WeaponAttackSpec attackSpec,
            out AreaEffectInstance areaEffect)
        {
            foreach (var rootNodeId in attackSpec.RootNodeIds)
            {
                var node = FindNode(attackSpec, rootNodeId);
                if (node.Type != CombatEffectNodeType.Area)
                {
                    continue;
                }

                var damage = CalculateLinkedDirectDamage(attackSpec, node, CombatEffectTriggerType.OnHit);
                areaEffect = new AreaEffectInstance(
                    Guid.NewGuid(),
                    actor.Id,
                    actor.Faction.Id,
                    target.Position,
                    node.AreaSpec,
                    damage);
                return true;
            }

            areaEffect = null;
            return false;
        }

        static void MoveTowardTarget(Actor actor, Actor target, float deltaGameSeconds)
        {
            if (!actor.Position.LayerId.Equals(target.Position.LayerId))
            {
                return;
            }

            var dx = target.Position.X - actor.Position.X;
            var dz = target.Position.Z - actor.Position.Z;
            var distSq = dx * dx + dz * dz;
            if (distSq <= 0f)
            {
                return;
            }

            var dist = (float)Math.Sqrt(distSq);
            var step = Math.Min(dist, GameConstants.ActorMoveSpeedMetersPerSecond * deltaGameSeconds);
            var ratio = step / dist;
            actor.MoveTo(new LayerPosition(
                actor.Position.LayerId,
                actor.Position.X + dx * ratio,
                actor.Position.Z + dz * ratio));
        }

        static bool IsWithinWeaponRange(Actor actor, Actor target)
        {
            if (!actor.Position.LayerId.Equals(target.Position.LayerId))
            {
                return false;
            }

            var range = actor.WeaponCombatParams.RangeMeters;
            return actor.Position.DistanceSquaredTo(target.Position) <= range * range;
        }

        // TODO: このメソッドは CombatEffectExecutor が実装されたら不要になる。
        // Area（範囲攻撃）や Projectile（飛翔物）を実装する際、WeaponAttackSpec の Node グラフを
        // CombatEffectExecutor が解釈・実行する設計に移行する（combat-domain-design.md 参照）。
        // その時点でターゲット単数前提のこの呼び出し構造ごと置き換える。
        static int CalculateDirectDamage(WeaponAttackSpec attackSpec)
        {
            var damage = 0;
            foreach (var rootNodeId in attackSpec.RootNodeIds)
            {
                var node = FindNode(attackSpec, rootNodeId);
                if (node.Type != CombatEffectNodeType.DirectDamage)
                {
                    throw new InvalidOperationException("Only direct damage root nodes are currently supported by combat execution.");
                }

                damage += node.DamageSpec.Amount;
            }

            return damage;
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
                if (node.Type != CombatEffectNodeType.DirectDamage)
                {
                    throw new InvalidOperationException("Only direct damage combat effect links are currently supported.");
                }

                damage += node.DamageSpec.Amount;
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

            throw new InvalidOperationException("Combat effect root node does not exist.");
        }
    }
}
