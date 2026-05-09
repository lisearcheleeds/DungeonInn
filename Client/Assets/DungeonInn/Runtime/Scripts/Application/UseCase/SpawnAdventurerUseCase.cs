using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Application.Factory;
using DungeonInn.Application.Profiles;
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
        readonly IActorProfileRegistry profileRegistry;
        readonly IGameEventBus eventBus;

        [Inject]
        public SpawnAdventurerUseCase(
            IAdventurerFactory adventurerFactory,
            IMasterRepository masterRepository,
            IActorProfileRegistry profileRegistry,
            IGameEventBus eventBus)
        {
            this.adventurerFactory = adventurerFactory ?? throw new ArgumentNullException(nameof(adventurerFactory));
            this.masterRepository = masterRepository ?? throw new ArgumentNullException(nameof(masterRepository));
            this.profileRegistry = profileRegistry ?? throw new ArgumentNullException(nameof(profileRegistry));
            this.eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
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
            profileRegistry.Register(actor.Id, archetypeMaster.Name);
            eventBus.Publish(new ActorSpawned(actor.Id));
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

        static void ProvideRookieEquipment(
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

            guild.Inventory.RemoveRange(rookieEquipment);
            actor.Inventory.AddRange(rookieEquipment);
            guild.RecordTransaction(
                new ExchangeTransaction(
                    Guid.NewGuid(),
                    guild.Id,
                    actor.Id,
                    rookieEquipment,
                    Array.Empty<ItemStack>(),
                    occurredAtTick));
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
