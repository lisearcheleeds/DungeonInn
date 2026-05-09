using System;
using System.Collections.Generic;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Dungeon;
using DungeonInn.Domain.Guild;
using DungeonInn.Domain.Item;
using DungeonInn.Domain.Map;

namespace DungeonInn.Application.GameLoop
{
    public sealed class GameWorldState : IGameWorldState
    {
        readonly List<Actor> actors = new();
        readonly Dictionary<Guid, Actor> actorById = new();
        readonly List<ItemInstance> items = new();
        readonly Dictionary<Guid, ItemInstance> itemById = new();

        public bool IsInitialized { get; private set; }
        public AdventurerGuild Guild { get; private set; }
        public GroundMap GroundMap { get; private set; }
        public Dungeon Dungeon { get; private set; }
        public IReadOnlyList<Actor> Actors => actors;
        public IReadOnlyList<ItemInstance> Items => items;
        public SpawnScheduleState SpawnSchedule { get; } = new();

        public void Initialize(AdventurerGuild guild, GroundMap groundMap, Dungeon dungeon)
        {
            if (IsInitialized)
            {
                throw new InvalidOperationException("Game world state is already initialized.");
            }

            Guild = guild ?? throw new ArgumentNullException(nameof(guild));
            GroundMap = groundMap ?? throw new ArgumentNullException(nameof(groundMap));
            Dungeon = dungeon ?? throw new ArgumentNullException(nameof(dungeon));
            IsInitialized = true;
        }

        public void RegisterActor(Actor actor)
        {
            if (actor == null)
            {
                throw new ArgumentNullException(nameof(actor));
            }

            if (actorById.ContainsKey(actor.Id))
            {
                throw new InvalidOperationException("Actor is already registered.");
            }

            actors.Add(actor);
            actorById[actor.Id] = actor;
        }

        public bool RemoveActor(Guid actorId)
        {
            if (!actorById.ContainsKey(actorId))
            {
                return false;
            }

            actorById.Remove(actorId);
            var index = actors.FindIndex(x => x.Id.Equals(actorId));
            if (index >= 0)
            {
                actors.RemoveAt(index);
            }

            return true;
        }

        public Actor FindActor(Guid actorId)
        {
            actorById.TryGetValue(actorId, out var actor);
            return actor;
        }

        public void AddItem(ItemInstance item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (itemById.ContainsKey(item.InstanceId))
            {
                throw new InvalidOperationException("Item instance is already registered.");
            }

            items.Add(item);
            itemById[item.InstanceId] = item;
        }

        public bool RemoveItem(Guid instanceId)
        {
            if (!itemById.ContainsKey(instanceId))
            {
                return false;
            }

            itemById.Remove(instanceId);
            var index = items.FindIndex(x => x.InstanceId.Equals(instanceId));
            if (index >= 0)
            {
                items.RemoveAt(index);
            }

            return true;
        }
    }
}
