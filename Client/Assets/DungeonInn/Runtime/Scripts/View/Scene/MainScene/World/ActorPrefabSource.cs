using System;
using UnityEngine;
using VContainer;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class ActorPrefabSource : IDisposable
    {
        readonly bool owned;

        [Inject]
        public ActorPrefabSource(VisualConfigSettings settings)
        {
            var configPrefab = settings?.ActorSpriteVisualConfigSO?.ActorPrefab;
            if (configPrefab != null)
            {
                Prefab = configPrefab;
                owned = false;
                return;
            }

            var actorObject = new GameObject("ActorPrefab_Default");
            actorObject.layer = WorldRenderingLayer.Layer;
            actorObject.AddComponent<SpriteRenderer>();
            Prefab = actorObject.AddComponent<ActorView>();
            actorObject.SetActive(false);
            owned = true;
        }

        public ActorView Prefab { get; }

        public void Dispose()
        {
            if (!owned || Prefab == null)
            {
                return;
            }

            DestroyPrefabObject(Prefab.gameObject);
        }

        static void DestroyPrefabObject(GameObject prefabObject)
        {
#if UNITY_EDITOR
            if (!UnityEngine.Application.isPlaying)
            {
                UnityEngine.Object.DestroyImmediate(prefabObject);
                return;
            }
#endif
            UnityEngine.Object.Destroy(prefabObject);
        }
    }
}
