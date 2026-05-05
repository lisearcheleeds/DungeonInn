using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DungeonInn.Application.Factory;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Guild;
using DungeonInn.Domain.Item;
using DungeonInn.Master;

namespace DungeonInn.Application.UseCase
{
    /// <summary>
    /// 冒険者系マスタから Actor を生成し、来訪時の初期処理を実行するユースケース。
    /// </summary>
    public sealed class SpawnAdventurerFromMasterUseCase
    {
        readonly IActorFactory actorFactory;
        readonly IMasterRepository masterRepository;
        readonly SpawnAdventurerUseCase spawnAdventurerUseCase = new();

        public SpawnAdventurerFromMasterUseCase(
            IActorFactory actorFactory,
            IMasterRepository masterRepository)
        {
            this.actorFactory = actorFactory ?? throw new ArgumentNullException(nameof(actorFactory));
            this.masterRepository = masterRepository ?? throw new ArgumentNullException(nameof(masterRepository));
        }

        /// <summary>
        /// マスタから冒険者を生成し、Lv1 の場合はギルド在庫から初期装備を支給して装備状態へ反映する。
        /// </summary>
        public async UniTask<Actor> ExecuteAsync(
            AdventurerGuild guild,
            ActorCreateRequest request,
            int occurredAtTick)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var archetypeMaster = masterRepository.GetActorArchetypeMaster(request.ArchetypeId);
            if (archetypeMaster.BehaviorType != ActorBehaviorType.Adventurer)
            {
                throw new InvalidOperationException("Spawn adventurer requires adventurer actor archetype.");
            }

            var isRookie = archetypeMaster.InitialLevel == 1;
            var actor = actorFactory.CreateActor(
                new ActorCreateRequest(
                    request.ArchetypeId,
                    request.ActorId,
                    request.Position,
                    request.Faction,
                    request.PreferenceSeed,
                    !isRookie));
            var rookieEquipment = isRookie ? ToItemStacks(archetypeMaster.InitialEquipmentItemIds) : Array.Empty<ItemStack>();
            await spawnAdventurerUseCase.ExecuteAsync(guild, actor, rookieEquipment, occurredAtTick);
            if (isRookie)
            {
                AddInitialInventory(actor, archetypeMaster.InitialInventoryItemIds);
                EquipInitialEquipment(actor, archetypeMaster.InitialEquipmentItemIds);
            }

            return actor;
        }

        void AddInitialInventory(Actor actor, IEnumerable<int> itemIds)
        {
            foreach (var itemId in itemIds)
            {
                masterRepository.GetItemMaster(itemId);
                actor.Inventory.Add(new ItemStack(itemId, 1));
            }
        }

        void EquipInitialEquipment(Actor actor, IEnumerable<int> itemIds)
        {
            foreach (var itemId in itemIds)
            {
                var equipmentMaster = masterRepository.GetEquipmentMaster(itemId);
                if (equipmentMaster.Slot == EquipmentSlot.Weapon)
                {
                    actor.Equip(equipmentMaster, masterRepository.GetWeaponMaster(itemId));
                    continue;
                }

                actor.Equip(equipmentMaster);
            }
        }

        static IReadOnlyList<ItemStack> ToItemStacks(IEnumerable<int> itemIds)
        {
            return itemIds
                .GroupBy(itemId => itemId)
                .Select(group => new ItemStack(group.Key, group.Count()))
                .ToArray();
        }
    }
}
