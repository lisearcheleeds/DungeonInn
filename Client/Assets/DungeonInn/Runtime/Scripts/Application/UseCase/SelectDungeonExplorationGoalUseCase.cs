using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DungeonInn.Domain.Character;
using DungeonInn.Domain.Commerce;
using DungeonInn.Domain.Dungeon;
using DungeonInn.Domain.Facility;
using DungeonInn.Domain.Guild;

namespace DungeonInn.Application.UseCase
{
    /// <summary>
    /// 冒険者のレベルと施設の交換項目から、次のダンジョン探索目的を選択するユースケース。
    /// </summary>
    public sealed class SelectDungeonExplorationGoalUseCase
    {
        readonly Random random;

        public SelectDungeonExplorationGoalUseCase()
        {
            random = new Random();
        }

        public SelectDungeonExplorationGoalUseCase(int seed)
        {
            random = new Random(seed);
        }

        /// <summary>
        /// レベル上げ、交換品収集、フロア到達の候補から探索目的を抽選する。
        /// </summary>
        public UniTask<DungeonExplorationGoal> ExecuteAsync(
            AdventurerGuild guild,
            Character adventurer,
            int targetFloorId)
        {
            var candidates = new List<DungeonExplorationGoal>
            {
                DungeonExplorationGoal.CreateLeveling()
            };

            AddCollectItemGoals(guild, candidates);

            if (10 <= adventurer.Level)
            {
                candidates.Add(DungeonExplorationGoal.CreateReachFloor(targetFloorId));
            }

            var selected = candidates[random.Next(candidates.Count)];
            return UniTask.FromResult(selected);
        }

        static void AddCollectItemGoals(AdventurerGuild guild, List<DungeonExplorationGoal> candidates)
        {
            foreach (var exchangeOffer in guild.ExchangeOffers.Where(x => x.IsActive))
            {
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
