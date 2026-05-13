using System;
using DungeonInn.Application.Actors.Ai;
using DungeonInn.Application.Actors.Equipment;
using DungeonInn.Application.Actors.Lifecycle;
using DungeonInn.Application.Actors.Movement;
using DungeonInn.Application.Actors.Profiles;
using DungeonInn.Application.Actors.Spawn;
using DungeonInn.Application.Combat;
using DungeonInn.Application.Dungeons;
using DungeonInn.Application.Economy;
using DungeonInn.Application.Facilities;
using DungeonInn.Application.Items;
using DungeonInn.Application.World;
using DungeonInn.Application.GameLoop;

using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Commerce;
using DungeonInn.Domain.Guild;
using DungeonInn.Domain.Item;
using VContainer;

namespace DungeonInn.Application.Facilities
{
    /// <summary>
    /// 冒険老E��た�Eスカウト候補をギルドスタチE��として雁E��するユースケース、E    /// </summary>
    public sealed class RecruitStaffOrchestrator
    {
        readonly CalculateScoutCostUseCase calculateScoutCostUseCase;
        readonly ExchangeExecutor exchangeExecutor = new();

        [Inject]
        public RecruitStaffOrchestrator(CalculateScoutCostUseCase calculateScoutCostUseCase)
        {
            this.calculateScoutCostUseCase = calculateScoutCostUseCase ?? throw new ArgumentNullException(nameof(calculateScoutCostUseCase));
        }

        /// <summary>
        /// スカウト費用を支払い、候補老E�E役割変更と取引履歴の記録を行う、E        /// </summary>
        public async UniTask ExecuteAsync(AdventurerGuild guild, Actor candidate, IReadOnlyList<ItemStack> staffSalary, int occurredAtTick)
        {
            if (candidate.Behavior is not AdventurerBehavior)
            {
                throw new InvalidOperationException("Candidate cannot be scouted.");
            }

            var scoutCost = await calculateScoutCostUseCase.ExecuteAsync(candidate);
            if (!guild.Inventory.HasAll(scoutCost))
            {
                throw new InvalidOperationException("Guild does not have scout cost items.");
            }

            var transaction = exchangeExecutor.Execute(
                guild,
                candidate,
                scoutCost,
                Array.Empty<ItemStack>(),
                occurredAtTick);
            candidate.ChangeBehavior(new GuildStaffBehavior(staffSalary));
            guild.RecordTransaction(transaction);

            return;
        }
    }
}
