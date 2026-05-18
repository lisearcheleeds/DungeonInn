using DungeonInn.View.Scene.MainScene.World;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DungeonInn.Tests.EditMode
{
    public sealed class ActorSpriteAnimatorTests
    {
        [Test]
        public void WalkAnimationUsesClipFrameIndicesAndFps()
        {
            var walkClip = CreateClip(2f, true, 0, 1);
            var animator = new ActorSpriteAnimator();
            animator.Setup(null, walkClip);
            animator.SetState(ActorAnimationState.Walk);

            animator.Tick(0.5f);
            Assert.That(animator.CurrentFrameIndex, Is.EqualTo(1));

            animator.Tick(0.5f);
            Assert.That(animator.CurrentFrameIndex, Is.EqualTo(0));

            Object.DestroyImmediate(walkClip);
        }

        static ActorSpriteAnimationClip CreateClip(float fps, bool loop, params int[] frameIndices)
        {
            var clip = ScriptableObject.CreateInstance<ActorSpriteAnimationClip>();
            using var serializedClip = new SerializedObject(clip);
            serializedClip.FindProperty("fps").floatValue = fps;
            serializedClip.FindProperty("loop").boolValue = loop;
            var frameIndicesProp = serializedClip.FindProperty("frameIndices");
            frameIndicesProp.ClearArray();
            for (var index = 0; index < frameIndices.Length; index++)
            {
                frameIndicesProp.InsertArrayElementAtIndex(index);
                frameIndicesProp.GetArrayElementAtIndex(index).intValue = frameIndices[index];
            }

            serializedClip.ApplyModifiedPropertiesWithoutUndo();
            return clip;
        }
    }
}
