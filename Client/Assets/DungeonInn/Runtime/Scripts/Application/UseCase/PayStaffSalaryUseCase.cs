using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DungeonInn.Domain.Character;
using DungeonInn.Domain.Commerce;
using DungeonInn.Domain.Guild;
using DungeonInn.Domain.Item;

namespace DungeonInn.Application.UseCase
{
    public sealed class PayStaffSalaryUseCase
    {
        public UniTask<IReadOnlyList<ItemStack>> ExecuteAsync(AdventurerGuild guild, IEnumerable<Character> staffMembers, int occurredAtTick)
        {
            var paidItems = new List<ItemStack>();
            var salaries = staffMembers
                .Where(staff => staff.IsGuildStaff)
                .Select(staff => staff.Salary)
                .ToArray();
            var requiredItems = salaries.SelectMany(salary => salary).ToArray();

            if (!guild.Inventory.HasAll(requiredItems))
            {
                throw new InvalidOperationException("Guild does not have enough salary items.");
            }

            foreach (var staff in staffMembers)
            {
                if (!staff.IsGuildStaff)
                {
                    continue;
                }

                var salary = staff.Salary;
                guild.Inventory.RemoveRange(salary);
                staff.Inventory.AddRange(salary);
                paidItems.AddRange(salary);
                guild.RecordTransaction(
                    new ExchangeTransaction(
                        Guid.NewGuid(),
                        guild.Id,
                        staff.Id,
                        salary,
                        Array.Empty<ItemStack>(),
                        occurredAtTick));
            }

            return UniTask.FromResult<IReadOnlyList<ItemStack>>(paidItems);
        }
    }
}
