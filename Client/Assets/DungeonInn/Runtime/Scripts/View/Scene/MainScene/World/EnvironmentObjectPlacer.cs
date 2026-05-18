using System;
using System.Collections.Generic;
using DungeonInn.Application.World;
using DungeonInn.Domain.Common;
using DungeonInn.Domain.Map;
using UnityEngine;
using VContainer;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class EnvironmentObjectPlacer : IDisposable
    {
        readonly GameObject propPrefab;
        readonly List<GameObject> placedObjects = new();

        [Inject]
        public EnvironmentObjectPlacer(VisualConfigSettings settings)
        {
            propPrefab = settings?.PropPrefab;
        }

        public void PlaceChunkProps(
            Transform parent,
            WorldMapLayerViewData layerData,
            int startX,
            int startZ,
            int width,
            int height)
        {
            for (var z = startZ; z < startZ + height; z++)
            {
                for (var x = startX; x < startX + width; x++)
                {
                    var position = new GridPosition(x, z);
                    var cellKind = layerData.GetCellKind(position);
                    if (cellKind == WorldMapCellViewKind.StairUp ||
                        cellKind == WorldMapCellViewKind.StairDown)
                    {
                        PlacePropAt(parent, x, z);
                    }
                }
            }
        }

        public void Dispose()
        {
            foreach (var placedObject in placedObjects)
            {
                if (placedObject != null)
                {
                    UnityEngine.Object.Destroy(placedObject);
                }
            }

            placedObjects.Clear();
        }

        void PlacePropAt(Transform parent, int gridX, int gridZ)
        {
            var worldX = (gridX + 0.5f) * GameConstants.MapCellSizeMeters;
            var worldZ = (gridZ + 0.5f) * GameConstants.MapCellSizeMeters;
            var propObject = propPrefab != null
                ? UnityEngine.Object.Instantiate(propPrefab)
                : GameObject.CreatePrimitive(PrimitiveType.Cube);
            propObject.name = $"Prop_{gridX}_{gridZ}";
            propObject.layer = WorldRenderingLayer.Layer;
            propObject.transform.SetParent(parent, false);
            propObject.transform.localPosition = new Vector3(worldX, 0.5f, worldZ);
            propObject.transform.localScale = Vector3.one * 0.5f;

            if (propPrefab == null)
            {
                var collider = propObject.GetComponent<Collider>();
                if (collider != null)
                {
                    UnityEngine.Object.Destroy(collider);
                }
            }

            placedObjects.Add(propObject);
        }
    }
}
