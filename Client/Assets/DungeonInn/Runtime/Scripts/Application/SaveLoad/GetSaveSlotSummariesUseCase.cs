using System;
using System.Collections.Generic;
using VContainer;

namespace DungeonInn.Application.SaveLoad
{
    public sealed class GetSaveSlotSummariesUseCase
    {
        readonly IGameSaveRepository gameSaveRepository;

        [Inject]
        public GetSaveSlotSummariesUseCase(IGameSaveRepository gameSaveRepository)
        {
            this.gameSaveRepository = gameSaveRepository
                ?? throw new ArgumentNullException(nameof(gameSaveRepository));
        }

        public IReadOnlyList<GameSaveSlotSummary> Execute()
        {
            return gameSaveRepository.GetSlotSummaries();
        }
    }
}
