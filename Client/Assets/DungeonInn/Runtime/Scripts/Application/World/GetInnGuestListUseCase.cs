using System;
using System.Collections.Generic;
using DungeonInn.Application.Actors.Lifecycle;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Map;
using DungeonInn.Master;
using VContainer;

namespace DungeonInn.Application.World
{
    public sealed class GetInnGuestListUseCase
    {
        readonly IGameWorldStateReader worldState;
        readonly IMasterRepository masterRepository;
        readonly AdventurerRecoveryStateService recoveryStateService;
        readonly IWorldGameSettingsRepository worldGameSettingsRepository;

        [Inject]
        public GetInnGuestListUseCase(
            IGameWorldStateReader worldState,
            IMasterRepository masterRepository,
            AdventurerRecoveryStateService recoveryStateService,
            IWorldGameSettingsRepository worldGameSettingsRepository)
        {
            this.worldState = worldState ?? throw new ArgumentNullException(nameof(worldState));
            this.masterRepository = masterRepository ?? throw new ArgumentNullException(nameof(masterRepository));
            this.recoveryStateService =
                recoveryStateService ?? throw new ArgumentNullException(nameof(recoveryStateService));
            this.worldGameSettingsRepository =
                worldGameSettingsRepository ?? throw new ArgumentNullException(nameof(worldGameSettingsRepository));
        }

        public bool CanExecute => worldState.IsInitialized;

        public void Execute(List<InnGuestSummary> buffer)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            buffer.Clear();
            if (!CanExecute)
            {
                return;
            }

            var innBalanceSettings = worldGameSettingsRepository.GetInnBalanceSettings();
            foreach (var actor in worldState.Actors)
            {
                if (actor.Behavior is not AdventurerBehavior adventurerBehavior ||
                    adventurerBehavior.LifecycleState != AdventurerLifecycleState.Recovering)
                {
                    continue;
                }

                if (!actor.Position.LayerId.Equals(MapLayerId.Ground))
                {
                    continue;
                }

                if (!worldState.Guild.HasActiveInnReservation(actor.Id))
                {
                    continue;
                }

                var maxHp = actor.Params.MaxHp;
                var hpRatio = maxHp <= 0 ? 0f : (float)actor.Hp / maxHp;
                var remainingSeconds = recoveryStateService.GetRemainingSeconds(
                    actor.Id,
                    actor.Hp,
                    maxHp,
                    innBalanceSettings.HpRecoveryPercentPerMinute);
                var archetypeMaster = masterRepository.GetActorArchetypeMaster(actor.ArchetypeId);
                buffer.Add(new InnGuestSummary(
                    actor.Id,
                    archetypeMaster.Name,
                    hpRatio,
                    remainingSeconds));
            }
        }
    }
}
