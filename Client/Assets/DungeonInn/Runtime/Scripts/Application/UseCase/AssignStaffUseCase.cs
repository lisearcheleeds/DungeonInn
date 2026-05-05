using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Facility;
using DungeonInn.Domain.Guild;

namespace DungeonInn.Application.UseCase
{
    /// <summary>
    /// ギルドスタッフを施設に配置し、施設ポイントを再計算するユースケース。
    /// </summary>
    public sealed class AssignStaffUseCase
    {
        /// <summary>
        /// 指定したスタッフを施設へ割り当て、全施設のスタッフポイントを更新する。
        /// </summary>
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
