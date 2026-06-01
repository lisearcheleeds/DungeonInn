using System;
using DungeonInn.Application.Actors.Ai;
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
    /// 蜀帝匱閠・・ｽ・ｽ縺滂ｿｽE繧ｹ繧ｫ繧ｦ繝亥呵｣懊ｒ繧ｮ繝ｫ繝峨せ繧ｿ繝・・ｽ・ｽ縺ｨ縺励※髮・・ｽ・ｽ縺吶ｋ繝ｦ繝ｼ繧ｹ繧ｱ繝ｼ繧ｹ縲・    /// </summary>
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
        /// 繧ｹ繧ｫ繧ｦ繝郁ｲｻ逕ｨ繧呈髪謇輔＞縲∝呵｣懆・・ｽE蠖ｹ蜑ｲ螟画峩縺ｨ蜿門ｼ募ｱ･豁ｴ縺ｮ險倬鹸繧定｡後≧縲・        /// </summary>
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
