using System;
using System.Collections.Generic;
using DungeonInn.Application.Actors.Movement;
using DungeonInn.Domain.Common;
using DungeonInn.Domain.Map;
using UnityEngine;
using UnityEngine.AI;
using VContainer;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class UnityNavMeshPathProvider : INavigationPathProvider
    {
        readonly MapLayerViewRegistry layerViewRegistry;
        readonly NavMeshPath navMeshPath = new();
        readonly List<GridPosition> resultPath = new();

        [Inject]
        public UnityNavMeshPathProvider(MapLayerViewRegistry layerViewRegistry)
        {
            this.layerViewRegistry = layerViewRegistry ?? throw new ArgumentNullException(nameof(layerViewRegistry));
        }

        public IReadOnlyList<GridPosition> TryFindPath(
            MapLayerId layerId,
            GridPosition start,
            GridPosition goal)
        {
            var tileRoot = layerViewRegistry.GetTileRoot(layerId);
            if (tileRoot == null)
            {
                return null;
            }

            var worldStart = GridToWorld(tileRoot, start);
            var worldGoal = GridToWorld(tileRoot, goal);
            if (!NavMesh.CalculatePath(worldStart, worldGoal, NavMesh.AllAreas, navMeshPath))
            {
                return null;
            }

            if (navMeshPath.status != NavMeshPathStatus.PathComplete)
            {
                return null;
            }

            resultPath.Clear();
            var corners = navMeshPath.corners;
            for (var i = 1; i < corners.Length; i++)
            {
                var gridPosition = WorldToGrid(tileRoot, corners[i]);
                if (resultPath.Count == 0 ||
                    !resultPath[resultPath.Count - 1].Equals(gridPosition))
                {
                    resultPath.Add(gridPosition);
                }
            }

            return resultPath.Count == 0 ? null : resultPath;
        }

        static Vector3 GridToWorld(Transform tileRoot, GridPosition grid)
        {
            var localX = (grid.X + 0.5f) * GameConstants.MapCellWidthMeters;
            var localZ = (grid.Z + 0.5f) * GameConstants.MapCellWidthMeters;
            return tileRoot.TransformPoint(new Vector3(localX, 0f, localZ));
        }

        static GridPosition WorldToGrid(Transform tileRoot, Vector3 worldPoint)
        {
            var local = tileRoot.InverseTransformPoint(worldPoint);
            return new GridPosition(
                Mathf.FloorToInt(local.x / GameConstants.MapCellWidthMeters),
                Mathf.FloorToInt(local.z / GameConstants.MapCellWidthMeters));
        }
    }
}
