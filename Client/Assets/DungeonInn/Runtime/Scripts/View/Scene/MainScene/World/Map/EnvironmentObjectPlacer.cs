using System;
using System.Collections.Generic;
using DungeonInn.Application.World;
using DungeonInn.Domain.Common;
using DungeonInn.Domain.Map;
using DungeonInn.Master;
using UnityEngine;
using VContainer;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class EnvironmentObjectPlacer : IDisposable
    {
        readonly WorldAddressableViewFactory viewFactory;
        readonly IMasterRepository masterRepository;
        readonly Dictionary<int, List<GameObject>> propsByLayer = new();

        [Inject]
        public EnvironmentObjectPlacer(
            WorldAddressableViewFactory viewFactory,
            IMasterRepository masterRepository)
        {
            this.viewFactory = viewFactory ?? throw new ArgumentNullException(nameof(viewFactory));
            this.masterRepository = masterRepository ?? throw new ArgumentNullException(nameof(masterRepository));
        }

        public void PlaceChunkProps(
            Transform parent,
            WorldMapLayerViewData layerData,
            int startX,
            int startZ,
            int width,
            int height)
        {
            var layerProps = GetOrCreateLayerProps(layerData.LayerId);
            for (var z = startZ; z < startZ + height; z++)
            {
                for (var x = startX; x < startX + width; x++)
                {
                    var position = new GridPosition(x, z);
                    var cellKind = layerData.GetCellKind(position);
                    if (cellKind == WorldMapCellViewKind.StairUp ||
                        cellKind == WorldMapCellViewKind.StairDown)
                    {
                        PlacePropAt(parent, layerProps, cellKind, x, z);
                    }
                }
            }
        }

        public void InvalidateLayer(MapLayerId layerId)
        {
            if (!propsByLayer.TryGetValue(layerId.Value, out var layerProps))
            {
                return;
            }

            DestroyProps(layerProps);
            propsByLayer.Remove(layerId.Value);
        }

        public void Dispose()
        {
            foreach (var layerProps in propsByLayer.Values)
            {
                DestroyProps(layerProps);
            }

            propsByLayer.Clear();
        }

        List<GameObject> GetOrCreateLayerProps(MapLayerId layerId)
        {
            if (!propsByLayer.TryGetValue(layerId.Value, out var layerProps))
            {
                layerProps = new List<GameObject>();
                propsByLayer.Add(layerId.Value, layerProps);
            }

            return layerProps;
        }

        void PlacePropAt(
            Transform parent,
            List<GameObject> layerProps,
            WorldMapCellViewKind cellKind,
            int gridX,
            int gridZ)
        {
            var worldX = (gridX + 0.5f) * GameConstants.MapCellWidthMeters;
            var worldZ = (gridZ + 0.5f) * GameConstants.MapCellWidthMeters;
            var propPrefab = viewFactory.GetPropPrefab(ResolvePropPrefabAddress(cellKind));
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
                    DestroyObject(collider);
                }
            }

            layerProps.Add(propObject);
        }

        string ResolvePropPrefabAddress(WorldMapCellViewKind cellKind)
        {
            switch (cellKind)
            {
                case WorldMapCellViewKind.StairUp:
                    return masterRepository.GetEnvironmentPropVisualMaster("StairUp").PrefabAddress;
                case WorldMapCellViewKind.StairDown:
                    return masterRepository.GetEnvironmentPropVisualMaster("StairDown").PrefabAddress;
                default:
                    return string.Empty;
            }
        }

        static void DestroyProps(List<GameObject> props)
        {
            foreach (var prop in props)
            {
                if (prop != null)
                {
                    DestroyPropObject(prop);
                }
            }

            props.Clear();
        }

        static void DestroyPropObject(GameObject prop)
        {
            DestroyObject(prop);
        }

        static void DestroyObject(UnityEngine.Object target)
        {
#if UNITY_EDITOR
            if (!UnityEngine.Application.isPlaying)
            {
                UnityEngine.Object.DestroyImmediate(target);
                return;
            }
#endif
            UnityEngine.Object.Destroy(target);
        }
    }
}
