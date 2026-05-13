using System;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Common;
using DungeonInn.Master;
using VContainer;

namespace DungeonInn.Application.UseCase
{
    public sealed class GrantExperienceService
    {
        readonly IMasterRepository masterRepository;
        readonly IEventPublisher eventPublisher;

        [Inject]
        public GrantExperienceService(IMasterRepository masterRepository, IEventPublisher eventPublisher)
        {
            this.masterRepository = masterRepository ?? throw new ArgumentNullException(nameof(masterRepository));
            this.eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        }

        public void Execute(Actor killer, Actor defeated)
        {
            Execute(killer, defeated, eventPublisher);
        }

        public void Execute(Actor killer, Actor defeated, IEventPublisher eventPublisher)
        {
            if (eventPublisher == null)
            {
                throw new ArgumentNullException(nameof(eventPublisher));
            }

            if (killer.ArchetypeId <= 0)
            {
                return;
            }

            var xpReward = Math.Max(
                GameConstants.KillExperienceRewardMinimum,
                defeated.Experience * GameConstants.KillExperienceRewardNumerator / GameConstants.KillExperienceRewardDenominator);
            var killerArchetype = masterRepository.GetActorArchetypeMaster(killer.ArchetypeId);
            var levelTable = masterRepository.GetLevelTable(killerArchetype.LevelTableId);
            var oldLevel = killer.Level;

            killer.GainExperience(xpReward);
            eventPublisher.Publish(new ExperienceGranted(killer.Id, xpReward, killer.Experience));

            killer.RecalculateLevel(levelTable);
            if (oldLevel < killer.Level)
            {
                eventPublisher.Publish(new ActorLeveledUp(killer.Id, oldLevel, killer.Level));
            }
        }
    }
}
