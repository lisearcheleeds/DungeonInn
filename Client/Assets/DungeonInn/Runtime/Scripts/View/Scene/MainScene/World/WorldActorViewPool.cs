using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class WorldActorViewPool : IDisposable
    {
        readonly Stack<WorldActorView> inactiveViews = new();
        readonly List<WorldActorView> createdViews = new();
        bool disposed;

        public int InactiveCount => inactiveViews.Count;
        public int CreatedCount => createdViews.Count;

        public WorldActorView Rent(Guid actorId, Sprite sprite)
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(WorldActorViewPool));
            }

            var actorView = inactiveViews.Count == 0
                ? CreateView()
                : inactiveViews.Pop();
            actorView.Reset(sprite);
#if DEBUG
            actorView.ActorObject.name = $"Actor_{actorId}";
#else
            actorView.ActorObject.name = "Actor";
#endif
            actorView.ActorObject.layer = WorldRenderingLayer.Layer;
            actorView.ActorObject.SetActive(true);
            return actorView;
        }

        public void Return(WorldActorView actorView)
        {
            if (actorView == null || actorView.ActorObject == null)
            {
                return;
            }

            actorView.ActorObject.SetActive(false);
            actorView.ActorObject.transform.SetParent(null, false);
            actorView.Reset(null);

            if (disposed)
            {
                DestroyActorObject(actorView.ActorObject);
                return;
            }

            inactiveViews.Push(actorView);
        }

        public void Dispose()
        {
            disposed = true;
            foreach (var actorView in createdViews)
            {
                if (actorView.ActorObject != null)
                {
                    DestroyActorObject(actorView.ActorObject);
                }
            }

            inactiveViews.Clear();
            createdViews.Clear();
        }

        WorldActorView CreateView()
        {
            var actorObject = new GameObject("Actor");
            actorObject.layer = WorldRenderingLayer.Layer;
            var spriteRenderer = actorObject.AddComponent<SpriteRenderer>();
            var actorView = new WorldActorView(actorObject, spriteRenderer);
            createdViews.Add(actorView);
            return actorView;
        }

        static void DestroyActorObject(GameObject actorObject)
        {
#if UNITY_EDITOR
            if (!UnityEngine.Application.isPlaying)
            {
                UnityEngine.Object.DestroyImmediate(actorObject);
                return;
            }
#endif
            UnityEngine.Object.Destroy(actorObject);
        }
    }
}
