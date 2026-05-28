using System;
using System.Linq;
using DungeonInn.Application.Actors.Lifecycle;
using DungeonInn.Application.Actors.Movement;
using DungeonInn.Application.Combat;
using DungeonInn.Application.Dungeons;
using DungeonInn.Application.Economy;
using DungeonInn.Application.Event;
using DungeonInn.Application.Items;
using DungeonInn.Application.World;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Common;
using DungeonInn.Domain.Dungeon;
using DungeonInn.Domain.Facility;
using DungeonInn.Domain.Guild;
using DungeonInn.Domain.Item;
using DungeonInn.Domain.Map;
using DungeonInn.Master;
using NUnit.Framework;
using R3;

namespace DungeonInn.Tests.EditMode
{
    public sealed class Milestone9CompletionReviewFixTests
    {
        [Test]
        public void FacilityLineupChangesWhenFacilityLevelIncreases()
        {
            var repository = new HardcodedMasterRepository();
            var useCase = new GetFacilityLineupUseCase(repository, repository);

            var levelOneLineup = useCase.Execute(FacilityType.GeneralStore, 1);
            var levelTwoLineup = useCase.Execute(FacilityType.GeneralStore, 2);

            Assert.That(levelOneLineup.Select(x => x.ItemId), Has.Member(2001));
            Assert.That(levelOneLineup.Select(x => x.ItemId), Has.No.Member(2002));
            Assert.That(levelTwoLineup.Select(x => x.ItemId), Has.Member(2002));
        }

        [Test]
        public void FacilityEffectUsesInnQualityForRecoveryRate()
        {
            var facility = new Facility(
                Guid.NewGuid(),
                FacilityType.Inn,
                "Test Inn",
                10,
                1,
                new Inventory(new FixedItemStackLimitResolver()));
            var service = new FacilityEffectService();
            var innBalanceSettings = InnBalanceSettings.CreateDefault();

            var levelOneRate = service.CalculateInnHpRecoveryPercentPerMinute(facility, innBalanceSettings);
            facility.UpgradeTo(2, 2, 2);
            var levelTwoRate = service.CalculateInnHpRecoveryPercentPerMinute(facility, innBalanceSettings);

            Assert.That(levelTwoRate, Is.EqualTo(levelOneRate * 2f));
        }

        [Test]
        public void DungeonLayerInfoIncludesGroundLayer()
        {
            var repository = new HardcodedMasterRepository();
            var worldState = CreateInitializedWorldState(repository);
            var useCase = new GetDungeonLayerInfoUseCase(worldState, repository);

            var layers = useCase.Execute();

            Assert.That(layers[0].FloorIndex, Is.EqualTo(0));
            Assert.That(layers[0].IsGenerated, Is.True);
        }

        [Test]
        public void MarketOffersRespectRequiredTotalFacilityLevel()
        {
            var repository = new HardcodedMasterRepository();
            var worldState = CreateInitializedWorldState(repository);
            var combinedInventoryViewService = new GuildCombinedInventoryViewService(worldState, repository);
            var progressService = new GuildProgressService(worldState);
            var useCase = new GetMarketOffersUseCase(
                repository,
                repository,
                combinedInventoryViewService,
                progressService);

            Assert.That(useCase.Execute().Select(x => x.OfferId), Is.EqualTo(new[] { 1 }));

            worldState.Guild.Facilities[1].UpgradeTo(2, 2, 1);

            Assert.That(useCase.Execute().Select(x => x.OfferId), Is.EqualTo(new[] { 1, 2 }));
        }

        static GameWorldState CreateInitializedWorldState(HardcodedMasterRepository repository)
        {
            var candidateService = TestRuntimeServiceFactory.CreateActorProcessingCandidateService();
            var worldState = new GameWorldState(
                new ActorSpatialIndexService(new FixedWorldGameSettingsRepository()),
                new ItemSpatialIndexService(new FixedWorldGameSettingsRepository()),
                candidateService,
                ActorViewDataStoreTestFactory.Create(),
                new FixedWorldGameSettingsRepository());
            var useCase = new InitializeGameWorldOrchestrator(
                worldState,
                new InitializeWorldMapUseCase(new FixedWorldGameSettingsRepository()),
                new InitializeDungeonOrchestrator(new GenerateDungeonFloorUseCase(new FixedWorldGameSettingsRepository(), new HardcodedMasterRepository())),
                repository,
                new NoOpGameEventBus(),
                new FixedWorldGameSettingsRepository());

            useCase.ExecuteAsync(
                    new InitializeGameWorldRequest(InitialWorldSettings.CreateDefault().DungeonSeed))
                .GetAwaiter()
                .GetResult();

            return worldState;
        }

        sealed class NoOpGameEventBus : IGameEventBus
        {
            public void Publish(IGameEvent gameEvent)
            {
            }

            public Observable<T> OnEvent<T>() where T : class, IGameEvent
            {
                return Observable.Empty<T>();
            }
        }
    }
}
