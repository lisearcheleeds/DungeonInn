using System;
using System.Collections.Generic;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Combat;
using DungeonInn.Domain.Dungeon;
using DungeonInn.Domain.Guild;
using DungeonInn.Domain.Item;
using DungeonInn.Domain.Map;

namespace DungeonInn.Application.GameLoop
{
    public interface IGameWorldState
    {
        bool IsInitialized { get; }
        AdventurerGuild Guild { get; }
        GroundMap GroundMap { get; }
        Dungeon Dungeon { get; }
        IReadOnlyList<Actor> Actors { get; }
        IReadOnlyList<ItemInstance> Items { get; }
        IReadOnlyList<ProjectileInstance> Projectiles { get; }
        SpawnScheduleState SpawnSchedule { get; }

        void Initialize(AdventurerGuild guild, GroundMap groundMap, Dungeon dungeon);
        void RegisterActor(Actor actor);
        bool RemoveActor(Guid actorId);
        Actor FindActor(Guid actorId);
        void AddItem(ItemInstance item);
        bool RemoveItem(Guid instanceId);
        void AddProjectile(ProjectileInstance projectile);
        bool RemoveProjectile(Guid projectileId);
    }
}
