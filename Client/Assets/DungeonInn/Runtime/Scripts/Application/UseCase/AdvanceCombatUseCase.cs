using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DungeonInn.Application.Combat;
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

        [Inject]
        public AdvanceCombatUseCase(IActorCombatService actorCombatService, IGameClock gameClock)
        {
            this.actorCombatService = actorCombatService
                ?? throw new ArgumentNullException(nameof(actorCombatService));
            this.gameClock = gameClock
                ?? throw new ArgumentNullException(nameof(gameClock));
        }

        public UniTask<AdvanceCombatResult> ExecuteAsync(IGameWorldState worldState, float deltaGameSeconds)
        {
            if (worldState == null) throw new ArgumentNullException(nameof(worldState));

            var currentGameTimeSeconds = gameClock.ElapsedGameTimeSeconds;
            var attacks = new List<CombatAttackEvent>();
            var deaths = new List<CombatDeathEvent>();
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

                var damage = CalculateDirectDamage(actor.WeaponCombatParams.AttackSpec);
                target.ReceiveDamage(damage);
                combatState.RecordAttack(currentGameTimeSeconds, actor.WeaponCombatParams.AttackIntervalSeconds);

                attacks.Add(new CombatAttackEvent(
                    actor.Id,
                    actor.Name,
                    target.Id,
                    target.Name,
                    damage,
                    target.Hp));

                if (target.Hp <= 0)
                {
                    worldState.RemoveActor(target.Id);
                    actorCombatService.ClearTargetsReferencing(target.Id);
                    actorCombatService.RemoveState(target.Id);
                    deaths.Add(new CombatDeathEvent(target.Id, target.Name));
                }
            }

            return UniTask.FromResult(new AdvanceCombatResult(attacks, deaths));
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
