using System;
using System.Collections.Generic;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Dungeon;
using DungeonInn.Domain.Map;
using VContainer;

namespace DungeonInn.Application.World
{
    public enum WorldMapCellViewKind
    {
        GroundWalkable,
        GroundBlocked,
        DungeonWalkable,
        DungeonBlocked,
        StairUp,
        StairDown
    }

    public readonly struct WorldMapLayerViewData
    {
        readonly WorldMapCellViewKind[] cellKinds;

        public WorldMapLayerViewData(
            MapLayerId layerId,
            string layerName,
            int width,
            int height,
            IReadOnlyList<WorldMapCellViewKind> cellKinds)
        {
            if (width < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(width));
            }

            if (height < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(height));
            }

            LayerId = layerId;
            LayerName = layerName ?? throw new ArgumentNullException(nameof(layerName));
            if (cellKinds == null)
            {
                throw new ArgumentNullException(nameof(cellKinds));
            }

            if (cellKinds.Count != width * height)
            {
                throw new ArgumentException("Cell kind count must match layer size.", nameof(cellKinds));
            }

            Width = width;
            Height = height;
            this.cellKinds = new WorldMapCellViewKind[cellKinds.Count];
            for (var index = 0; index < cellKinds.Count; index++)
            {
                this.cellKinds[index] = cellKinds[index];
            }
        }

        public MapLayerId LayerId { get; }
        public string LayerName { get; }
        public int Width { get; }
        public int Height { get; }

        public WorldMapCellViewKind GetCellKind(GridPosition position)
        {
            if (position.X < 0 || Width <= position.X)
            {
                throw new ArgumentOutOfRangeException(nameof(position));
            }

            if (position.Z < 0 || Height <= position.Z)
            {
                throw new ArgumentOutOfRangeException(nameof(position));
            }

            return cellKinds[position.Z * Width + position.X];
        }
    }

    public readonly struct ActorViewData
    {
        public ActorViewData(Guid actorId, LayerPosition position, ActorBehaviorType behaviorType)
        {
            ActorId = actorId;
            Position = position;
            BehaviorType = behaviorType;
        }

        public Guid ActorId { get; }
        public LayerPosition Position { get; }
        public ActorBehaviorType BehaviorType { get; }
    }

    public readonly struct ActorViewDataChangeBuffer
    {
        public ActorViewDataChangeBuffer(
            IReadOnlyList<ActorViewData> changedActors,
            IReadOnlyList<Guid> removedActorIds)
        {
            ChangedActors = changedActors ?? throw new ArgumentNullException(nameof(changedActors));
            RemovedActorIds = removedActorIds ?? throw new ArgumentNullException(nameof(removedActorIds));
        }

        public IReadOnlyList<ActorViewData> ChangedActors { get; }
        public IReadOnlyList<Guid> RemovedActorIds { get; }
    }

    public interface IWorldMapViewDataProvider
    {
        WorldMapLayerViewData GetLayer(MapLayerId layerId);
        void InvalidateLayer(MapLayerId layerId);
    }

    public interface IActorViewDataProvider
    {
        ActorViewDataChangeBuffer ConsumeChanges();
    }

    public sealed class WorldMapViewDataProvider : IWorldMapViewDataProvider
    {
        readonly IGameWorldStateReader worldState;
        readonly Dictionary<int, WorldMapLayerViewData> cachedLayers = new();
        readonly List<WorldMapCellViewKind> cellKindBuffer = new();

        [Inject]
        public WorldMapViewDataProvider(IGameWorldStateReader worldState)
        {
            this.worldState = worldState ?? throw new ArgumentNullException(nameof(worldState));
        }

        public WorldMapLayerViewData GetLayer(MapLayerId layerId)
        {
            if (layerId.Value == MapLayerId.Ground.Value)
            {
                return GetOrCreateGroundLayer(worldState.GroundMap);
            }

            var floorIndex = layerId.Value;
            if (worldState.Dungeon.Floors.TryGetValue(floorIndex, out var floor))
            {
                return GetOrCreateDungeonLayer(floor);
            }

            throw new InvalidOperationException($"Layer not found: {layerId.Value}");
        }

        public void InvalidateLayer(MapLayerId layerId)
        {
            cachedLayers.Remove(layerId.Value);
        }

        WorldMapLayerViewData GetOrCreateGroundLayer(GroundMap groundMap)
        {
            if (cachedLayers.TryGetValue(MapLayerId.Ground.Value, out var layerData))
            {
                return layerData;
            }

            var width = groundMap.Layer.Width;
            var height = groundMap.Layer.Depth;
            cellKindBuffer.Clear();
            for (var z = 0; z < height; z++)
            {
                for (var x = 0; x < width; x++)
                {
                    var position = new GridPosition(x, z);
                    cellKindBuffer.Add(groundMap.IsWalkable(position)
                        ? WorldMapCellViewKind.GroundWalkable
                        : WorldMapCellViewKind.GroundBlocked);
                }
            }

            layerData = new WorldMapLayerViewData(MapLayerId.Ground, "Ground", width, height, cellKindBuffer);
            cachedLayers.Add(MapLayerId.Ground.Value, layerData);
            return layerData;
        }

        WorldMapLayerViewData GetOrCreateDungeonLayer(DungeonFloor floor)
        {
            var layerId = MapLayerId.DungeonFloor(floor.FloorIndex);
            if (cachedLayers.TryGetValue(layerId.Value, out var layerData))
            {
                return layerData;
            }

            var width = floor.Layer.Width;
            var height = floor.Layer.Depth;
            cellKindBuffer.Clear();
            for (var z = 0; z < height; z++)
            {
                for (var x = 0; x < width; x++)
                {
                    cellKindBuffer.Add(ResolveDungeonCellKind(floor, new GridPosition(x, z)));
                }
            }

            layerData = new WorldMapLayerViewData(
                layerId,
                $"DungeonFloor{floor.FloorIndex}",
                width,
                height,
                cellKindBuffer);
            cachedLayers.Add(layerId.Value, layerData);
            return layerData;
        }

        static WorldMapCellViewKind ResolveDungeonCellKind(DungeonFloor floor, GridPosition position)
        {
            if (floor.IsStairPosition(position, DungeonStairType.Up))
            {
                return WorldMapCellViewKind.StairUp;
            }

            if (floor.IsStairPosition(position, DungeonStairType.Down))
            {
                return WorldMapCellViewKind.StairDown;
            }

            return floor.IsWalkable(position)
                ? WorldMapCellViewKind.DungeonWalkable
                : WorldMapCellViewKind.DungeonBlocked;
        }
    }

    public sealed class ActorViewDataStore : IActorViewDataProvider
    {
        readonly Dictionary<Guid, ActorViewData> actorViewDataById = new();
        readonly HashSet<Guid> dirtyActorIds = new();
        readonly HashSet<Guid> removedActorIdSet = new();
        readonly List<ActorViewData> changedActors = new();
        readonly List<Guid> removedActorIds = new();

        public void SyncActor(Actor actor)
        {
            if (actor == null)
            {
                throw new ArgumentNullException(nameof(actor));
            }

            var viewData = new ActorViewData(actor.Id, actor.Position, ResolveBehaviorType(actor));
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

        static bool IsSame(ActorViewData first, ActorViewData second)
        {
            return first.ActorId.Equals(second.ActorId) &&
                first.Position.LayerId.Equals(second.Position.LayerId) &&
                first.Position.X.Equals(second.Position.X) &&
                first.Position.Z.Equals(second.Position.Z) &&
                first.BehaviorType == second.BehaviorType;
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

            return ActorBehaviorType.None;
        }
    }
}
