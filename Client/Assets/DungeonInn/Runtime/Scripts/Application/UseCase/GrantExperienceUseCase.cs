using System;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Common;
using DungeonInn.Master;
using VContainer;


namespace DungeonInn.Application.UseCase
{
    public sealed class GrantExperienceUseCase
    {
        readonly IMasterRepository masterRepository;
        readonly IEventPublisher eventBus;

        [Inject]
        public GrantExperienceUseCase(IMasterRepository masterRepository, IEventPublisher eventBus)
        {
            this.masterRepository = masterRepository ?? throw new ArgumentNullException(nameof(masterRepository));
            this.eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        public void Execute(Actor killer, Actor defeated)
        {
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
            eventBus.Publish(new ExperienceGranted(killer.Id, xpReward, killer.Experience));

            killer.RecalculateLevel(levelTable);
            if (killer.Level > oldLevel)
            {
                eventBus.Publish(new ActorLeveledUp(killer.Id, oldLevel, killer.Level));
            }
        }
    }
}
