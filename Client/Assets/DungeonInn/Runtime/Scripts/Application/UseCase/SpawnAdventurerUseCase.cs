using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DungeonInn.Application.Factory;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Commerce;
using DungeonInn.Domain.Guild;
using DungeonInn.Domain.Item;
using DungeonInn.Master;
using VContainer;

namespace DungeonInn.Application.UseCase
{
    public sealed class SpawnAdventurerUseCase
    {
        readonly IAdventurerFactory adventurerFactory;
        readonly IMasterRepository masterRepository;
        readonly ActorSpawnCompletionService spawnCompletionService;
        readonly ExchangeExecutor exchangeExecutor = new();

        [Inject]
        public SpawnAdventurerUseCase(
            IAdventurerFactory adventurerFactory,
            IMasterRepository masterRepository,
            ActorSpawnCompletionService spawnCompletionService)
        {
            this.adventurerFactory = adventurerFactory ?? throw new ArgumentNullException(nameof(adventurerFactory));
            this.masterRepository = masterRepository ?? throw new ArgumentNullException(nameof(masterRepository));
            this.spawnCompletionService = spawnCompletionService ?? throw new ArgumentNullException(nameof(spawnCompletionService));
        }

        public SpawnAdventurerUseCase(
            IAdventurerFactory adventurerFactory,
            IMasterRepository masterRepository,
            DungeonInn.Application.Profiles.IActorProfileRegistry profileRegistry,
            DungeonInn.Application.Event.IEventPublisher eventBus)
            : this(
                adventurerFactory,
                masterRepository,
                new ActorSpawnCompletionService(profileRegistry, eventBus))
        {
        }

        public UniTask<Actor> ExecuteAsync(AdventurerGuild guild, AdventurerCreateRequest request, int occurredAtTick)
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

            var actor = adventurerFactory.Create(request);

            if (actor.Level == 1)
            {
                var rookieEquipment = ToItemStacks(archetypeMaster.InitialEquipmentItemIds);
                ProvideRookieEquipment(guild, actor, rookieEquipment, occurredAtTick);
                actor.RequireBehavior<AdventurerBehavior>().ChangeLifecycleState(AdventurerLifecycleState.Arrived);
            }
            else if (actor.Level < 5)
            {
                throw new InvalidOperationException("Only level 1 or level 5 and higher adventurers can spawn.");
            }
            else
            {
                actor.RequireBehavior<AdventurerBehavior>().ChangeLifecycleState(AdventurerLifecycleState.Arrived);
            }

            EquipInitialEquipment(actor, archetypeMaster.InitialEquipmentItemIds);
            spawnCompletionService.Complete(actor, archetypeMaster, request.DisplayName);
            return UniTask.FromResult(actor);
        }

        void EquipInitialEquipment(Actor actor, IEnumerable<int> itemIds)
        {
            foreach (var itemId in itemIds)
            {
                var equipmentMaster = masterRepository.GetEquipmentMaster(itemId);
                var stack = new ItemStack(itemId, 1);
                if (actor.Inventory.Has(stack))
                {
                    actor.Inventory.Remove(stack);
                }

                if (equipmentMaster.Slot == EquipmentSlot.Weapon)
                {
                    actor.Equip(equipmentMaster, masterRepository.GetWeaponMaster(itemId));
                    continue;
                }

                actor.Equip(equipmentMaster);
            }
        }

        void ProvideRookieEquipment(
            AdventurerGuild guild,
            Actor actor,
            IReadOnlyList<ItemStack> rookieEquipment,
            int occurredAtTick)
        {
            if (rookieEquipment.Count == 0)
            {
                return;
            }

            if (!guild.Inventory.HasAll(rookieEquipment))
            {
                throw new InvalidOperationException("Guild does not have rookie equipment.");
            }

            var transaction = exchangeExecutor.Execute(
                guild,
                actor,
                rookieEquipment,
                Array.Empty<ItemStack>(),
                occurredAtTick);
            guild.RecordTransaction(transaction);
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
