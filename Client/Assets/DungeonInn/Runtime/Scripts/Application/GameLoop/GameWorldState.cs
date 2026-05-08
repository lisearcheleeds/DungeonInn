using System;
using System.Collections.Generic;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Dungeon;
using DungeonInn.Domain.Guild;
using DungeonInn.Domain.Map;

namespace DungeonInn.Application.GameLoop
{
    public sealed class GameWorldState : IGameWorldState
    {
        readonly List<Actor> actors = new();
        readonly Dictionary<Guid, Actor> actorById = new();

        public bool IsInitialized { get; private set; }
        public AdventurerGuild Guild { get; private set; }
        public GroundMap GroundMap { get; private set; }
        public Dungeon Dungeon { get; private set; }
        public IReadOnlyList<Actor> Actors => actors;
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
    }
}
