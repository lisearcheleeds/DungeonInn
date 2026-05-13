using System.Collections.Generic;
using UnityEngine;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class MapChunkMesh
    {
        public MapChunkMesh(Mesh mesh, IReadOnlyList<Material> materials)
        {
            Mesh = mesh;
            Materials = materials;
        }

        public Mesh Mesh { get; }
        public IReadOnlyList<Material> Materials { get; }
    }
}
