using System;
using VContainer;

namespace DungeonInn.Application.SaveLoad
{
    public sealed class GetLatestSaveSlotUseCase
    {
        readonly IGameSaveRepository gameSaveRepository;

        [Inject]
        public GetLatestSaveSlotUseCase(IGameSaveRepository gameSaveRepository)
        {
            this.gameSaveRepository = gameSaveRepository
                ?? throw new ArgumentNullException(nameof(gameSaveRepository));
        }

        public bool TryExecute(out GameSaveSlotSummary summary)
        {
            return gameSaveRepository.TryGetLatestSlot(out summary);
        }
    }
}
