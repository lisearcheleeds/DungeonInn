using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DungeonInn.Domain.Character;
using DungeonInn.Domain.Commerce;
using DungeonInn.Domain.Guild;
using DungeonInn.Domain.Item;

namespace DungeonInn.Application.UseCase
{
    /// <summary>
    /// 来訪した冒険者のスポーン時初期処理を行うユースケース。
    /// </summary>
    public sealed class SpawnAdventurerUseCase
    {
        /// <summary>
        /// スポーン可能レベルを検証し、Lv1 冒険者には新米装備を支給する。
        /// </summary>
        public UniTask ExecuteAsync(
            AdventurerGuild guild,
            Character adventurer,
            IReadOnlyList<ItemStack> rookieEquipment,
            int occurredAtTick)
        {
            if (adventurer.Level == 1)
            {
                ProvideRookieEquipment(guild, adventurer, rookieEquipment, occurredAtTick);
                adventurer.ChangeLifecycleState(AdventurerLifecycleState.Arrived);
                return UniTask.CompletedTask;
            }

            if (adventurer.Level < 5)
            {
                throw new InvalidOperationException("Only level 1 or level 5 and higher adventurers can spawn.");
            }

            adventurer.ChangeLifecycleState(AdventurerLifecycleState.Arrived);
            return UniTask.CompletedTask;
        }

        static void ProvideRookieEquipment(
            AdventurerGuild guild,
            Character adventurer,
            IReadOnlyList<ItemStack> rookieEquipment,
            int occurredAtTick)
        {
            if (rookieEquipment == null)
            {
                throw new ArgumentNullException(nameof(rookieEquipment));
            }

            if (rookieEquipment.Count == 0)
            {
                return;
            }

            if (!guild.Inventory.HasAll(rookieEquipment))
            {
                throw new InvalidOperationException("Guild does not have rookie equipment.");
            }

            guild.Inventory.RemoveRange(rookieEquipment);
            adventurer.Inventory.AddRange(rookieEquipment);
            guild.RecordTransaction(
                new ExchangeTransaction(
                    Guid.NewGuid(),
                    guild.Id,
                    adventurer.Id,
                    rookieEquipment,
                    Array.Empty<ItemStack>(),
                    occurredAtTick));
        }
    }
}
