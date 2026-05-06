using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DungeonInn.Application.Combat;
using DungeonInn.Application.GameLoop;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Map;
using VContainer;

namespace DungeonInn.Application.UseCase
{
    public sealed class DetectCombatEncounterUseCase
    {
        const float EncounterRangeMeters = 20f;

        readonly IActorCombatService actorCombatService;

        [Inject]
        public DetectCombatEncounterUseCase(IActorCombatService actorCombatService)
        {
            this.actorCombatService = actorCombatService
                ?? throw new ArgumentNullException(nameof(actorCombatService));
        }

        public UniTask ExecuteAsync(IGameWorldState worldState)
        {
            if (worldState == null) throw new ArgumentNullException(nameof(worldState));

            var actors = worldState.Actors;

            foreach (var actor in actors)
            {
                if (actor.Position.LayerId.Equals(MapLayerId.Ground))
                {
                    continue;
                }

                var combatState = actorCombatService.GetOrCreateCombatState(actor.Id);
                var nearest = FindNearestHostile(actor, actors);

                if (nearest != null)
                {
                    combatState.SetTarget(nearest.Id);
                }
                else
                {
                    combatState.ClearTarget();
                }
            }

            return UniTask.CompletedTask;
        }

        static Actor FindNearestHostile(Actor actor, IReadOnlyList<Actor> actors)
        {
            Actor nearest = null;
            var nearestDistSq = EncounterRangeMeters * EncounterRangeMeters;

            foreach (var candidate in actors)
            {
                if (candidate.Id == actor.Id)
                {
                    continue;
                }

                if (!candidate.Position.LayerId.Equals(actor.Position.LayerId))
                {
                    continue;
                }

                if (!AreHostile(actor.Faction, candidate.Faction))
                {
                    continue;
                }

                var distSq = actor.Position.DistanceSquaredTo(candidate.Position);
                if (distSq <= nearestDistSq)
                {
                    nearestDistSq = distSq;
                    nearest = candidate;
                }
            }

            return nearest;
        }

        // TODO: FactionMasterから取得する
        static bool AreHostile(ActorFaction a, ActorFaction b)
        {
            return (a.Id == 1 && b.Id == 2) || (a.Id == 2 && b.Id == 1);
        }
    }
}
