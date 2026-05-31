using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Facility;
using DungeonInn.Domain.Guild;
using DungeonInn.Master;
using VContainer;

namespace DungeonInn.Application.Actors.Ai
{
    public sealed class SelectAdventureGoalUseCase
    {
        const int LevelUpBaseWeight = 30;
        const int EarnMoneyBaseWeight = 20;
        const int ReachFloorBaseWeight = 20;
        const int CollectItemBaseWeight = 20;
        const int DefeatMonsterBaseWeight = 10;
        const int GuildGoldShortageThreshold = 500;
        const int GuildGoldShortageBonusWeight = 30;
        const int NearNextLevelBonusWeight = 15;
        const int DefaultEarnMoneyTarget = 100;
        const int DefaultDefeatMonsterCount = 1;

        readonly IMasterRepository masterRepository;
        readonly Random random;

        [Inject]
        public SelectAdventureGoalUseCase(IMasterRepository masterRepository)
        {
            this.masterRepository = masterRepository ?? throw new ArgumentNullException(nameof(masterRepository));
            random = new Random();
        }

        public SelectAdventureGoalUseCase(IMasterRepository masterRepository, int seed)
        {
            this.masterRepository = masterRepository ?? throw new ArgumentNullException(nameof(masterRepository));
            random = new Random(seed);
        }

        public UniTask<ActorGoal> ExecuteAsync(
            AdventurerGuild guild,
            Actor adventurer,
            int targetFloorId)
        {
            if (guild == null)
            {
                throw new ArgumentNullException(nameof(guild));
            }

            if (adventurer == null)
            {
                throw new ArgumentNullException(nameof(adventurer));
            }

            var candidates = new List<GoalCandidate>
            {
                new(
                    new ActorGoal(ActorGoalType.LevelUp, 0, 1, 0),
                    LevelUpBaseWeight + CalculateLevelUpBonusWeight(adventurer)),
                new(
                    new ActorGoal(ActorGoalType.EarnMoney, 0, DefaultEarnMoneyTarget, 0),
                    EarnMoneyBaseWeight + CalculateGuildGoldShortageBonusWeight(guild))
            };

            AddCollectItemGoals(guild, candidates);
            AddDefeatMonsterGoal(targetFloorId, candidates);

            if (1 < targetFloorId)
            {
                candidates.Add(new GoalCandidate(
                    new ActorGoal(ActorGoalType.ReachFloor, targetFloorId, 1, 0),
                    ReachFloorBaseWeight));
            }

            return UniTask.FromResult(SelectWeighted(candidates));
        }

        static int CalculateLevelUpBonusWeight(Actor adventurer)
        {
            return 0 < adventurer.Experience ? NearNextLevelBonusWeight : 0;
        }

        static int CalculateGuildGoldShortageBonusWeight(AdventurerGuild guild)
        {
            var availableGold = 0;
            foreach (var facility in guild.Facilities)
            {
                if (facility.Type == FacilityType.GeneralStore || facility.Type == FacilityType.EquipmentShop)
                {
                    availableGold += facility.Inventory.Gold;
                }
            }

            return availableGold < GuildGoldShortageThreshold ? GuildGoldShortageBonusWeight : 0;
        }

        static void AddCollectItemGoals(AdventurerGuild guild, List<GoalCandidate> candidates)
        {
            foreach (var exchangeOffer in guild.ExchangeOffers)
            {
                if (!exchangeOffer.IsActive)
                {
                    continue;
                }

                var facility = guild.GetFacility(exchangeOffer.FacilityId);
                if (!CanUseExchangeOfferForExplorationGoal(facility.Type))
                {
                    continue;
                }

                foreach (var requestedItem in exchangeOffer.RequestedItems)
                {
                    candidates.Add(new GoalCandidate(
                        new ActorGoal(
                            ActorGoalType.CollectItem,
                            requestedItem.ItemId,
                            requestedItem.Count,
                            0),
                        CollectItemBaseWeight));
                }
            }
        }

        void AddDefeatMonsterGoal(int targetFloorId, List<GoalCandidate> candidates)
        {
            if (!TryFindMonsterSpeciesForFloor(targetFloorId, out var speciesId))
            {
                return;
            }

            candidates.Add(new GoalCandidate(
                new ActorGoal(ActorGoalType.DefeatMonster, speciesId, DefaultDefeatMonsterCount, 0),
                DefeatMonsterBaseWeight));
        }

        bool TryFindMonsterSpeciesForFloor(int floorId, out int speciesId)
        {
            var targetDepthBand = default(DungeonDepthBandMaster);
            foreach (var depthBand in masterRepository.DungeonDepthBandMasters.Values)
            {
                if (floorId < depthBand.MinFloorIndex || depthBand.MaxFloorIndex < floorId)
                {
                    continue;
                }

                targetDepthBand = depthBand;
                break;
            }

            if (targetDepthBand == null)
            {
                speciesId = 0;
                return false;
            }

            var spawnTable = masterRepository.GetSpawnTableMaster(targetDepthBand.MonsterSpawnTableId);
            if (spawnTable.TargetType != SpawnTableTargetType.ActorArchetype || spawnTable.Entries.Count == 0)
            {
                speciesId = 0;
                return false;
            }

            var entry = spawnTable.Entries[random.Next(spawnTable.Entries.Count)];
            var archetype = masterRepository.GetActorArchetypeMaster(entry.TargetMasterId);
            speciesId = archetype.SpeciesId;
            return 0 < speciesId;
        }

        static bool CanUseExchangeOfferForExplorationGoal(FacilityType facilityType)
        {
            return facilityType == FacilityType.GeneralStore || facilityType == FacilityType.EquipmentShop;
        }

        ActorGoal SelectWeighted(IReadOnlyList<GoalCandidate> candidates)
        {
            var totalWeight = 0;
            foreach (var candidate in candidates)
            {
                totalWeight += Math.Max(0, candidate.Weight);
            }

            if (totalWeight <= 0)
            {
                return candidates[0].Goal;
            }

            var selectedWeight = random.Next(totalWeight);
            var accumulatedWeight = 0;
            foreach (var candidate in candidates)
            {
                accumulatedWeight += Math.Max(0, candidate.Weight);
                if (selectedWeight < accumulatedWeight)
                {
                    return candidate.Goal;
                }
            }

            return candidates[^1].Goal;
        }

        readonly struct GoalCandidate
        {
            public ActorGoal Goal { get; }
            public int Weight { get; }

            public GoalCandidate(ActorGoal goal, int weight)
            {
                Goal = goal ?? throw new ArgumentNullException(nameof(goal));
                Weight = Math.Max(0, weight);
            }
        }
    }
}
