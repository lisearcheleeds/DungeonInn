using System;
using System.Collections.Generic;
using VContainer;

namespace DungeonInn.Application.Dungeons
{
    public sealed class DungeonInfoScreenService : IDungeonInfoScreenService
    {
        readonly GetDungeonLayerInfoUseCase getDungeonLayerInfoUseCase;

        [Inject]
        public DungeonInfoScreenService(GetDungeonLayerInfoUseCase getDungeonLayerInfoUseCase)
        {
            this.getDungeonLayerInfoUseCase =
                getDungeonLayerInfoUseCase ?? throw new ArgumentNullException(nameof(getDungeonLayerInfoUseCase));
        }

        public IReadOnlyList<DungeonLayerInfoSummary> GetLayers()
        {
            return getDungeonLayerInfoUseCase.Execute();
        }
    }
}
