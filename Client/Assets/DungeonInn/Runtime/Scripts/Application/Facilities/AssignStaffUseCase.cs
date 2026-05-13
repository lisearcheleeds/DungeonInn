using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Facility;
using DungeonInn.Domain.Guild;

namespace DungeonInn.Application.Facilities
{
    /// <summary>
    /// ギルドスタチE��を施設に配置し、施設ポイントを再計算するユースケース、E    /// </summary>
    public sealed class AssignStaffUseCase
    {
        /// <summary>
        /// 持E��したスタチE��を施設へ割り当て、�E施設のスタチE��ポイントを更新する、E        /// </summary>
        public UniTask ExecuteAsync(
            AdventurerGuild guild,
            Actor staff,
            Guid facilityId,
            IReadOnlyDictionary<Guid, Actor> staffById)
        {
            guild.AssignStaff(staff, facilityId);
            guild.RecalculateFacilityPoints(staffById);
            return UniTask.CompletedTask;
        }
    }
}
