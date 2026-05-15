using DungeonInn.Application.World;
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Application.GameLoop;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Common;
using DungeonInn.Domain.Guild;
using DungeonInn.Domain.Map;
using VContainer;

namespace DungeonInn.Application.Actors.Lifecycle
{
    public sealed class RecoverAdventurerAtInnUseCase
    {
        readonly IEventPublisher eventPublisher;
        readonly IGameClock gameClock;
        readonly AdventurerRecoveryStateService recoveryStateService;
        readonly ActorProcessingCandidateService candidateService;
        readonly List<Guid> actorIdBuffer = new();

        [Inject]
        public RecoverAdventurerAtInnUseCase(
            IEventPublisher eventPublisher,
            IGameClock gameClock,
            AdventurerRecoveryStateService recoveryStateService,
            ActorProcessingCandidateService candidateService)
        {
            this.eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
            this.gameClock = gameClock ?? throw new ArgumentNullException(nameof(gameClock));
            this.recoveryStateService = recoveryStateService ?? throw new ArgumentNullException(nameof(recoveryStateService));
            this.candidateService = candidateService ?? throw new ArgumentNullException(nameof(candidateService));
        }

        public UniTask ExecuteAsync(IGameWorldState worldState, float deltaGameSeconds)
        {
            if (worldState == null)
            {
                throw new ArgumentNullException(nameof(worldState));
            }

            var guild = worldState.Guild;
            candidateService.CollectRecoveryCandidates(actorIdBuffer);
            foreach (var actorId in actorIdBuffer)
            {
                var actor = worldState.FindActor(actorId);
                if (actor == null)
                {
                    candidateService.RemoveActor(actorId);
                    continue;
                }

                if (actor.Behavior is not AdventurerBehavior behavior)
                {
                    candidateService.RemoveActor(actor.Id);
                    continue;
                }

                if (behavior.LifecycleState != AdventurerLifecycleState.Recovering)
                {
                    candidateService.ClearRecoveryCandidate(actor.Id);
                    continue;
                }

                if (!actor.Position.LayerId.Equals(MapLayerId.Ground))
                {
                    candidateService.ClearRecoveryCandidate(actor.Id);
                    continue;
                }

                TickRecovery(guild, actor, behavior, deltaGameSeconds);
            }

            return UniTask.CompletedTask;
        }

        void TickRecovery(AdventurerGuild guild, Actor actor, AdventurerBehavior behavior, float deltaGameSeconds)
        {
            if (!guild.HasActiveInnReservation(actor.Id))
            {
                return;
            }

            var accumulated = recoveryStateService.GetAccumulatedHp(actor.Id);
            accumulated += actor.Params.MaxHp * GameConstants.InnHpRecoveryPercentPerMinute / 60f * deltaGameSeconds;
            var healAmount = (int)accumulated;

            if (healAmount > 0)
            {
                actor.Recover(healAmount, 0, 0, 0, 0);
                accumulated -= healAmount;
                eventPublisher.Publish(new ActorRecoveringAtInn(actor.Id, actor.Hp, actor.Params.MaxHp));
            }

            recoveryStateService.SetAccumulatedHp(actor.Id, accumulated);

            if (actor.Hp >= actor.Params.MaxHp)
            {
                recoveryStateService.Remove(actor.Id);
                candidateService.ClearRecoveryCandidate(actor.Id);
                guild.ReleaseInnReservation(actor.Id, gameClock.CurrentScheduleTick);
                behavior.ChangeLifecycleState(AdventurerLifecycleState.Preparing);
                eventPublisher.Publish(new ActorFullyRecovered(actor.Id));
            }
        }
    }
}
