using System;
using System.Collections.Generic;
using DungeonInn.Application.Actors.Movement;
using DungeonInn.Domain.Map;

namespace DungeonInn.View.Scene.Bridge
{
    public interface INavigationPathProviderRegistry
    {
        void Register(INavigationPathProvider provider);
        void Unregister(INavigationPathProvider provider);
    }

    public sealed class NavigationPathProviderProxy :
        INavigationPathProvider,
        INavigationPathProviderRegistry
    {
        INavigationPathProvider current;

        public IReadOnlyList<GridPosition> TryFindPath(
            MapLayerId layerId,
            GridPosition start,
            GridPosition goal)
        {
            if (current == null)
            {
                return null;
            }

            return current.TryFindPath(layerId, start, goal);
        }

        public void Register(INavigationPathProvider provider)
        {
            if (current != null)
            {
                throw new InvalidOperationException("Duplicate navigation path provider registration.");
            }

            current = provider;
        }

        public void Unregister(INavigationPathProvider provider)
        {
            if (ReferenceEquals(current, provider))
            {
                current = null;
            }
        }
    }
}
