using DungeonInn.Application.World;
using System;
using System.Collections.Generic;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Combat;
using DungeonInn.Domain.Dungeon;
using DungeonInn.Domain.Guild;
using DungeonInn.Domain.Item;
using DungeonInn.Domain.Map;

namespace DungeonInn.Application.World
{
    public interface IGameWorldStateReader
    {
        bool IsInitialized { get; }
        AdventurerGuild Guild { get; }
        GroundMap GroundMap { get; }
        Dungeon Dungeon { get; }
        InnEconomyState InnEconomy { get; }
        IReadOnlyList<Actor> Actors { get; }
        IReadOnlyList<ItemInstance> Items { get; }
        IReadOnlyList<ProjectileInstance> Projectiles { get; }
        IReadOnlyList<AreaEffectInstance> AreaEffects { get; }
        SpawnScheduleState SpawnSchedule { get; }

        Actor FindActor(Guid actorId);
    }

    public interface IGameWorldStateWriter
    {
        AdventurerGuild WritableGuild { get; }
        void Initialize(AdventurerGuild guild, GroundMap groundMap, Dungeon dungeon);
        void RegisterActor(Actor actor);
        bool RemoveActor(Guid actorId);
        void AddItem(ItemInstance item);
        bool RemoveItem(Guid instanceId);
        void AddProjectile(ProjectileInstance projectile);
        bool RemoveProjectile(Guid projectileId);
        void AddAreaEffect(AreaEffectInstance areaEffect);
        bool RemoveAreaEffect(Guid areaEffectId);
    }

    public interface IGameWorldState : IGameWorldStateReader, IGameWorldStateWriter
    {
    }
}
