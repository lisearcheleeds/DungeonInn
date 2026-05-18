using System.Collections.Generic;
using DungeonInn.Domain.Map;

namespace DungeonInn.Application.Actors.Movement
{
    public interface INavigationPathProvider
    {
        // 戻り値は provider 内部の一時バッファであり、呼び出し側は同一フレームで消費すること。
        IReadOnlyList<GridPosition> TryFindPath(MapLayerId layerId, GridPosition start, GridPosition goal);
    }
}
