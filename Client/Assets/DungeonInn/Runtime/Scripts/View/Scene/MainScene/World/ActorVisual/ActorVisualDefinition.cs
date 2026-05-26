using System;
using System.Collections.Generic;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class ActorVisualDefinition
    {
        readonly Dictionary<ActorVisualAnimationKey, ActorVisualAnimationClip> clips;

        public ActorVisualDefinition(
            string visualId,
            ActorVisualSizeTier visualSizeTier,
            IReadOnlyDictionary<ActorVisualAnimationKey, ActorVisualAnimationClip> clips)
        {
            if (string.IsNullOrWhiteSpace(visualId))
            {
                throw new ArgumentException("Actor visual id is required.", nameof(visualId));
            }

            VisualId = visualId;
            VisualSizeTier = visualSizeTier;
            this.clips = new Dictionary<ActorVisualAnimationKey, ActorVisualAnimationClip>(
                clips ?? throw new ArgumentNullException(nameof(clips)));
        }

        public string VisualId { get; }
        public ActorVisualSizeTier VisualSizeTier { get; }

        public bool TryGetClip(
            ActorAnimationKey animationKey,
            ActorAnimationDirection direction,
            out ActorVisualAnimationClip clip)
        {
            return clips.TryGetValue(new ActorVisualAnimationKey(animationKey, direction), out clip);
        }
    }
}
