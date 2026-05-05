using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Item;

namespace DungeonInn.Application.UseCase
{
    /// <summary>
    /// スカウト候補 Actor の現在パラメータからスカウト費用を計算するユースケース。
    /// </summary>
    public sealed class CalculateScoutCostUseCase
    {
        readonly ScoutCostPolicy scoutCostPolicy = new();

        /// <summary>
        /// Actor のレベルや能力値を使い、表示・実行時点のスカウト費用を返す。
        /// </summary>
        public UniTask<IReadOnlyList<ItemStack>> ExecuteAsync(Actor candidate)
        {
            return UniTask.FromResult(scoutCostPolicy.Calculate(candidate));
        }
    }
}
