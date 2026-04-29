using UnityEngine;

namespace DungeonInn.Runtime.Scripts.View.World
{
    public class WorldRenderer : MonoBehaviour, IWorldRenderer
    {
        const int ChunkSize = 16;
        const int ChunksPerAxis = 4;
        const int FloorCount = 6;
        const float FloorSpacing = 5f;

        GameObject[][] floorRoots;

        void Start()
        {
            BuildWorld();
        }

        void BuildWorld()
        {
            floorRoots = new GameObject[FloorCount][];

            for (int floor = 0; floor < FloorCount; floor++)
            {
                floorRoots[floor] = new GameObject[ChunksPerAxis * ChunksPerAxis];
                float yOffset = -floor * FloorSpacing;
                var color = floor == 0
                    ? new Color(0.35f, 0.6f, 0.3f)
                    : new Color(0.45f - floor * 0.05f, 0.35f, 0.25f);

                for (int cx = 0; cx < ChunksPerAxis; cx++)
                {
                    for (int cz = 0; cz < ChunksPerAxis; cz++)
                    {
                        var chunkGo = new GameObject($"Chunk_F{floor}_{cx}_{cz}");
                        chunkGo.transform.SetParent(transform);

                        var chunk = chunkGo.AddComponent<GridChunk>();
                        chunk.Build(cx * ChunkSize, cz * ChunkSize, ChunkSize, yOffset, color);

                        chunkGo.SetActive(floor == 0);
                        floorRoots[floor][cx * ChunksPerAxis + cz] = chunkGo;
                    }
                }
            }
        }

        public void SetActiveFloor(int floorIndex)
        {
            if (floorIndex < 0 || floorIndex >= FloorCount) return;

            for (int floor = 0; floor < FloorCount; floor++)
            {
                if (floorRoots[floor] == null) continue;
                bool active = floor == floorIndex;
                foreach (var chunk in floorRoots[floor])
                {
                    chunk?.SetActive(active);
                }
            }
        }
    }
}
