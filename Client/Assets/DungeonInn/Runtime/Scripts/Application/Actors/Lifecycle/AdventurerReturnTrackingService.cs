using System;
using System.Collections.Generic;
using R3;
using VContainer;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Application.Actors.Profiles;

namespace DungeonInn.Application.Actors.Lifecycle
{
    public sealed class AdventurerReturnTrackingService : IDisposable
    {
        readonly IActorProfileRegistry profileRegistry;
        readonly ActorExplorationAchievementRegistry achievementRegistry;
        readonly HashSet<Guid> dirtyActorIds = new();
        readonly List<Guid> dirtyIdBuffer = new();
        DisposableBag bag;

        [Inject]
        public AdventurerReturnTrackingService(
            IEventSubscriber eventSubscriber,
            IActorProfileRegistry profileRegistry,
            ActorExplorationAchievementRegistry achievementRegistry)
        {
            if (eventSubscriber == null)
            {
                throw new ArgumentNullException(nameof(eventSubscriber));
            }

            this.profileRegistry = profileRegistry ?? throw new ArgumentNullException(nameof(profileRegistry));
            this.achievementRegistry = achievementRegistry ?? throw new ArgumentNullException(nameof(achievementRegistry));

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
            eventSubscriber.OnEvent<ActorDeparted>()
                .Subscribe(OnActorDeparted)
                .AddTo(ref bag);
        }

        public bool HasDirtyActors => 0 < dirtyActorIds.Count;

        public bool IsDirty(Guid actorId)
        {
            return dirtyActorIds.Contains(actorId);
        }

        public void CollectDirtyActorIds(List<Guid> results)
        {
            if (results == null)
            {
                throw new ArgumentNullException(nameof(results));
            }

            results.Clear();
            foreach (var actorId in dirtyActorIds)
            {
                results.Add(actorId);
            }
        }

        public void ClearDirty(Guid actorId)
        {
            dirtyActorIds.Remove(actorId);
        }

        public void RemoveMissingDirtyActors(HashSet<Guid> foundDirtyActorIds)
        {
            dirtyIdBuffer.Clear();
            foreach (var actorId in dirtyActorIds)
            {
                dirtyIdBuffer.Add(actorId);
            }

            foreach (var actorId in dirtyIdBuffer)
            {
                if (!foundDirtyActorIds.Contains(actorId))
                {
                    dirtyActorIds.Remove(actorId);
                }
            }
        }

        public int GetDefeatedMonsterCount(Guid actorId, int monsterSpeciesId)
        {
            return achievementRegistry.GetDefeatedMonsterCount(actorId, monsterSpeciesId);
        }

        void MarkDirty(Guid actorId)
        {
            dirtyActorIds.Add(actorId);
        }

        void OnActorDeparted(ActorDeparted gameEvent)
        {
            dirtyActorIds.Remove(gameEvent.ActorId);
        }

        void OnActorDefeated(ActorDefeated gameEvent)
        {
            dirtyActorIds.Remove(gameEvent.ActorId);

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

            achievementRegistry.RecordDefeatedMonster(gameEvent.KillerActorId.Value, profile.SpeciesId);
        }

        public void Dispose()
        {
            bag.Dispose();
        }
    }
}
