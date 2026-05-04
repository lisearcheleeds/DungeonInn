using System;
using Cysharp.Threading.Tasks;
using DungeonInn.Domain.Character;
using DungeonInn.Domain.Commerce;
using DungeonInn.Domain.Guild;
using DungeonInn.Domain.Item;

namespace DungeonInn.Application.UseCase
{
    public sealed class RecruitStaffUseCase
    {
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
