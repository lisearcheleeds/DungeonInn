using System;
using Cysharp.Threading.Tasks;
using DungeonInn.Domain.Character;
using DungeonInn.Domain.Commerce;
using DungeonInn.Domain.Guild;
using DungeonInn.Domain.Item;

namespace DungeonInn.Application.UseCase
{
    /// <summary>
    /// 冒険者またはスカウト候補をギルドスタッフとして雇用するユースケース。
    /// </summary>
    public sealed class RecruitStaffUseCase
    {
        /// <summary>
        /// スカウト費用を支払い、候補者の役割変更と取引履歴の記録を行う。
        /// </summary>
        public UniTask ExecuteAsync(AdventurerGuild guild, Character candidate, int occurredAtTick)
        {
            if (!candidate.CanBeScouted)
            {
                throw new InvalidOperationException("Candidate cannot be scouted.");
            }

            var scoutCost = candidate.ScoutCost;
            if (!guild.Inventory.HasAll(scoutCost.Items))
            {
                throw new InvalidOperationException("Guild does not have scout cost items.");
            }

            guild.Inventory.RemoveRange(scoutCost.Items);
            candidate.RecruitAsStaff();
            guild.RecordTransaction(
                new ExchangeTransaction(
                    Guid.NewGuid(),
                    guild.Id,
                    candidate.Id,
                    scoutCost.Items,
                    Array.Empty<ItemStack>(),
                    occurredAtTick));

            return UniTask.CompletedTask;
        }
    }
}
