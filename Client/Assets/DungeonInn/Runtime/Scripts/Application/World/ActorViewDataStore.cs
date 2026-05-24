using System;
using System.Collections.Generic;
using DungeonInn.Domain.Actor;
using DungeonInn.Master;
using VContainer;

namespace DungeonInn.Application.World
{
    public sealed class ActorViewDataStore : IActorViewDataProvider, IActorStatusViewDataProvider, IActorSelectionCandidateProvider
    {
        readonly IMasterRepository masterRepository;
        readonly Dictionary<Guid, ActorViewData> actorViewDataById = new();
        readonly HashSet<Guid> dirtyActorIds = new();
        readonly HashSet<Guid> removedActorIdSet = new();
        readonly List<ActorViewData> changedActors = new();
        readonly List<Guid> removedActorIds = new();
        readonly HashSet<Guid> statusRemovedActorIdSet = new();
        readonly List<Guid> statusRemovedActors = new();

        [Inject]
        public ActorViewDataStore(IMasterRepository masterRepository)
        {
            this.masterRepository = masterRepository ?? throw new ArgumentNullException(nameof(masterRepository));
        }

        public void SyncActor(Actor actor)
        {
            if (actor == null)
            {
                throw new ArgumentNullException(nameof(actor));
            }

            var archetypeMaster = masterRepository.GetActorArchetypeMaster(actor.ArchetypeId);
            var viewData = new ActorViewData(
                actor.Id,
                actor.Position,
                ResolveBehaviorType(actor),
                archetypeMaster.VisualId);
            if (actorViewDataById.TryGetValue(actor.Id, out var current) && IsSame(current, viewData))
            {
                return;
            }

            actorViewDataById[actor.Id] = viewData;
            removedActorIdSet.Remove(actor.Id);
            dirtyActorIds.Add(actor.Id);
        }

        public void RemoveActor(Guid actorId)
        {
            if (!actorViewDataById.Remove(actorId))
            {
                return;
            }

            dirtyActorIds.Remove(actorId);
            removedActorIdSet.Add(actorId);
            statusRemovedActorIdSet.Add(actorId);
        }

        public ActorViewDataChangeBuffer ConsumeChanges()
        {
            changedActors.Clear();
            foreach (var actorId in dirtyActorIds)
            {
                if (actorViewDataById.TryGetValue(actorId, out var actor))
                {
                    changedActors.Add(actor);
                }
            }

            removedActorIds.Clear();
            foreach (var actorId in removedActorIdSet)
            {
                removedActorIds.Add(actorId);
            }

            dirtyActorIds.Clear();
            removedActorIdSet.Clear();
            return new ActorViewDataChangeBuffer(changedActors, removedActorIds);
        }

        public IReadOnlyList<Guid> ConsumeRemovedActorIds()
        {
            statusRemovedActors.Clear();
            foreach (var actorId in statusRemovedActorIdSet)
            {
                statusRemovedActors.Add(actorId);
            }

            statusRemovedActorIdSet.Clear();
            return statusRemovedActors;
        }

        public void CopyActiveActorsTo(List<ActorViewData> results)
        {
            if (results == null)
            {
                throw new ArgumentNullException(nameof(results));
            }

            results.Clear();
            foreach (var actor in actorViewDataById.Values)
            {
                results.Add(actor);
            }
        }

        public void CopySelectionCandidatesTo(List<ActorViewData> results)
        {
            CopyActiveActorsTo(results);
        }

        public void CopyActorIdsTo(List<Guid> results)
        {
            if (results == null)
            {
                throw new ArgumentNullException(nameof(results));
            }

            results.Clear();
            foreach (var id in actorViewDataById.Keys)
            {
                results.Add(id);
            }
        }

        static bool IsSame(ActorViewData first, ActorViewData second)
        {
            return first.ActorId.Equals(second.ActorId) &&
                first.Position.LayerId.Equals(second.Position.LayerId) &&
                first.Position.X.Equals(second.Position.X) &&
                first.Position.Z.Equals(second.Position.Z) &&
                first.BehaviorType == second.BehaviorType &&
                first.VisualId == second.VisualId;
        }

        static ActorBehaviorType ResolveBehaviorType(Actor actor)
        {
            if (actor.Behavior is AdventurerBehavior)
            {
                return ActorBehaviorType.Adventurer;
            }

            if (actor.Behavior is MonsterBehavior)
            {
                return ActorBehaviorType.Monster;
            }

            if (actor.Behavior is GuildStaffBehavior)
            {
                return ActorBehaviorType.GuildStaff;
            }

            if (actor.Behavior is PetBehavior)
            {
                return ActorBehaviorType.Pet;
            }

            return ActorBehaviorType.None;
        }
    }
}
