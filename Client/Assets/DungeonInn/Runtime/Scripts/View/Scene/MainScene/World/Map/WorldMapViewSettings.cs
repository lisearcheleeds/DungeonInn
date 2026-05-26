using System;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class WorldMapViewSettings
    {
        public int ChunkTileSize { get; }
        public int ChunkBuildsPerFrame { get; }
        public float TileHeightMeters { get; }

        public WorldMapViewSettings(int chunkTileSize, int chunkBuildsPerFrame, float tileHeightMeters)
        {
            ChunkTileSize = Math.Max(1, chunkTileSize);
            ChunkBuildsPerFrame = Math.Max(1, chunkBuildsPerFrame);
            TileHeightMeters = Math.Max(0f, tileHeightMeters);
        }

        public static WorldMapViewSettings CreateDefault()
        {
            return new WorldMapViewSettings(16, 1, 2f);
        }
    }
}
