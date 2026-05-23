using System;

namespace DungeonInn.Master
{
    public sealed class ActorVisualMaster
    {
        public ActorVisualMaster(string visualId, int skinId, string visualDefinitionAddress)
        {
            if (string.IsNullOrWhiteSpace(visualId))
            {
                throw new ArgumentException("Actor visual id is required.", nameof(visualId));
            }

            if (skinId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(skinId));
            }

            if (string.IsNullOrWhiteSpace(visualDefinitionAddress))
            {
                throw new ArgumentException("Actor visual definition address is required.", nameof(visualDefinitionAddress));
            }

            VisualId = visualId;
            SkinId = skinId;
            VisualDefinitionAddress = visualDefinitionAddress;
        }

        public string VisualId { get; }
        public int SkinId { get; }
        public string VisualDefinitionAddress { get; }
    }
}
