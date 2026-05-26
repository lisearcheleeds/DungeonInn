using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonInn.View.Scene.MainScene.World
{
    [CreateAssetMenu(menuName = "DungeonInn/Visual/ActorVisualDefinition")]
    public sealed class ActorVisualDefinitionSO : ScriptableObject
    {
        [SerializeField] string visualId;
        [SerializeField] ActorVisualSizeTier visualSizeTier = ActorVisualSizeTier.AdventurerS;
        [SerializeField] ActorVisualAnimationEntry[] idleEntries = Array.Empty<ActorVisualAnimationEntry>();
        [SerializeField] ActorVisualAnimationEntry[] walkEntries = Array.Empty<ActorVisualAnimationEntry>();
        [SerializeField] ActorVisualAnimationEntry[] workEntries = Array.Empty<ActorVisualAnimationEntry>();
        [SerializeField] ActorVisualAnimationEntry[] attackEntries = Array.Empty<ActorVisualAnimationEntry>();
        [SerializeField] ActorVisualAnimationEntry[] damageEntries = Array.Empty<ActorVisualAnimationEntry>();
        [SerializeField] ActorVisualAnimationEntry[] deadEntries = Array.Empty<ActorVisualAnimationEntry>();

        public string VisualId => visualId;
        public ActorVisualSizeTier VisualSizeTier => visualSizeTier;
        public IReadOnlyList<ActorVisualAnimationEntry> IdleEntries => idleEntries ?? Array.Empty<ActorVisualAnimationEntry>();
        public IReadOnlyList<ActorVisualAnimationEntry> WalkEntries => walkEntries ?? Array.Empty<ActorVisualAnimationEntry>();
        public IReadOnlyList<ActorVisualAnimationEntry> WorkEntries => workEntries ?? Array.Empty<ActorVisualAnimationEntry>();
        public IReadOnlyList<ActorVisualAnimationEntry> AttackEntries => attackEntries ?? Array.Empty<ActorVisualAnimationEntry>();
        public IReadOnlyList<ActorVisualAnimationEntry> DamageEntries => damageEntries ?? Array.Empty<ActorVisualAnimationEntry>();
        public IReadOnlyList<ActorVisualAnimationEntry> DeadEntries => deadEntries ?? Array.Empty<ActorVisualAnimationEntry>();

        public IReadOnlyList<ActorVisualAnimationEntry> GetEntries(ActorAnimationKey animationKey)
        {
            switch (animationKey)
            {
                case ActorAnimationKey.Idle:
                    return IdleEntries;
                case ActorAnimationKey.Walk:
                    return WalkEntries;
                case ActorAnimationKey.Work:
                    return WorkEntries;
                case ActorAnimationKey.Attack:
                    return AttackEntries;
                case ActorAnimationKey.Damage:
                    return DamageEntries;
                case ActorAnimationKey.Dead:
                    return DeadEntries;
                default:
                    return Array.Empty<ActorVisualAnimationEntry>();
            }
        }

        public ActorVisualDefinition ToRuntimeDefinition()
        {
            return ActorVisualDefinitionFactory.Create(this);
        }
    }
}
