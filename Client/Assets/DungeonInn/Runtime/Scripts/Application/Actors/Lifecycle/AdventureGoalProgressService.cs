using System;
using DungeonInn.Application.Combat;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Commerce;
using DungeonInn.Domain.Item;
using DungeonInn.Domain.Map;
using DungeonInn.Master;
using VContainer;

namespace DungeonInn.Application.Actors.Lifecycle
{
    public sealed class AdventureGoalProgressService
    {
        readonly IActorCombatService actorCombatService;
        readonly ActorExplorationAchievementRegistry achievementRegistry;
        readonly IItemMasterRepository masterRepository;
        readonly PricePolicy pricePolicy = new();

        [Inject]
        public AdventureGoalProgressService(
            IActorCombatService actorCombatService,
            ActorExplorationAchievementRegistry achievementRegistry,
            IItemMasterRepository masterRepository)
        {
            this.actorCombatService = actorCombatService ?? throw new ArgumentNullException(nameof(actorCombatService));
            this.achievementRegistry = achievementRegistry ?? throw new ArgumentNullException(nameof(achievementRegistry));
            this.masterRepository = masterRepository ?? throw new ArgumentNullException(nameof(masterRepository));
        }

        public bool UpdateProgress(Actor actor)
        {
            if (actor == null)
            {
                throw new ArgumentNullException(nameof(actor));
            }

            switch (actor.CurrentGoal.Type)
            {
                case ActorGoalType.LevelUp:
                    return UpdateLevelUpProgress(actor);
                case ActorGoalType.CollectItem:
                    return UpdateCollectItemProgress(actor);
                case ActorGoalType.DefeatMonster:
                    return UpdateDefeatMonsterProgress(actor);
                case ActorGoalType.ReachFloor:
                    return UpdateReachFloorProgress(actor);
                case ActorGoalType.EarnMoney:
                    return UpdateEarnMoneyProgress(actor);
                case ActorGoalType.None:
                    return false;
                default:
                    return false;
            }
        }

        bool UpdateLevelUpProgress(Actor actor)
        {
            if (achievementRegistry.TryGetAdventureStartStatus(actor.Id, out var startStatus))
            {
                var progress = actor.Level > startStatus.Level || actor.Experience > startStatus.Experience ? 1 : 0;
                actor.CurrentGoal.SetProgress(progress);
                return actor.CurrentGoal.IsCompleted();
            }

            if (!actorCombatService.HasParticipatedInCombat(actor.Id))
            {
                actor.CurrentGoal.SetProgress(0);
                return false;
            }

            actor.CurrentGoal.SetProgress(Math.Max(1, actor.CurrentGoal.TargetCount));
            return true;
        }

        bool UpdateCollectItemProgress(Actor actor)
        {
            var progress = achievementRegistry.GetItemCountIncrease(
                actor.Id,
                actor.CurrentGoal.TargetId,
                actor.Inventory.ItemCounts);
            actor.CurrentGoal.SetProgress(progress);
            return actor.CurrentGoal.IsCompleted();
        }

        bool UpdateDefeatMonsterProgress(Actor actor)
        {
            var count = achievementRegistry.GetDefeatedMonsterCount(actor.Id, actor.CurrentGoal.TargetId);
            actor.CurrentGoal.SetProgress(count);
            return actor.CurrentGoal.IsCompleted();
        }

        bool UpdateReachFloorProgress(Actor actor)
        {
            if (actor.Position.LayerId.Equals(MapLayerId.Ground))
            {
                actor.CurrentGoal.SetProgress(0);
                return false;
            }

            var progress = actor.CurrentGoal.TargetId <= actor.Position.LayerId.Value ? 1 : 0;
            actor.CurrentGoal.SetProgress(progress);
            return actor.CurrentGoal.IsCompleted();
        }

        bool UpdateEarnMoneyProgress(Actor actor)
        {
            var totalValue = 0;
            foreach (var kvp in actor.Inventory.ItemCounts)
            {
                var itemId = kvp.Key;
                var increasedCount = achievementRegistry.GetItemCountIncrease(actor.Id, itemId, actor.Inventory.ItemCounts);
                if (increasedCount <= 0)
                {
                    continue;
                }

                if (itemId == SpecialItemIds.Money)
                {
                    totalValue += increasedCount;
                    continue;
                }

                if (!masterRepository.ItemMasters.TryGetValue(itemId, out var itemMaster))
                {
                    continue;
                }

                if (!IsEarnMoneyTarget(actor, itemMaster))
                {
                    continue;
                }

                totalValue += pricePolicy.CalculatePurchasePrice(
                    new ItemStack(itemId, increasedCount),
                    masterRepository.ItemMasters).Count;
            }

            actor.CurrentGoal.SetProgress(totalValue);
            return actor.CurrentGoal.IsCompleted();
        }

        static bool IsEarnMoneyTarget(Actor actor, ItemMaster itemMaster)
        {
            if (!itemMaster.CanTrade)
            {
                return false;
            }

            if (itemMaster.HasTag(ItemTag.Recovery) || itemMaster.HasTag(ItemTag.ManaRecovery))
            {
                return false;
            }

            if (IsEquipmentItem(itemMaster) && actor.Equipment.IsEquipped(itemMaster.Id))
            {
                return false;
            }

            return true;
        }

        static bool IsEquipmentItem(ItemMaster itemMaster)
        {
            return itemMaster.HasTag(ItemTag.Weapon) ||
                itemMaster.HasTag(ItemTag.Armor) ||
                itemMaster.HasTag(ItemTag.Accessory);
        }
    }
}
