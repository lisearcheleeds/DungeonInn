using System.Collections.Generic;
using DungeonInn.Domain.Actor;

namespace DungeonInn.Application.GameLoop
{
    public sealed class GameWorldFrameBuffer
    {
        readonly List<Actor> actors = new();

        public IReadOnlyList<Actor> CopyActors(IReadOnlyList<Actor> source)
        {
            actors.Clear();
            for (var i = 0; i < source.Count; i++)
            {
                actors.Add(source[i]);
            }

            return actors;
        }
    }
}
