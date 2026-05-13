using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Item;
using VContainer;

namespace DungeonInn.Application.Facilities
{
    /// <summary>
    /// スカウト候裁EActor の現在パラメータからスカウト費用を計算するユースケース、E    /// </summary>
    public sealed class CalculateScoutCostUseCase
    {
        readonly ScoutCostPolicy scoutCostPolicy;

        [Inject]
        public CalculateScoutCostUseCase(ScoutCostPolicy scoutCostPolicy)
        {
            this.scoutCostPolicy = scoutCostPolicy ?? throw new System.ArgumentNullException(nameof(scoutCostPolicy));
        }

        /// <summary>
        /// Actor のレベルめE�E力値を使ぁE��表示・実行時点のスカウト費用を返す、E        /// </summary>
        public UniTask<IReadOnlyList<ItemStack>> ExecuteAsync(Actor candidate)
        {
            return UniTask.FromResult(scoutCostPolicy.Calculate(candidate));
        }
    }
}
