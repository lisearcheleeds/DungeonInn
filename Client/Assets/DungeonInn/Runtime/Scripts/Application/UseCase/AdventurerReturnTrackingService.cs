using System;
using System.Collections.Generic;
using R3;
using VContainer;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Application.Profiles;

namespace DungeonInn.Application.UseCase
{
    public sealed class AdventurerReturnTrackingService : IDisposable
    {
        readonly IActorProfileRegistry profileRegistry;
        readonly Dictionary<Guid, Dictionary<int, int>> defeatedMonsterCountsByActor = new();
        readonly HashSet<Guid> dirtyActorIds = new();
        DisposableBag bag;

        [Inject]
        public AdventurerReturnTrackingService(
            IEventSubscriber eventSubscriber,
            IActorProfileRegistry profileRegistry)
        {
            if (eventSubscriber == null)
            {
                throw new ArgumentNullException(nameof(eventSubscriber));
            }

            this.profileRegistry = profileRegistry ?? throw new ArgumentNullException(nameof(profileRegistry));

            eventSubscriber.OnEvent<ActorDefeated>()
                .Subscribe(OnActorDefeated)
                .AddTo(ref bag);
            eventSubscriber.OnEvent<CombatEncounterEnded>()
                .Subscribe(gameEvent => MarkDirty(gameEvent.ActorId))
                .AddTo(ref bag);
            eventSubscriber.OnEvent<ItemPickedUp>()
                .Subscribe(gameEvent => MarkDirty(gameEvent.ActorId))
                .AddTo(ref bag);
            eventSubscriber.OnEvent<ActorLeveledUp>()
                .Subscribe(gameEvent => MarkDirty(gameEvent.ActorId))
                .AddTo(ref bag);
            eventSubscriber.OnEvent<ActorEnteredDungeon>()
                .Subscribe(gameEvent => MarkDirty(gameEvent.ActorId))
                .AddTo(ref bag);
        }

        public bool HasDirtyActors => 0 < dirtyActorIds.Count;

        public bool IsDirty(Guid actorId)
        {
            return dirtyActorIds.Contains(actorId);
        }

        public void ClearDirty(Guid actorId)
        {
            dirtyActorIds.Remove(actorId);
        }

        public void RemoveMissingDirtyActors(HashSet<Guid> foundDirtyActorIds)
        {
            var dirtyIds = new List<Guid>(dirtyActorIds);
            foreach (var actorId in dirtyIds)
            {
                if (!foundDirtyActorIds.Contains(actorId))
                {
                    dirtyActorIds.Remove(actorId);
                }
            }
        }

        public int GetDefeatedMonsterCount(Guid actorId, int monsterSpeciesId)
        {
            if (!defeatedMonsterCountsByActor.TryGetValue(actorId, out var defeatedMonsterCounts))
            {
                return 0;
            }

            defeatedMonsterCounts.TryGetValue(monsterSpeciesId, out var count);
            return count;
        }

        void MarkDirty(Guid actorId)
        {
            dirtyActorIds.Add(actorId);
        }

        void OnActorDefeated(ActorDefeated gameEvent)
        {
            if (gameEvent.KillerActorId.HasValue)
            {
                MarkDirty(gameEvent.KillerActorId.Value);
            }

            if (!gameEvent.KillerActorId.HasValue)
            {
                return;
            }

            if (!profileRegistry.TryGetProfile(gameEvent.ActorId, out var profile) || profile.SpeciesId < 1)
            {
                return;
            }

            var actorId = gameEvent.KillerActorId.Value;
            if (!defeatedMonsterCountsByActor.TryGetValue(actorId, out var defeatedMonsterCounts))
            {
                defeatedMonsterCounts = new Dictionary<int, int>();
                defeatedMonsterCountsByActor.Add(actorId, defeatedMonsterCounts);
            }

            if (!defeatedMonsterCounts.TryGetValue(profile.SpeciesId, out var count))
            {
                count = 0;
            }

            defeatedMonsterCounts[profile.SpeciesId] = count + 1;
        }

        public void Dispose()
        {
            bag.Dispose();
        }
    }
}
