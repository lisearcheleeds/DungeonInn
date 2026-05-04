using Cysharp.Threading.Tasks;
using DungeonInn.Domain.Character;
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
        public UniTask ExecuteAsync(AdventurerGuild guild, Character adventurer)
        {
            if (adventurer.LifecycleState == AdventurerLifecycleState.Dead)
            {
                guild.ReleaseInnReservation(adventurer.Id, 0);
                return UniTask.CompletedTask;
            }

            if (guild.HasActiveInnReservation(adventurer.Id))
            {
                AdvanceResident(adventurer);
                return UniTask.CompletedTask;
            }

            AdvanceVisitor(adventurer);
            return UniTask.CompletedTask;
        }

        static void AdvanceResident(Character adventurer)
        {
            if (adventurer.Hp < adventurer.CalculateMaxHp())
            {
                adventurer.MarkRecovering();
                return;
            }

            adventurer.MarkPreparing();
        }

        static void AdvanceVisitor(Character adventurer)
        {
            if (5 <= adventurer.Level)
            {
                adventurer.MarkReadyToLeave();
                return;
            }

            adventurer.MarkPreparing();
        }
    }
}
