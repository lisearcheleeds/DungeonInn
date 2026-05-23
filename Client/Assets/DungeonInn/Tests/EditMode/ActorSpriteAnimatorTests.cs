using System;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.View.Scene.MainScene.World;
using NUnit.Framework;
using R3;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

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

            UnityEngine.Object.DestroyImmediate(walkClip);
        }

        [Test]
        public void HitOneShotReportsCompleteAtFinalFrame()
        {
            var hitClip = CreateClip(2f, false, 3, 4);
            var animator = new ActorSpriteAnimator();
            animator.Setup(null, null);
            animator.SetupCombatClips(null, hitClip, null);
            animator.SetState(ActorAnimationState.Hit);

            Assert.That(animator.IsHitOneShotComplete, Is.False);

            animator.Tick(0.5f);
            Assert.That(animator.CurrentFrameIndex, Is.EqualTo(4));
            Assert.That(animator.IsHitOneShotComplete, Is.True);

            UnityEngine.Object.DestroyImmediate(hitClip);
        }

        [Test]
        public void MissingCombatClipFallsBackToIdleFrame()
        {
            var idleClip = CreateClip(4f, true, 7);
            var animator = new ActorSpriteAnimator();
            animator.Setup(idleClip, null);
            animator.SetupCombatClips(null, null, null);
            animator.SetState(ActorAnimationState.Combat);
            LogAssert.Expect(
                LogType.Warning,
                "[ActorSpriteAnimator] Combat animation clip is not assigned. Falling back to idle clip.");

            animator.Tick(1f);

            Assert.That(animator.CurrentFrameIndex, Is.EqualTo(7));

            UnityEngine.Object.DestroyImmediate(idleClip);
        }

        [Test]
        public void CombatAnimationOverrideClearsWhenEncounterEnds()
        {
            var subscriber = new ManualEventSubscriber();
            var presenter = new ActorCombatAnimationPresenter(subscriber);
            var attackerId = Guid.NewGuid();
            var targetId = Guid.NewGuid();

            try
            {
                presenter.Initialize();

                subscriber.Publish(new CombatAttackOccurred(attackerId, targetId, 1, 9));
                Assert.That(presenter.TryGetOverride(attackerId, out var attackState), Is.True);
                Assert.That(attackState, Is.EqualTo(ActorAnimationState.Combat));

                subscriber.Publish(new CombatEncounterEnded(attackerId));

                Assert.That(presenter.TryGetOverride(attackerId, out _), Is.False);
            }
            finally
            {
                presenter.Dispose();
                subscriber.Dispose();
            }
        }

        [Test]
        public void CombatEncounterEndDoesNotClearDeadAnimationOverride()
        {
            var subscriber = new ManualEventSubscriber();
            var presenter = new ActorCombatAnimationPresenter(subscriber);
            var actorId = Guid.NewGuid();

            try
            {
                presenter.Initialize();

                subscriber.Publish(new ActorDefeated(actorId, null, DeathCause.Combat));
                subscriber.Publish(new CombatEncounterEnded(actorId));

                Assert.That(presenter.TryGetOverride(actorId, out var state), Is.True);
                Assert.That(state, Is.EqualTo(ActorAnimationState.Dead));
            }
            finally
            {
                presenter.Dispose();
                subscriber.Dispose();
            }
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

        sealed class ManualEventSubscriber : IEventSubscriber, IDisposable
        {
            readonly Subject<IGameEvent> subject = new();

            public Observable<T> OnEvent<T>() where T : class, IGameEvent
            {
                return subject.Where(gameEvent => gameEvent is T).Select(gameEvent => (T)(object)gameEvent);
            }

            public void Publish(IGameEvent gameEvent)
            {
                subject.OnNext(gameEvent);
            }

            public void Dispose()
            {
                subject.Dispose();
            }
        }
    }
}
