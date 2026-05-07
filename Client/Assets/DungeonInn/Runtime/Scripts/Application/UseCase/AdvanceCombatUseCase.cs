using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DungeonInn.Application.Combat;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Application.GameLoop;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Combat;
using DungeonInn.Domain.Map;
using VContainer;

namespace DungeonInn.Application.UseCase
{
    public sealed class AdvanceCombatUseCase
    {
        const float CombatApproachSpeedMetersPerSecond = 5.0f;

        readonly IActorCombatService actorCombatService;
        readonly IGameClock gameClock;
        readonly IGameEventBus eventBus;

        [Inject]
        public AdvanceCombatUseCase(
            IActorCombatService actorCombatService,
            IGameClock gameClock,
            IGameEventBus eventBus)
        {
            this.actorCombatService = actorCombatService
                ?? throw new ArgumentNullException(nameof(actorCombatService));
            this.gameClock = gameClock
                ?? throw new ArgumentNullException(nameof(gameClock));
            this.eventBus = eventBus
                ?? throw new ArgumentNullException(nameof(eventBus));
        }

        public UniTask ExecuteAsync(IGameWorldState worldState, float deltaGameSeconds)
        {
            if (worldState == null) throw new ArgumentNullException(nameof(worldState));

            var currentGameTimeSeconds = gameClock.ElapsedGameTimeSeconds;
            var actors = new List<Actor>(worldState.Actors);

            foreach (var actor in actors)
            {
                if (actor.Hp <= 0 || FindActor(worldState, actor.Id) == null)
                {
                    continue;
                }

                var combatState = actorCombatService.GetOrCreateCombatState(actor.Id);
                if (!combatState.TargetActorId.HasValue)
                {
                    continue;
                }

                var target = FindActor(worldState, combatState.TargetActorId.Value);
                if (target == null || target.Hp <= 0)
                {
                    combatState.ClearTarget();
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

                // TODO: Area・Projectile 攻撃の実装時に CombatEffectExecutor.Execute(attackSpec, actor, targets) へ置き換える。
                // 現在は WeaponAttackSpec の Node グラフを解釈せず、DirectDamage 相当の値を直接計算している暫定実装。
                var damage = CalculateDirectDamage(actor.WeaponCombatParams.AttackSpec);
                target.ReceiveDamage(damage);
                combatState.RecordAttack(currentGameTimeSeconds, actor.WeaponCombatParams.AttackIntervalSeconds);

                eventBus.Publish(new CombatAttackOccurred(
                    actor.Id,
                    target.Id,
                    damage,
                    target.Hp));

                if (target.Hp <= 0)
                {
                    foreach (var witness in actors)
                    {
                        var witnessCombatState = actorCombatService.GetOrCreateCombatState(witness.Id);
                        if (witnessCombatState.TargetActorId.HasValue && witnessCombatState.TargetActorId.Value.Equals(target.Id))
                        {
                            eventBus.Publish(new CombatEncounterEnded(witness.Id));
                        }
                    }

                    worldState.RemoveActor(target.Id);
                    actorCombatService.ClearTargetsReferencing(target.Id);
                    actorCombatService.RemoveState(target.Id);
                    eventBus.Publish(new ActorDefeated(target.Id, actor.Id, DeathCause.Combat));
                }
            }

            return UniTask.CompletedTask;
        }

        static Actor FindActor(IGameWorldState worldState, Guid actorId)
        {
            foreach (var actor in worldState.Actors)
            {
                if (actor.Id.Equals(actorId))
                {
                    return actor;
                }
            }

            return null;
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
            var step = Math.Min(dist, CombatApproachSpeedMetersPerSecond * deltaGameSeconds);
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
