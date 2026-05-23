using System;

namespace DungeonInn.View.Scene.MainScene.World
{
    public readonly struct ActorVisualRequestKey : IEquatable<ActorVisualRequestKey>
    {
        public ActorVisualRequestKey(string visualId, int skinId)
        {
            if (string.IsNullOrWhiteSpace(visualId))
            {
                throw new ArgumentException("Actor visual id is required.", nameof(visualId));
            }

            if (skinId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(skinId));
            }

            VisualId = visualId;
            SkinId = skinId;
        }

        public string VisualId { get; }
        public int SkinId { get; }

        public bool Equals(ActorVisualRequestKey other)
        {
            return VisualId == other.VisualId &&
                SkinId == other.SkinId;
        }

        public override bool Equals(object obj)
        {
            return obj is ActorVisualRequestKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return ((VisualId != null ? VisualId.GetHashCode() : 0) * 397) ^ SkinId;
        }
    }
}
