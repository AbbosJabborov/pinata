using System.Collections.Generic;
using UnityEngine;

namespace Pinata.Gameplay
{
    public static class ProceduralMeshBuilder
    {
        /// <summary>
        /// Generates a subdivided box mesh with width, height, and depth, subdivided into resolution x resolution quads per face.
        /// All 6 faces have outward-pointing normals and clockwise winding.
        /// </summary>
        public static Mesh CreateSubdividedBox(float width, float height, float depth, int resolution = 8)
        {
            Mesh mesh = new Mesh();
            mesh.name = "SubdividedBox";

            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> triangles = new List<int>();

            Vector3 halfSize = new Vector3(width * 0.5f, height * 0.5f, depth * 0.5f);

            // 6 faces of the cube (normal, right, up) where Cross(right, up) == normal
            // +Z (Front)
            BuildFace(vertices, normals, uvs, triangles, Vector3.forward, Vector3.right, Vector3.up, halfSize, resolution);
            // -Z (Back)
            BuildFace(vertices, normals, uvs, triangles, Vector3.back, Vector3.left, Vector3.up, halfSize, resolution);
            // +X (Right)
            BuildFace(vertices, normals, uvs, triangles, Vector3.right, Vector3.back, Vector3.up, halfSize, resolution);
            // -X (Left)
            BuildFace(vertices, normals, uvs, triangles, Vector3.left, Vector3.forward, Vector3.up, halfSize, resolution);
            // +Y (Top)
            BuildFace(vertices, normals, uvs, triangles, Vector3.up, Vector3.right, Vector3.back, halfSize, resolution);
            // -Y (Bottom)
            BuildFace(vertices, normals, uvs, triangles, Vector3.down, Vector3.right, Vector3.forward, halfSize, resolution);

            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);

            mesh.RecalculateBounds();
            mesh.RecalculateTangents();

            return mesh;
        }

        private static void BuildFace(
            List<Vector3> vertices,
            List<Vector3> normals,
            List<Vector2> uvs,
            List<int> triangles,
            Vector3 normal,
            Vector3 right,
            Vector3 up,
            Vector3 halfSize,
            int resolution)
        {
            int startIndex = vertices.Count;

            for (int y = 0; y <= resolution; y++)
            {
                float v = (float)y / resolution;
                float yOffset = (v - 0.5f) * 2f;

                for (int x = 0; x <= resolution; x++)
                {
                    float u = (float)x / resolution;
                    float xOffset = (u - 0.5f) * 2f;

                    Vector3 pos = normal + up * yOffset + right * xOffset;
                    // Scale position along dimensions
                    pos = new Vector3(pos.x * halfSize.x, pos.y * halfSize.y, pos.z * halfSize.z);

                    vertices.Add(pos);
                    normals.Add(normal);
                    uvs.Add(new Vector2(u, v));
                }
            }

            int rowSize = resolution + 1;
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    int i0 = startIndex + y * rowSize + x;
                    int i1 = i0 + 1;
                    int i2 = i0 + rowSize;
                    int i3 = i2 + 1;

                    // Clockwise triangles for outward normal
                    triangles.Add(i0);
                    triangles.Add(i1);
                    triangles.Add(i2);

                    triangles.Add(i1);
                    triangles.Add(i3);
                    triangles.Add(i2);
                }
            }
        }
    }
}
