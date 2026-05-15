using System;
using System.Collections.Generic;
using R3;
using VContainer;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;

namespace DungeonInn.Application.Actors.Lifecycle
{
    public sealed class ActorExplorationAchievementRegistry : IDisposable
    {
        readonly Dictionary<Guid, Dictionary<int, int>> defeatedMonsterSpeciesCountsByActor = new();
        DisposableBag bag;

        [Inject]
        public ActorExplorationAchievementRegistry(IEventSubscriber eventSubscriber)
        {
            if (eventSubscriber == null)
            {
                throw new ArgumentNullException(nameof(eventSubscriber));
            }

            eventSubscriber.OnEvent<ActorEnteredDungeon>()
                .Subscribe(gameEvent => { Reset(gameEvent.ActorId); })
                .AddTo(ref bag);
            eventSubscriber.OnEvent<ActorDefeated>()
                .Subscribe(gameEvent => { Cleanup(gameEvent.ActorId); })
                .AddTo(ref bag);
            eventSubscriber.OnEvent<ActorDeparted>()
                .Subscribe(gameEvent => { Cleanup(gameEvent.ActorId); })
                .AddTo(ref bag);
        }

        public int GetDefeatedMonsterCount(Guid actorId, int monsterSpeciesId)
        {
            if (!defeatedMonsterSpeciesCountsByActor.TryGetValue(actorId, out var defeatedMonsterCounts))
            {
                return 0;
            }

            defeatedMonsterCounts.TryGetValue(monsterSpeciesId, out var count);
            return count;
        }

        public void RecordDefeatedMonster(Guid actorId, int monsterSpeciesId)
        {
            if (monsterSpeciesId < 1)
            {
                return;
            }

            if (!defeatedMonsterSpeciesCountsByActor.TryGetValue(actorId, out var defeatedMonsterCounts))
            {
                defeatedMonsterCounts = new Dictionary<int, int>();
                defeatedMonsterSpeciesCountsByActor.Add(actorId, defeatedMonsterCounts);
            }

            if (!defeatedMonsterCounts.TryGetValue(monsterSpeciesId, out var count))
            {
                count = 0;
            }

            defeatedMonsterCounts[monsterSpeciesId] = count + 1;
        }

        public void Reset(Guid actorId)
        {
            defeatedMonsterSpeciesCountsByActor.Remove(actorId);
        }

        public void Cleanup(Guid actorId)
        {
            defeatedMonsterSpeciesCountsByActor.Remove(actorId);
        }

        public void Dispose()
        {
            bag.Dispose();
        }
    }
}
