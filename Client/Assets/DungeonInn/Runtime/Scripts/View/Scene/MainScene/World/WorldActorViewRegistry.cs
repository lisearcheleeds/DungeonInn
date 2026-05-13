using System;
using System.Collections.Generic;
using DungeonInn.Domain.Actor;
using UnityEngine;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class WorldActorViewRegistry : IDisposable
    {
        const float ActorSphereDiameterMeters = 3f;

        readonly WorldViewRoot viewRoot;
        readonly Dictionary<Guid, GameObject> actorObjects = new();

        public WorldActorViewRegistry(WorldViewRoot viewRoot)
        {
            this.viewRoot = viewRoot ?? throw new ArgumentNullException(nameof(viewRoot));
        }

        public GameObject GetOrCreateActorObject(Actor actor, Material material)
        {
            if (actorObjects.TryGetValue(actor.Id, out var actorObject))
            {
                return actorObject;
            }

            actorObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            actorObject.name = $"Actor_{actor.Id}";
            actorObject.transform.SetParent(viewRoot.ActorRoot, false);
            actorObject.transform.localScale = Vector3.one * ActorSphereDiameterMeters;
            RemoveCollider(actorObject);
            ApplyMaterial(actorObject, material);
            actorObjects.Add(actor.Id, actorObject);
            return actorObject;
        }

        public void RemoveMissingActorObjects(HashSet<Guid> activeActorIds)
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

        public void Dispose()
        {
            foreach (var actorObject in actorObjects.Values)
            {
                if (actorObject != null)
                {
                    UnityEngine.Object.Destroy(actorObject);
                }
            }

            actorObjects.Clear();
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
