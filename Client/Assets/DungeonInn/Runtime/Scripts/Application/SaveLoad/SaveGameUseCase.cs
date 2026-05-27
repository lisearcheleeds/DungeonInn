using System;
using VContainer;

namespace DungeonInn.Application.SaveLoad
{
    public sealed class SaveGameUseCase
    {
        readonly IGameSaveRepository gameSaveRepository;
        readonly CreateGameSaveSnapshotUseCase createSnapshotUseCase;
        readonly ActiveSaveSlotService activeSaveSlotService;

        [Inject]
        public SaveGameUseCase(
            IGameSaveRepository gameSaveRepository,
            CreateGameSaveSnapshotUseCase createSnapshotUseCase,
            ActiveSaveSlotService activeSaveSlotService)
        {
            this.gameSaveRepository = gameSaveRepository
                ?? throw new ArgumentNullException(nameof(gameSaveRepository));
            this.createSnapshotUseCase = createSnapshotUseCase
                ?? throw new ArgumentNullException(nameof(createSnapshotUseCase));
            this.activeSaveSlotService = activeSaveSlotService
                ?? throw new ArgumentNullException(nameof(activeSaveSlotService));
        }

        public void Execute(int slotId)
        {
            var saveData = createSnapshotUseCase.Execute(slotId);
            gameSaveRepository.Save(slotId, saveData);
            activeSaveSlotService.SetActiveSlot(slotId);
        }
    }
}
