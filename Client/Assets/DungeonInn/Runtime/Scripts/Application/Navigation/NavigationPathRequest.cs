using System;
using DungeonInn.Domain.Map;

namespace DungeonInn.Application.Navigation
{
    public sealed class NavigationPathRequest
    {
        public LayerPosition From { get; }
        public LayerPosition To { get; }
        public float AgentRadius { get; }

        public NavigationPathRequest(LayerPosition from, LayerPosition to, float agentRadius)
        {
            if (!from.LayerId.Equals(to.LayerId))
            {
                throw new ArgumentException("Navigation path cannot cross layers.", nameof(to));
            }

            From = from;
            To = to;
            AgentRadius = Math.Max(0f, agentRadius);
        }
    }
}
