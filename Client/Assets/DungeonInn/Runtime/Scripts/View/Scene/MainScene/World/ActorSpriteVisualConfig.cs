using System;
using System.Collections.Generic;
using DungeonInn.Domain.Actor;
using UnityEngine;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class ActorSpriteVisualConfig : IDisposable
    {
        readonly Dictionary<ActorBehaviorType, Material> debugMaterials = new();

        public ActorSpriteVisualConfig()
        {
            Add(ActorBehaviorType.Adventurer, new Color(0.1f, 0.45f, 1f, 1f));
            Add(ActorBehaviorType.Monster, new Color(0.9f, 0.15f, 0.1f, 1f));
            Add(ActorBehaviorType.None, new Color(1f, 0.85f, 0.1f, 1f));
        }

        public Material GetDebugMaterial(Actor actor)
        {
            var behaviorType = ResolveBehaviorType(actor);
            if (!debugMaterials.TryGetValue(behaviorType, out var material))
            {
                return debugMaterials[ActorBehaviorType.None];
            }

            return material;
        }

        public void Dispose()
        {
            foreach (var material in debugMaterials.Values)
            {
                WorldDebugMaterialFactory.Dispose(material);
            }

            debugMaterials.Clear();
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

        void Add(ActorBehaviorType behaviorType, Color color)
        {
            debugMaterials.Add(behaviorType, WorldDebugMaterialFactory.Create(color));
        }
    }
}
