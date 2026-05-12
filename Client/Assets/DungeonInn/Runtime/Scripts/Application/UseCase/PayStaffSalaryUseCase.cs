using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Commerce;
using DungeonInn.Domain.Guild;
using DungeonInn.Domain.Item;

namespace DungeonInn.Application.UseCase
{
    /// <summary>
    /// ギルドスタッフへの給与支払いを処理するユースケース。
    /// </summary>
    public sealed class PayStaffSalaryUseCase
    {
        readonly ExchangeExecutor exchangeExecutor = new();

        /// <summary>
        /// 雇用中スタッフの給与をギルド在庫から支払い、取引履歴を記録する。
        /// </summary>
        public UniTask<IReadOnlyList<ItemStack>> ExecuteAsync(AdventurerGuild guild, IEnumerable<Actor> staffMembers, int occurredAtTick)
        {
            var paidItems = new List<ItemStack>();
            var salaries = staffMembers
                .Where(staff => staff.Behavior is GuildStaffBehavior)
                .Select(staff => staff.RequireBehavior<GuildStaffBehavior>().Salary)
                .ToArray();
            var requiredItems = salaries.SelectMany(salary => salary).ToArray();

            if (!guild.Inventory.HasAll(requiredItems))
            {
                throw new InvalidOperationException("Guild does not have enough salary items.");
            }

            foreach (var staff in staffMembers)
            {
                if (staff.Behavior is not GuildStaffBehavior)
                {
                    continue;
                }

                var salary = staff.RequireBehavior<GuildStaffBehavior>().Salary;
                var transaction = exchangeExecutor.Execute(
                    guild,
                    staff,
                    salary,
                    Array.Empty<ItemStack>(),
                    occurredAtTick);
                paidItems.AddRange(salary);
                guild.RecordTransaction(transaction);
            }

            return UniTask.FromResult<IReadOnlyList<ItemStack>>(paidItems);
        }
    }
}
