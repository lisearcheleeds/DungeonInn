using Cysharp.Threading.Tasks;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Guild;

namespace DungeonInn.Application.UseCase
{
    /// <summary>
    /// 冒険者の現在状態と宿屋居住権から、次のライフサイクル状態を決定するユースケース。
    /// </summary>
    public sealed class AdvanceAdventurerLifecycleUseCase
    {
        /// <summary>
        /// 死亡、居住中、来訪中の条件に応じて冒険者の状態を進める。
        /// </summary>
        public UniTask ExecuteAsync(AdventurerGuild guild, Actor adventurer)
        {
            var behavior = adventurer.RequireBehavior<AdventurerBehavior>();
            if (behavior.LifecycleState == AdventurerLifecycleState.Dead)
            {
                guild.ReleaseInnReservation(adventurer.Id, 0);
                return UniTask.CompletedTask;
            }

            if (guild.HasActiveInnReservation(adventurer.Id))
            {
                AdvanceResident(adventurer, behavior);
                return UniTask.CompletedTask;
            }

            AdvanceVisitor(adventurer, behavior);
            return UniTask.CompletedTask;
        }

        static void AdvanceResident(Actor adventurer, AdventurerBehavior behavior)
        {
            if (adventurer.Hp < adventurer.Params.MaxHp)
            {
                behavior.ChangeLifecycleState(AdventurerLifecycleState.Recovering);
                return;
            }

            behavior.ChangeLifecycleState(AdventurerLifecycleState.Preparing);
        }

        static void AdvanceVisitor(Actor adventurer, AdventurerBehavior behavior)
        {
            if (5 <= adventurer.Level)
            {
                behavior.ChangeLifecycleState(AdventurerLifecycleState.ReadyToLeave);
                return;
            }

            behavior.ChangeLifecycleState(AdventurerLifecycleState.Preparing);
        }
    }
}
