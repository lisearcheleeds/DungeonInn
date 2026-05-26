using System;
using System.Collections.Generic;
using DungeonInn.Application.Actors.Movement;
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
        readonly List<LayerPosition> resultPath = new();

        [Inject]
        public UnityNavMeshPathProvider(MapLayerViewRegistry layerViewRegistry)
        {
            this.layerViewRegistry = layerViewRegistry ?? throw new ArgumentNullException(nameof(layerViewRegistry));
        }

        public IReadOnlyList<LayerPosition> TryFindPath(
            MapLayer layer,
            LayerPosition start,
            LayerPosition goal)
        {
            var tileRoot = layerViewRegistry.GetTileRoot(layer.Id);
            if (tileRoot == null)
            {
                return null;
            }

            var worldStart = LayerToWorld(tileRoot, start);
            var worldGoal = LayerToWorld(tileRoot, goal);
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
                var waypoint = WorldToLayerPosition(layer, tileRoot, corners[i]);
                if (resultPath.Count == 0 ||
                    0.0001f < resultPath[resultPath.Count - 1].DistanceSquaredTo(waypoint))
                {
                    resultPath.Add(waypoint);
                }
            }

            return resultPath.Count == 0 ? null : resultPath;
        }

        static Vector3 LayerToWorld(Transform tileRoot, LayerPosition position)
        {
            return tileRoot.TransformPoint(new Vector3(position.X, 0f, position.Z));
        }

        static LayerPosition WorldToLayerPosition(MapLayer layer, Transform tileRoot, Vector3 worldPoint)
        {
            var local = tileRoot.InverseTransformPoint(worldPoint);
            var maxX = layer.Width * layer.CellSizeMeters - 0.0001f;
            var maxZ = layer.Depth * layer.CellSizeMeters - 0.0001f;
            return new LayerPosition(
                layer.Id,
                Mathf.Clamp(local.x, 0f, maxX),
                Mathf.Clamp(local.z, 0f, maxZ));
        }
    }
}
