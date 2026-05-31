using System;
using System.Collections.Generic;
using R3;
using VContainer;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Domain.Actor;

namespace DungeonInn.Application.Actors.Lifecycle
{
    public sealed class ActorExplorationAchievementRegistry : IDisposable
    {
        readonly Dictionary<Guid, Dictionary<int, int>> defeatedMonsterSpeciesCountsByActor = new();
        readonly Dictionary<Guid, Dictionary<int, int>> inventoryBaselineByActor = new();
        readonly Dictionary<Guid, ActorAdventureStartStatus> startStatusByActor = new();
        DisposableBag bag;

        [Inject]
        public ActorExplorationAchievementRegistry(IEventSubscriber eventSubscriber)
        {
            if (eventSubscriber == null)
            {
                throw new ArgumentNullException(nameof(eventSubscriber));
            }

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

        public int GetItemCountIncrease(Guid actorId, int itemId, IReadOnlyDictionary<int, int> currentItemCounts)
        {
            if (currentItemCounts == null)
            {
                throw new ArgumentNullException(nameof(currentItemCounts));
            }

            currentItemCounts.TryGetValue(itemId, out var currentCount);
            if (!inventoryBaselineByActor.TryGetValue(actorId, out var baseline))
            {
                return currentCount;
            }

            baseline.TryGetValue(itemId, out var baselineCount);
            return Math.Max(0, currentCount - baselineCount);
        }

        public IReadOnlyDictionary<int, int> GetInventoryBaseline(Guid actorId)
        {
            return inventoryBaselineByActor.TryGetValue(actorId, out var baseline)
                ? baseline
                : null;
        }

        public bool TryGetAdventureStartStatus(Guid actorId, out ActorAdventureStartStatus status)
        {
            return startStatusByActor.TryGetValue(actorId, out status);
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

        public void RecordAdventureStart(Actor actor)
        {
            if (actor == null)
            {
                throw new ArgumentNullException(nameof(actor));
            }

            defeatedMonsterSpeciesCountsByActor.Remove(actor.Id);
            inventoryBaselineByActor[actor.Id] = new Dictionary<int, int>(actor.Inventory.ItemCounts);
            startStatusByActor[actor.Id] = new ActorAdventureStartStatus(actor.Level, actor.Experience);
        }

        public void Cleanup(Guid actorId)
        {
            defeatedMonsterSpeciesCountsByActor.Remove(actorId);
            inventoryBaselineByActor.Remove(actorId);
            startStatusByActor.Remove(actorId);
        }

        public void Dispose()
        {
            bag.Dispose();
        }
    }

    public readonly struct ActorAdventureStartStatus
    {
        public int Level { get; }
        public int Experience { get; }

        public ActorAdventureStartStatus(int level, int experience)
        {
            Level = Math.Max(1, level);
            Experience = Math.Max(0, experience);
        }
    }
}
