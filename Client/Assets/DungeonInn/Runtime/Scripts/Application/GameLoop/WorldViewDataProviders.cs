using System;
using System.Collections.Generic;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Dungeon;
using DungeonInn.Domain.Map;
using VContainer;

namespace DungeonInn.Application.GameLoop
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

    public interface IWorldMapViewDataProvider
    {
        IReadOnlyList<WorldMapLayerViewData> GetLayers();
    }

    public interface IActorViewDataProvider
    {
        IReadOnlyList<ActorViewData> GetActors();
    }

    public sealed class WorldMapViewDataProvider : IWorldMapViewDataProvider
    {
        readonly IGameWorldStateReader worldState;
        readonly List<WorldMapLayerViewData> layers = new();
        readonly Dictionary<int, WorldMapLayerViewData> cachedLayers = new();
        readonly List<WorldMapCellViewKind> cellKindBuffer = new();

        [Inject]
        public WorldMapViewDataProvider(IGameWorldStateReader worldState)
        {
            this.worldState = worldState ?? throw new ArgumentNullException(nameof(worldState));
        }

        public IReadOnlyList<WorldMapLayerViewData> GetLayers()
        {
            layers.Clear();
            var groundMap = worldState.GroundMap;
            layers.Add(GetOrCreateLayer(
                MapLayerId.Ground,
                "Ground",
                groundMap.Layer.Width,
                groundMap.Layer.Depth,
                position => groundMap.IsWalkable(position)
                    ? WorldMapCellViewKind.GroundWalkable
                    : WorldMapCellViewKind.GroundBlocked));

            foreach (var pair in worldState.Dungeon.Floors)
            {
                var floor = pair.Value;
                layers.Add(GetOrCreateLayer(
                    MapLayerId.DungeonFloor(pair.Key),
                    $"DungeonFloor{floor.FloorIndex}",
                    floor.Layer.Width,
                    floor.Layer.Depth,
                    position => ResolveDungeonCellKind(floor, position)));
            }

            return layers;
        }

        WorldMapLayerViewData GetOrCreateLayer(
            MapLayerId layerId,
            string layerName,
            int width,
            int height,
            Func<GridPosition, WorldMapCellViewKind> resolveCellKind)
        {
            if (cachedLayers.TryGetValue(layerId.Value, out var layerData))
            {
                return layerData;
            }

            cellKindBuffer.Clear();
            for (var z = 0; z < height; z++)
            {
                for (var x = 0; x < width; x++)
                {
                    cellKindBuffer.Add(resolveCellKind(new GridPosition(x, z)));
                }
            }

            layerData = new WorldMapLayerViewData(layerId, layerName, width, height, cellKindBuffer);
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

    public sealed class ActorViewDataProvider : IActorViewDataProvider
    {
        readonly IGameWorldStateReader worldState;
        readonly List<ActorViewData> actors = new();

        [Inject]
        public ActorViewDataProvider(IGameWorldStateReader worldState)
        {
            this.worldState = worldState ?? throw new ArgumentNullException(nameof(worldState));
        }

        public IReadOnlyList<ActorViewData> GetActors()
        {
            actors.Clear();
            foreach (var actor in worldState.Actors)
            {
                actors.Add(new ActorViewData(actor.Id, actor.Position, ResolveBehaviorType(actor)));
            }

            return actors;
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
