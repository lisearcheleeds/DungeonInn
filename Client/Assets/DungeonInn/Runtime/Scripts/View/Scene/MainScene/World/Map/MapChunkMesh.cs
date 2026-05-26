using UnityEngine;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class MapChunkMesh
    {
        public MapChunkMesh(Mesh mesh, Material[] materials)
        {
            Mesh = mesh;
            Materials = materials;
        }

        public Mesh Mesh { get; }
        public Material[] Materials { get; }
    }
}
