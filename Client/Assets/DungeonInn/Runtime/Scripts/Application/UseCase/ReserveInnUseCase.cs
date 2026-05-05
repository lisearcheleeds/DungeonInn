using System;
using Cysharp.Threading.Tasks;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Facility;
using DungeonInn.Domain.Guild;

namespace DungeonInn.Application.UseCase
{
    /// <summary>
    /// 冒険者に宿屋の居住権を割り当てるユースケース。
    /// </summary>
    public sealed class ReserveInnUseCase
    {
        /// <summary>
        /// 宿屋の空きと既存予約を検証し、新しい宿屋予約を作成して冒険者を Resident 状態へ遷移させる。
        /// </summary>
        public UniTask<InnReservation> ExecuteAsync(
            AdventurerGuild guild,
            Actor adventurer,
            Guid innFacilityId,
            int occurredAtTick)
        {
            var reservation = guild.ReserveInn(
                Guid.NewGuid(),
                adventurer,
                innFacilityId,
                occurredAtTick);

            adventurer.RequireBehavior<AdventurerBehavior>().ChangeLifecycleState(AdventurerLifecycleState.Resident);
            return UniTask.FromResult(reservation);
        }
    }
}
