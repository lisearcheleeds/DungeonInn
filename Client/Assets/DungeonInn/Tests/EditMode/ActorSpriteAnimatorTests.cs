using System;
using System.Collections.Generic;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.View.Scene.MainScene.World;
using NUnit.Framework;
using R3;
using UnityEngine;

namespace DungeonInn.Tests.EditMode
{
    public sealed class ActorSpriteAnimatorTests
    {
        [Test]
        public void WalkAnimationUsesClipFrameIndicesAndFps()
        {
            var firstSprite = CreateSprite();
            var secondSprite = CreateSprite();
            var definition = CreateDefinition(
                ActorAnimationKey.Walk,
                ActorAnimationDirection.SE,
                new ActorVisualAnimationClip(2f, true, new[] { firstSprite, secondSprite }));
            var animator = new ActorSpriteAnimator();
            animator.ApplyVisual(definition);
            animator.SetState(ActorAnimationState.Walk);

            animator.Tick(0.5f, ActorAnimationDirection.SE);
            Assert.That(animator.GetCurrentSprite(ActorAnimationDirection.SE), Is.EqualTo(secondSprite));

            animator.Tick(0.5f, ActorAnimationDirection.SE);
            Assert.That(animator.GetCurrentSprite(ActorAnimationDirection.SE), Is.EqualTo(firstSprite));

            UnityEngine.Object.DestroyImmediate(firstSprite.texture);
            UnityEngine.Object.DestroyImmediate(firstSprite);
            UnityEngine.Object.DestroyImmediate(secondSprite.texture);
            UnityEngine.Object.DestroyImmediate(secondSprite);
        }

        [Test]
        public void DamageOneShotReportsCompleteAtFinalFrame()
        {
            var firstSprite = CreateSprite();
            var secondSprite = CreateSprite();
            var definition = CreateDefinition(
                ActorAnimationKey.Damage,
                ActorAnimationDirection.SE,
                new ActorVisualAnimationClip(2f, false, new[] { firstSprite, secondSprite }));
            var animator = new ActorSpriteAnimator();
            animator.ApplyVisual(definition);
            animator.SetState(ActorAnimationState.Damage);

            Assert.That(animator.IsDamageOneShotComplete(ActorAnimationDirection.SE), Is.False);

            animator.Tick(0.5f, ActorAnimationDirection.SE);
            Assert.That(animator.GetCurrentSprite(ActorAnimationDirection.SE), Is.EqualTo(secondSprite));
            Assert.That(animator.IsDamageOneShotComplete(ActorAnimationDirection.SE), Is.True);

            UnityEngine.Object.DestroyImmediate(firstSprite.texture);
            UnityEngine.Object.DestroyImmediate(firstSprite);
            UnityEngine.Object.DestroyImmediate(secondSprite.texture);
            UnityEngine.Object.DestroyImmediate(secondSprite);
        }

        [Test]
        public void DamageOneShotCompletionUsesRequestedDirectionClip()
        {
            var seSprite = CreateSprite();
            var firstNwSprite = CreateSprite();
            var secondNwSprite = CreateSprite();
            var clips = new Dictionary<ActorVisualAnimationKey, ActorVisualAnimationClip>
            {
                {
                    new ActorVisualAnimationKey(ActorAnimationKey.Damage, ActorAnimationDirection.SE),
                    new ActorVisualAnimationClip(2f, false, new[] { seSprite })
                },
                {
                    new ActorVisualAnimationKey(ActorAnimationKey.Damage, ActorAnimationDirection.NW),
                    new ActorVisualAnimationClip(2f, false, new[] { firstNwSprite, secondNwSprite })
                }
            };
            var definition = new ActorVisualDefinition("test_visual", ActorVisualSizeTier.AdventurerS, clips);
            var animator = new ActorSpriteAnimator();
            animator.ApplyVisual(definition);
            animator.SetState(ActorAnimationState.Damage);

            Assert.That(animator.IsDamageOneShotComplete(ActorAnimationDirection.SE), Is.True);
            Assert.That(animator.IsDamageOneShotComplete(ActorAnimationDirection.NW), Is.False);

            UnityEngine.Object.DestroyImmediate(seSprite.texture);
            UnityEngine.Object.DestroyImmediate(seSprite);
            UnityEngine.Object.DestroyImmediate(firstNwSprite.texture);
            UnityEngine.Object.DestroyImmediate(firstNwSprite);
            UnityEngine.Object.DestroyImmediate(secondNwSprite.texture);
            UnityEngine.Object.DestroyImmediate(secondNwSprite);
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
                Assert.That(attackState, Is.EqualTo(ActorAnimationState.Attack));

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

        static ActorVisualDefinition CreateDefinition(
            ActorAnimationKey animationKey,
            ActorAnimationDirection direction,
            ActorVisualAnimationClip clip)
        {
            var clips = new Dictionary<ActorVisualAnimationKey, ActorVisualAnimationClip>
            {
                { new ActorVisualAnimationKey(animationKey, direction), clip }
            };

            return new ActorVisualDefinition("test_visual", ActorVisualSizeTier.AdventurerS, clips);
        }

        static Sprite CreateSprite()
        {
            var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            return Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0f), 16f);
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

