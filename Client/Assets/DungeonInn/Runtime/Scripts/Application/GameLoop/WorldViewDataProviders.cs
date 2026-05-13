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
        readonly Func<GridPosition, WorldMapCellViewKind> resolveCellKind;

        public WorldMapLayerViewData(
            MapLayerId layerId,
            string layerName,
            MapLayer layer,
            Func<GridPosition, WorldMapCellViewKind> resolveCellKind)
        {
            LayerId = layerId;
            LayerName = layerName ?? throw new ArgumentNullException(nameof(layerName));
            if (layer == null)
            {
                throw new ArgumentNullException(nameof(layer));
            }

            Width = layer.Width;
            Height = layer.Depth;
            this.resolveCellKind = resolveCellKind ?? throw new ArgumentNullException(nameof(resolveCellKind));
        }

        public MapLayerId LayerId { get; }
        public string LayerName { get; }
        public int Width { get; }
        public int Height { get; }

        public WorldMapCellViewKind GetCellKind(GridPosition position)
        {
            return resolveCellKind(position);
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

        [Inject]
        public WorldMapViewDataProvider(IGameWorldStateReader worldState)
        {
            this.worldState = worldState ?? throw new ArgumentNullException(nameof(worldState));
        }

        public IReadOnlyList<WorldMapLayerViewData> GetLayers()
        {
            layers.Clear();
            var groundMap = worldState.GroundMap;
            layers.Add(new WorldMapLayerViewData(
                MapLayerId.Ground,
                "Ground",
                groundMap.Layer,
                position => groundMap.IsWalkable(position)
                    ? WorldMapCellViewKind.GroundWalkable
                    : WorldMapCellViewKind.GroundBlocked));

            foreach (var pair in worldState.Dungeon.Floors)
            {
                var floor = pair.Value;
                layers.Add(new WorldMapLayerViewData(
                    MapLayerId.DungeonFloor(pair.Key),
                    $"DungeonFloor{floor.FloorIndex}",
                    floor.Layer,
                    position => ResolveDungeonCellKind(floor, position)));
            }

            return layers;
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
