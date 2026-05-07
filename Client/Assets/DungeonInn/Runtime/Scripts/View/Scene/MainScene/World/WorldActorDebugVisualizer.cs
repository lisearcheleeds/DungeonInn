using System;
using System.Collections.Generic;
using DungeonInn.Application.GameLoop;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Common;
using DungeonInn.Domain.Dungeon;
using DungeonInn.Domain.Map;
using UnityEngine;
using VContainer;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class WorldActorDebugVisualizer : IDisposable
    {
        const float LayerHeightOffset = -240f;
        const float ActorHeightOffset = 1.5f;
        const float ActorSphereDiameterMeters = 3f;

        readonly IGameWorldState gameWorldState;
        readonly Dictionary<Guid, GameObject> actorObjects = new();
        readonly HashSet<int> builtLayerIds = new();
        readonly Material groundWalkableMaterial;
        readonly Material groundBlockedMaterial;
        readonly Material dungeonWalkableMaterial;
        readonly Material adventurerMaterial;
        readonly Material monsterMaterial;
        readonly Material otherActorMaterial;
        readonly GameObject root;
        readonly GameObject tileRoot;
        readonly GameObject actorRoot;

        [Inject]
        public WorldActorDebugVisualizer(IGameWorldState gameWorldState)
        {
            this.gameWorldState = gameWorldState ?? throw new ArgumentNullException(nameof(gameWorldState));
            groundWalkableMaterial = CreateMaterial(new Color(0.24f, 0.32f, 0.24f, 0.45f));
            groundBlockedMaterial = CreateMaterial(new Color(0.25f, 0.25f, 0.25f, 0.65f));
            dungeonWalkableMaterial = CreateMaterial(new Color(0.18f, 0.20f, 0.26f, 0.65f));
            adventurerMaterial = CreateMaterial(new Color(0.1f, 0.45f, 1f, 1f));
            monsterMaterial = CreateMaterial(new Color(0.9f, 0.15f, 0.1f, 1f));
            otherActorMaterial = CreateMaterial(new Color(1f, 0.85f, 0.1f, 1f));

            root = new GameObject("WorldActorDebugVisualizer");
            tileRoot = new GameObject("Tiles");
            actorRoot = new GameObject("Actors");
            tileRoot.transform.SetParent(root.transform, false);
            actorRoot.transform.SetParent(root.transform, false);
        }

        public void UpdateVisuals()
        {
            if (!gameWorldState.IsInitialized)
            {
                return;
            }

            BuildMissingLayerTiles();
            UpdateActorObjects();
        }

        public void Dispose()
        {
            if (root != null)
            {
                UnityEngine.Object.Destroy(root);
            }
        }

        void BuildMissingLayerTiles()
        {
            if (!builtLayerIds.Contains(MapLayerId.Ground.Value))
            {
                BuildGroundTiles();
                builtLayerIds.Add(MapLayerId.Ground.Value);
            }

            foreach (var pair in gameWorldState.Dungeon.Floors)
            {
                var layerId = MapLayerId.DungeonFloor(pair.Key).Value;
                if (builtLayerIds.Contains(layerId))
                {
                    continue;
                }

                BuildDungeonTiles(pair.Value);
                builtLayerIds.Add(layerId);
            }
        }

        void BuildGroundTiles()
        {
            var layer = gameWorldState.GroundMap.Layer;
            var layerRoot = CreateLayerRoot("Ground", layer.Id);
            for (var z = 0; z < layer.Depth; z++)
            {
                for (var x = 0; x < layer.Width; x++)
                {
                    var position = new GridPosition(x, z);
                    var material = gameWorldState.GroundMap.IsWalkable(position)
                        ? groundWalkableMaterial
                        : groundBlockedMaterial;
                    CreateTile(layerRoot, layer, position, material);
                }
            }
        }

        void BuildDungeonTiles(DungeonFloor floor)
        {
            var layerRoot = CreateLayerRoot($"DungeonFloor{floor.FloorIndex}", floor.Layer.Id);
            for (var z = 0; z < floor.Layer.Depth; z++)
            {
                for (var x = 0; x < floor.Layer.Width; x++)
                {
                    var position = new GridPosition(x, z);
                    if (!floor.IsWalkable(position))
                    {
                        continue;
                    }

                    CreateTile(layerRoot, floor.Layer, position, dungeonWalkableMaterial);
                }
            }
        }

        Transform CreateLayerRoot(string layerName, MapLayerId layerId)
        {
            var layerRoot = new GameObject(layerName);
            layerRoot.transform.SetParent(tileRoot.transform, false);
            layerRoot.transform.position = new Vector3(0f, ResolveLayerY(layerId), 0f);
            return layerRoot.transform;
        }

        void CreateTile(Transform layerRoot, MapLayer layer, GridPosition position, Material material)
        {
            var tile = GameObject.CreatePrimitive(PrimitiveType.Plane);
            tile.name = $"Tile_{position.X}_{position.Z}";
            tile.transform.SetParent(layerRoot, false);
            tile.transform.position = ToUnityPosition(layer.GetCellCenter(position));
            tile.transform.localScale = Vector3.one * (GameConstants.MapCellSizeMeters / 10f);
            RemoveCollider(tile);
            ApplyMaterial(tile, material);
        }

        void UpdateActorObjects()
        {
            var activeActorIds = new HashSet<Guid>();
            foreach (var actor in gameWorldState.Actors)
            {
                activeActorIds.Add(actor.Id);
                var actorObject = GetOrCreateActorObject(actor);
                actorObject.transform.position = ToUnityPosition(actor.Position) + Vector3.up * ActorHeightOffset;
            }

            RemoveMissingActorObjects(activeActorIds);
        }

        GameObject GetOrCreateActorObject(Actor actor)
        {
            if (actorObjects.TryGetValue(actor.Id, out var actorObject))
            {
                return actorObject;
            }

            actorObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            actorObject.name = $"Actor_{actor.Id}";
            actorObject.transform.SetParent(actorRoot.transform, false);
            actorObject.transform.localScale = Vector3.one * ActorSphereDiameterMeters;
            RemoveCollider(actorObject);
            ApplyMaterial(actorObject, ResolveActorMaterial(actor));
            actorObjects.Add(actor.Id, actorObject);
            return actorObject;
        }

        void RemoveMissingActorObjects(HashSet<Guid> activeActorIds)
        {
            var removeActorIds = new List<Guid>();
            foreach (var pair in actorObjects)
            {
                if (!activeActorIds.Contains(pair.Key))
                {
                    removeActorIds.Add(pair.Key);
                }
            }

            foreach (var actorId in removeActorIds)
            {
                UnityEngine.Object.Destroy(actorObjects[actorId]);
                actorObjects.Remove(actorId);
            }
        }

        Vector3 ToUnityPosition(LayerPosition position)
        {
            return new Vector3(position.X, ResolveLayerY(position.LayerId), position.Z);
        }

        static float ResolveLayerY(MapLayerId layerId)
        {
            return layerId.Value * LayerHeightOffset;
        }

        Material ResolveActorMaterial(Actor actor)
        {
            if (actor.Behavior is AdventurerBehavior)
            {
                return adventurerMaterial;
            }

            if (actor.Behavior is MonsterBehavior)
            {
                return monsterMaterial;
            }

            return otherActorMaterial;
        }

        static Material CreateMaterial(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            var material = new Material(shader);
            material.color = color;
            return material;
        }

        static void ApplyMaterial(GameObject target, Material material)
        {
            var renderer = target.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }
        }

        static void RemoveCollider(GameObject target)
        {
            var collider = target.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.Destroy(collider);
            }
        }
    }
}
