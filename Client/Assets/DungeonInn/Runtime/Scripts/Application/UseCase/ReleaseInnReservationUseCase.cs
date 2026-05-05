using Cysharp.Threading.Tasks;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Guild;

namespace DungeonInn.Application.UseCase
{
    /// <summary>
    /// 冒険者が保持している宿屋居住権を解除するユースケース。
    /// </summary>
    public sealed class ReleaseInnReservationUseCase
    {
        /// <summary>
        /// 有効な宿屋予約を解除し、死亡していない冒険者を来訪状態へ戻す。
        /// </summary>
        public UniTask ExecuteAsync(AdventurerGuild guild, Actor adventurer, int occurredAtTick)
        {
            guild.ReleaseInnReservation(adventurer.Id, occurredAtTick);
            var behavior = adventurer.RequireBehavior<AdventurerBehavior>();

            if (behavior.LifecycleState != AdventurerLifecycleState.Dead)
            {
                behavior.ChangeLifecycleState(AdventurerLifecycleState.Arrived);
            }

            return UniTask.CompletedTask;
        }
    }
}
