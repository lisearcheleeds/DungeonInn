using System;
using System.Collections.Generic;

namespace DungeonInn.View.Scene.MainScene.World
{
    public static class ActorVisualDefinitionFactory
    {
        public static ActorVisualDefinition Create(ActorVisualDefinitionSO source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var clips = new Dictionary<ActorVisualAnimationKey, ActorVisualAnimationClip>();
            foreach (ActorAnimationKey animationKey in Enum.GetValues(typeof(ActorAnimationKey)))
            {
                foreach (var entry in source.GetEntries(animationKey))
                {
                    if (entry == null)
                    {
                        continue;
                    }

                    var key = new ActorVisualAnimationKey(animationKey, entry.Direction);
                    if (clips.ContainsKey(key))
                    {
                        throw new InvalidOperationException(
                            $"Actor visual definition has duplicate animation entry. VisualId={source.VisualId} Key={animationKey} Direction={entry.Direction}");
                    }

                    clips.Add(
                        key,
                        new ActorVisualAnimationClip(
                            entry.Fps,
                            entry.Loop,
                            entry.Sprites));
                }
            }

            ValidateRequiredClips(source, clips);
            return new ActorVisualDefinition(source.VisualId, source.VisualSizeTier, clips);
        }

        static void ValidateRequiredClips(
            ActorVisualDefinitionSO source,
            IReadOnlyDictionary<ActorVisualAnimationKey, ActorVisualAnimationClip> clips)
        {
            foreach (ActorAnimationKey animationKey in Enum.GetValues(typeof(ActorAnimationKey)))
            {
                foreach (ActorAnimationDirection direction in Enum.GetValues(typeof(ActorAnimationDirection)))
                {
                    var key = new ActorVisualAnimationKey(animationKey, direction);
                    if (!clips.TryGetValue(key, out var clip))
                    {
                        throw new InvalidOperationException(
                            $"Actor visual definition is missing animation entry. VisualId={source.VisualId} Key={animationKey} Direction={direction}");
                    }

                    if (clip == null || clip.FrameCount <= 0)
                    {
                        throw new InvalidOperationException(
                            $"Actor visual definition has empty animation clip. VisualId={source.VisualId} Key={animationKey} Direction={direction}");
                    }
                }
            }
        }
    }
}
