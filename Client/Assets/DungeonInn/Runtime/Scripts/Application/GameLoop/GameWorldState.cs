using System;
using System.Collections.Generic;
using DungeonInn.Domain.Actor;
using DungeonInn.Application.Combat;
using DungeonInn.Domain.Combat;
using DungeonInn.Domain.Dungeon;
using DungeonInn.Domain.Guild;
using DungeonInn.Domain.Item;
using DungeonInn.Domain.Map;
using VContainer;

namespace DungeonInn.Application.GameLoop
{
    public sealed class GameWorldState : IGameWorldState
    {
        readonly List<Actor> actors = new();
        readonly Dictionary<Guid, Actor> actorById = new();
        readonly Dictionary<Guid, int> actorIndexById = new();
        readonly List<ItemInstance> items = new();
        readonly Dictionary<Guid, ItemInstance> itemById = new();
        readonly Dictionary<Guid, int> itemIndexById = new();
        readonly List<ProjectileInstance> projectiles = new();
        readonly Dictionary<Guid, ProjectileInstance> projectileById = new();
        readonly Dictionary<Guid, int> projectileIndexById = new();
        readonly List<AreaEffectInstance> areaEffects = new();
        readonly Dictionary<Guid, AreaEffectInstance> areaEffectById = new();
        readonly Dictionary<Guid, int> areaEffectIndexById = new();
        readonly ActorSpatialIndexService actorSpatialIndexService;

        public bool IsInitialized { get; private set; }
        public AdventurerGuild Guild { get; private set; }
        public GroundMap GroundMap { get; private set; }
        public Dungeon Dungeon { get; private set; }
        public InnEconomyState InnEconomy { get; } = new();
        public IReadOnlyList<Actor> Actors => actors;
        public IReadOnlyList<ItemInstance> Items => items;
        public IReadOnlyList<ProjectileInstance> Projectiles => projectiles;
        public IReadOnlyList<AreaEffectInstance> AreaEffects => areaEffects;
        public SpawnScheduleState SpawnSchedule { get; } = new();

        [Inject]
        public GameWorldState(ActorSpatialIndexService actorSpatialIndexService)
        {
            this.actorSpatialIndexService = actorSpatialIndexService
                ?? throw new ArgumentNullException(nameof(actorSpatialIndexService));
        }

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

            actorIndexById[actor.Id] = actors.Count;
            actors.Add(actor);
            actorById[actor.Id] = actor;
            actorSpatialIndexService.SyncActor(actor);
        }

        public bool RemoveActor(Guid actorId)
        {
            if (!actorById.ContainsKey(actorId))
            {
                return false;
            }

            actorById.Remove(actorId);
            RemoveAtSwap(actors, actorIndexById, actorId, actor => actor.Id);
            actorSpatialIndexService.RemoveActor(actorId);

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

            itemIndexById[item.InstanceId] = items.Count;
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
            RemoveAtSwap(items, itemIndexById, instanceId, item => item.InstanceId);

            return true;
        }

        public void AddProjectile(ProjectileInstance projectile)
        {
            if (projectile == null)
            {
                throw new ArgumentNullException(nameof(projectile));
            }

            if (projectileById.ContainsKey(projectile.Id))
            {
                throw new InvalidOperationException("Projectile is already registered.");
            }

            projectileIndexById[projectile.Id] = projectiles.Count;
            projectiles.Add(projectile);
            projectileById[projectile.Id] = projectile;
        }

        public bool RemoveProjectile(Guid projectileId)
        {
            if (!projectileById.ContainsKey(projectileId))
            {
                return false;
            }

            projectileById.Remove(projectileId);
            RemoveAtSwap(projectiles, projectileIndexById, projectileId, projectile => projectile.Id);

            return true;
        }

        public void AddAreaEffect(AreaEffectInstance areaEffect)
        {
            if (areaEffect == null)
            {
                throw new ArgumentNullException(nameof(areaEffect));
            }

            if (areaEffectById.ContainsKey(areaEffect.Id))
            {
                throw new InvalidOperationException("Area effect is already registered.");
            }

            areaEffectIndexById[areaEffect.Id] = areaEffects.Count;
            areaEffects.Add(areaEffect);
            areaEffectById[areaEffect.Id] = areaEffect;
        }

        public bool RemoveAreaEffect(Guid areaEffectId)
        {
            if (!areaEffectById.ContainsKey(areaEffectId))
            {
                return false;
            }

            areaEffectById.Remove(areaEffectId);
            RemoveAtSwap(areaEffects, areaEffectIndexById, areaEffectId, areaEffect => areaEffect.Id);

            return true;
        }

        static void RemoveAtSwap<T>(
            List<T> values,
            Dictionary<Guid, int> indexById,
            Guid removedId,
            Func<T, Guid> idSelector)
        {
            if (!indexById.TryGetValue(removedId, out var index))
            {
                return;
            }

            var lastIndex = values.Count - 1;
            if (index < lastIndex)
            {
                var lastValue = values[lastIndex];
                values[index] = lastValue;
                indexById[idSelector(lastValue)] = index;
            }

            values.RemoveAt(lastIndex);
            indexById.Remove(removedId);
        }
    }
}
