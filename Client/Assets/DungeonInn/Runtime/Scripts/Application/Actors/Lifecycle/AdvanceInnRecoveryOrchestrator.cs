using System;
using Cysharp.Threading.Tasks;
using DungeonInn.Application.World;
using VContainer;

namespace DungeonInn.Application.Actors.Lifecycle
{
    public sealed class AdvanceInnRecoveryOrchestrator
    {
        readonly RecoverAdventurerAtInnUseCase recoverAdventurerAtInnUseCase;

        [Inject]
        public AdvanceInnRecoveryOrchestrator(
            RecoverAdventurerAtInnUseCase recoverAdventurerAtInnUseCase)
        {
            this.recoverAdventurerAtInnUseCase = recoverAdventurerAtInnUseCase
                ?? throw new ArgumentNullException(nameof(recoverAdventurerAtInnUseCase));
        }

        public UniTask ExecuteAsync(IGameWorldState worldState, float deltaGameSeconds)
        {
            return recoverAdventurerAtInnUseCase.ExecuteAsync(worldState, deltaGameSeconds);
        }
    }
}
