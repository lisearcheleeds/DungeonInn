using System;
using Cysharp.Threading.Tasks;
using VContainer;
using DungeonInn.Application.Actors.Ai;
using DungeonInn.Application.Actors.Equipment;
using DungeonInn.Application.Actors.Lifecycle;
using DungeonInn.Application.Actors.Movement;
using DungeonInn.Application.Actors.Profiles;
using DungeonInn.Application.Actors.Spawn;
using DungeonInn.Application.Dungeons;
using DungeonInn.Application.Economy;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Application.GameLoop;
using DungeonInn.Application.Facilities;
using DungeonInn.Application.Items;
using DungeonInn.Application.World;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Map;

namespace DungeonInn.Application.Combat
{
    public sealed class AdvanceCombatUseCase
    {
        readonly IActorCombatService actorCombatService;
        readonly IGameClock gameClock;
        readonly GameWorldFrameBuffer frameBuffer;
        readonly CombatEffectExecutor combatEffectExecutor;
        readonly ActorDefeatOrchestrator actorDefeatOrchestrator;
        readonly IEventPublisher eventPublisher;
        readonly ActorMovementService actorMovementService;
        readonly IWorldGameSettingsRepository worldGameSettingsRepository;
        readonly CombatEncounterTargetResolver targetResolver;

        [Inject]
        public AdvanceCombatUseCase(
            IActorCombatService actorCombatService,
            IGameClock gameClock,
            GameWorldFrameBuffer frameBuffer,
            CombatEffectExecutor combatEffectExecutor,
            ActorDefeatOrchestrator actorDefeatOrchestrator,
            IEventPublisher eventPublisher,
            ActorMovementService actorMovementService,
            IWorldGameSettingsRepository worldGameSettingsRepository,
            CombatEncounterTargetResolver targetResolver)
        {
            this.actorCombatService = actorCombatService
                ?? throw new ArgumentNullException(nameof(actorCombatService));
            this.gameClock = gameClock
                ?? throw new ArgumentNullException(nameof(gameClock));
            this.frameBuffer = frameBuffer
                ?? throw new ArgumentNullException(nameof(frameBuffer));
            this.combatEffectExecutor = combatEffectExecutor
                ?? throw new ArgumentNullException(nameof(combatEffectExecutor));
            this.actorDefeatOrchestrator = actorDefeatOrchestrator
                ?? throw new ArgumentNullException(nameof(actorDefeatOrchestrator));
            this.eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
            this.actorMovementService = actorMovementService
                ?? throw new ArgumentNullException(nameof(actorMovementService));
            this.worldGameSettingsRepository = worldGameSettingsRepository
                ?? throw new ArgumentNullException(nameof(worldGameSettingsRepository));
            this.targetResolver = targetResolver ?? throw new ArgumentNullException(nameof(targetResolver));
        }

        public UniTask ExecuteAsync(IGameWorldState worldState, float deltaGameSeconds)
        {
            if (worldState == null)
            {
                throw new ArgumentNullException(nameof(worldState));
            }

            var currentGameTimeSeconds = gameClock.ElapsedGameTimeSeconds;
            var actors = frameBuffer.CopyActors(worldState.Actors);
            var bufferedEventPublisher = new BufferedEventPublisher(eventPublisher);

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
                    EndEncounter(actor.Id, bufferedEventPublisher);
                    continue;
                }

                if (target.Position.LayerId.Equals(MapLayerId.Ground) ||
                    !target.Position.LayerId.Equals(actor.Position.LayerId))
                {
                    EndEncounter(actor.Id, bufferedEventPublisher);
                    continue;
                }

                var hasLineOfSight = targetResolver.HasLineOfSight(worldState.Dungeon, actor, target);
                if (!hasLineOfSight)
                {
                    var moved = MoveTowardTarget(
                        worldState,
                        actor,
                        target,
                        deltaGameSeconds,
                        0f,
                        out var arrivedAtTarget);
                    if (arrivedAtTarget || !moved)
                    {
                        EndEncounter(actor.Id, bufferedEventPublisher);
                    }

                    continue;
                }

                if (!IsWithinWeaponRange(actor, target))
                {
                    MoveTowardTarget(
                        worldState,
                        actor,
                        target,
                        deltaGameSeconds,
                        actor.WeaponCombatParams.RangeMeters,
                        out _);

                    continue;
                }

                if (!combatState.IsAttackReady(currentGameTimeSeconds))
                {
                    continue;
                }

                var targetDefeated = combatEffectExecutor.ExecuteAttack(
                    worldState,
                    actor,
                    target,
                    actor.WeaponCombatParams.AttackSpec,
                    bufferedEventPublisher);
                if (targetDefeated && worldState.FindActor(target.Id) != null)
                {
                    actorDefeatOrchestrator.Execute(worldState, actor, target, bufferedEventPublisher);
                }

                combatState.RecordAttack(currentGameTimeSeconds, actor.WeaponCombatParams.AttackIntervalSeconds);
                actorCombatService.MarkCombatParticipation(actor.Id);
            }

            bufferedEventPublisher.Flush();
            return UniTask.CompletedTask;
        }

        bool MoveTowardTarget(
            IGameWorldState worldState,
            Actor actor,
            Actor target,
            float deltaGameSeconds,
            float arrivalDistanceMeters,
            out bool arrived)
        {
            if (!actor.Position.LayerId.Equals(target.Position.LayerId))
            {
                arrived = false;
                return false;
            }

            if (actor.Position.LayerId.Equals(MapLayerId.Ground))
            {
                arrived = false;
                return false;
            }

            var previousPosition = actor.Position;
            var floor = worldState.Dungeon.GetFloor(actor.Position.LayerId.Value);
            arrived = actorMovementService.MoveToward(
                actor,
                target.Position,
                floor.Layer,
                floor,
                worldGameSettingsRepository.GetActorSimulationSettings().MoveSpeedMetersPerSecond,
                deltaGameSeconds,
                arrivalDistanceMeters,
                snapToDestinationOnArrival: false);
            return 0.0001f < previousPosition.DistanceSquaredTo(actor.Position);
        }

        void EndEncounter(Guid actorId, IEventPublisher publisher)
        {
            if (!actorCombatService.HasTarget(actorId))
            {
                return;
            }

            actorCombatService.ClearTarget(actorId);
            publisher.Publish(new CombatEncounterEnded(actorId));
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
    }
}
