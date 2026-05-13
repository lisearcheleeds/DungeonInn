using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Commerce;
using DungeonInn.Domain.Dungeon;
using DungeonInn.Domain.Facility;
using DungeonInn.Domain.Guild;
using VContainer;

namespace DungeonInn.Application.Actors.Ai
{
    /// <summary>
    /// 冒険老E�Eレベルと施設の交換頁E��から、次のダンジョン探索目皁E��選択するユースケース、E    /// </summary>
    public sealed class SelectDungeonExplorationGoalUseCase
    {
        readonly Random random;

        [Inject]
        public SelectDungeonExplorationGoalUseCase()
        {
            random = new Random();
        }

        public SelectDungeonExplorationGoalUseCase(int seed)
        {
            random = new Random(seed);
        }

        /// <summary>
        /// レベル上げ、交換品収集、フロア到達�E候補から探索目皁E��抽選する、E        /// </summary>
        public UniTask<DungeonExplorationGoal> ExecuteAsync(
            AdventurerGuild guild,
            Actor adventurer,
            int targetFloorId)
        {
            var candidates = new List<DungeonExplorationGoal>
            {
                DungeonExplorationGoal.CreateLeveling()
            };

            AddCollectItemGoals(guild, candidates);

            if (1 < targetFloorId)
            {
                candidates.Add(DungeonExplorationGoal.CreateReachFloor(targetFloorId));
            }

            var selected = candidates[random.Next(candidates.Count)];
            return UniTask.FromResult(selected);
        }

        static void AddCollectItemGoals(AdventurerGuild guild, List<DungeonExplorationGoal> candidates)
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
                    candidates.Add(
                        DungeonExplorationGoal.CreateCollectItem(
                            requestedItem.ItemId,
                            requestedItem.Count));
                }
            }
        }

        static bool CanUseExchangeOfferForExplorationGoal(FacilityType facilityType)
        {
            return facilityType == FacilityType.GeneralStore || facilityType == FacilityType.EquipmentShop;
        }
    }
}
