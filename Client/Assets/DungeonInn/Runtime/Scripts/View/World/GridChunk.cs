using UnityEngine;

namespace DungeonInn.Runtime.Scripts.View.World
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class GridChunk : MonoBehaviour
    {
        public void Build(int startX, int startZ, int size, float yOffset, Color color)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = color;
            GetComponent<MeshRenderer>().sharedMaterial = mat;

            int cellCount = size * size;
            var vertices = new Vector3[cellCount * 4];
            var triangles = new int[cellCount * 6];
            var uvs = new Vector2[cellCount * 4];

            int vi = 0, ti = 0;
            for (int x = 0; x < size; x++)
            {
                for (int z = 0; z < size; z++)
                {
                    float wx = startX + x;
                    float wz = startZ + z;

                    vertices[vi]     = new Vector3(wx,     yOffset, wz);
                    vertices[vi + 1] = new Vector3(wx,     yOffset, wz + 1);
                    vertices[vi + 2] = new Vector3(wx + 1, yOffset, wz + 1);
                    vertices[vi + 3] = new Vector3(wx + 1, yOffset, wz);

                    uvs[vi]     = new Vector2(0, 0);
                    uvs[vi + 1] = new Vector2(0, 1);
                    uvs[vi + 2] = new Vector2(1, 1);
                    uvs[vi + 3] = new Vector2(1, 0);

                    triangles[ti]     = vi;
                    triangles[ti + 1] = vi + 1;
                    triangles[ti + 2] = vi + 2;
                    triangles[ti + 3] = vi;
                    triangles[ti + 4] = vi + 2;
                    triangles[ti + 5] = vi + 3;

                    vi += 4;
                    ti += 6;
                }
            }

            var mesh = new Mesh { name = "ChunkMesh" };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvs;
            mesh.RecalculateNormals();

            GetComponent<MeshFilter>().sharedMesh = mesh;
        }
    }
}
