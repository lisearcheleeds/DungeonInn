using DungeonInn.Domain.Map;

namespace DungeonInn.Application.Navigation
{
    public interface INavigationPathProvider
    {
        bool CanProvidePath(MapLayerId layerId);

        NavigationPathResult TryFindPath(NavigationPathRequest request);
    }
}
