using System;
using System.Collections.Generic;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Application.GameLoop;
using DungeonInn.Application.World;
using DungeonInn.Domain.Commerce;
using DungeonInn.Master;
using VContainer;

namespace DungeonInn.Application.Economy
{
    public sealed class UpgradeFacilityUseCase
    {
        readonly IGameWorldStateReader worldState;
        readonly IFacilityUpgradeMasterRepository facilityUpgradeMasterRepository;
        readonly GuildCombinedInventoryViewService combinedInventoryViewService;
        readonly GuildInventoryWithdrawalService withdrawalService;
        readonly IGameClock gameClock;
        readonly IEventPublisher eventPublisher;

        [Inject]
        public UpgradeFacilityUseCase(
            IGameWorldStateReader worldState,
            IFacilityUpgradeMasterRepository facilityUpgradeMasterRepository,
            GuildCombinedInventoryViewService combinedInventoryViewService,
            GuildInventoryWithdrawalService withdrawalService,
            IGameClock gameClock,
            IEventPublisher eventPublisher)
        {
            this.worldState = worldState ?? throw new ArgumentNullException(nameof(worldState));
            this.facilityUpgradeMasterRepository = facilityUpgradeMasterRepository
                ?? throw new ArgumentNullException(nameof(facilityUpgradeMasterRepository));
            this.combinedInventoryViewService =
                combinedInventoryViewService ?? throw new ArgumentNullException(nameof(combinedInventoryViewService));
            this.withdrawalService = withdrawalService ?? throw new ArgumentNullException(nameof(withdrawalService));
            this.gameClock = gameClock ?? throw new ArgumentNullException(nameof(gameClock));
            this.eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        }

        public FacilityUpgradeResult Execute(Guid facilityId)
        {
            var facility = worldState.Guild.GetFacility(facilityId);
            if (!facilityUpgradeMasterRepository.TryGetFacilityUpgradeMaster(
                    facility.Type,
                    facility.Level,
                    out var upgradeMaster))
            {
                throw new InvalidOperationException("Facility has no next upgrade.");
            }

            if (!combinedInventoryViewService.HasAll(upgradeMaster.Costs))
            {
                throw new InvalidOperationException("Guild combined inventory does not satisfy upgrade cost.");
            }

            var previousLevel = facility.Level;
            var withdrawalResult = withdrawalService.Withdraw(upgradeMaster.Costs);
            facility.UpgradeTo(upgradeMaster.ToLevel, upgradeMaster.Quality, upgradeMaster.Capacity);
            var transactions = RecordTransactions(facility.Id, withdrawalResult);
            eventPublisher.Publish(new FacilityUpgraded(
                facility.Id,
                facility.Type,
                previousLevel,
                facility.Level));
            return new FacilityUpgradeResult(
                facility.Id,
                previousLevel,
                facility.Level,
                withdrawalResult,
                transactions);
        }

        IReadOnlyList<ExchangeTransaction> RecordTransactions(
            Guid facilityId,
            GuildInventoryWithdrawalResult withdrawalResult)
        {
            var transactions = new List<ExchangeTransaction>();
            foreach (var source in withdrawalResult.Sources)
            {
                var transaction = new ExchangeTransaction(
                    Guid.NewGuid(),
                    source.SourceId,
                    facilityId,
                    new[] { source.ItemStack },
                    Array.Empty<Domain.Item.ItemStack>(),
                    gameClock.CurrentScheduleTick);
                worldState.Guild.RecordTransaction(transaction);
                transactions.Add(transaction);
            }

            return transactions;
        }
    }
}
