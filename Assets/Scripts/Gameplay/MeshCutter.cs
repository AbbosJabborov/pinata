using System.Collections.Generic;
using UnityEngine;

namespace Pinata.Gameplay
{
    public static class MeshCutter
    {
        public struct CutResult
        {
            public bool success;
            public GameObject pieceA;
            public GameObject pieceB;
        }

        public static CutResult Cut(GameObject target, Vector3 planePoint, Vector3 planeNormal, Material capMaterial = null)
        {
            CutResult result = new CutResult { success = false };

            if (target == null) return result;
            MeshFilter filter = target.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null) return result;

            Mesh origMesh = filter.sharedMesh;
            Transform tr = target.transform;

            Vector3 localPlaneNormal = tr.InverseTransformDirection(planeNormal).normalized;
            Vector3 localPlanePoint = tr.InverseTransformPoint(planePoint);
            Plane plane = new Plane(localPlaneNormal, localPlanePoint);

            Vector3[] origVerts = origMesh.vertices;
            Vector3[] origNorms = origMesh.normals;
            Vector2[] origUVs = origMesh.uv;
            int[] origTris = origMesh.triangles;

            // Check if plane intersects the mesh bounds
            Bounds bounds = origMesh.bounds;
            if (!IntersectsBounds(plane, bounds)) return result;

            // Containers for Side A (positive) and Side B (negative)
            MeshData meshA = new MeshData();
            MeshData meshB = new MeshData();

            List<Vector3> cutPointsA = new List<Vector3>();
            List<Vector3> cutPointsB = new List<Vector3>();

            for (int i = 0; i < origTris.Length; i += 3)
            {
                int i0 = origTris[i];
                int i1 = origTris[i + 1];
                int i2 = origTris[i + 2];

                Vector3 v0 = origVerts[i0];
                Vector3 v1 = origVerts[i1];
                Vector3 v2 = origVerts[i2];

                bool side0 = plane.GetSide(v0);
                bool side1 = plane.GetSide(v1);
                bool side2 = plane.GetSide(v2);

                if (side0 && side1 && side2)
                {
                    // Entire triangle on Side A
                    meshA.AddTriangle(v0, origNorms[i0], origUVs[i0], v1, origNorms[i1], origUVs[i1], v2, origNorms[i2], origUVs[i2]);
                }
                else if (!side0 && !side1 && !side2)
                {
                    // Entire triangle on Side B
                    meshB.AddTriangle(v0, origNorms[i0], origUVs[i0], v1, origNorms[i1], origUVs[i1], v2, origNorms[i2], origUVs[i2]);
                }
                else
                {
                    // Intersected triangle: Split into 1 triangle and 1 quad (2 triangles)
                    SplitTriangle(plane,
                        v0, origNorms[i0], origUVs[i0], side0,
                        v1, origNorms[i1], origUVs[i1], side1,
                        v2, origNorms[i2], origUVs[i2], side2,
                        meshA, meshB, cutPointsA, cutPointsB);
                }
            }

            if (meshA.vertices.Count < 3 || meshB.vertices.Count < 3)
            {
                return result;
            }

            // Cap the sliced interior faces
            CapCutFaces(plane, cutPointsA, meshA, true);
            CapCutFaces(plane, cutPointsB, meshB, false);

            Renderer origRenderer = target.GetComponent<Renderer>();
            Material mat = (origRenderer != null) ? origRenderer.sharedMaterial : null;

            // Spawn Piece A
            GameObject pieceA = new GameObject(target.name + "_SliceA");
            pieceA.transform.position = tr.position;
            pieceA.transform.rotation = tr.rotation;
            pieceA.transform.localScale = tr.localScale;

            var mfA = pieceA.AddComponent<MeshFilter>();
            mfA.sharedMesh = meshA.BuildMesh("SliceA");
            var mrA = pieceA.AddComponent<MeshRenderer>();
            mrA.sharedMaterial = mat;

            var rbA = pieceA.AddComponent<Rigidbody>();
            rbA.mass = 1.2f;
            var colA = pieceA.AddComponent<BoxCollider>();

            // Spawn Piece B
            GameObject pieceB = new GameObject(target.name + "_SliceB");
            pieceB.transform.position = tr.position;
            pieceB.transform.rotation = tr.rotation;
            pieceB.transform.localScale = tr.localScale;

            var mfB = pieceB.AddComponent<MeshFilter>();
            mfB.sharedMesh = meshB.BuildMesh("SliceB");
            var mrB = pieceB.AddComponent<MeshRenderer>();
            mrB.sharedMaterial = mat;

            var rbB = pieceB.AddComponent<Rigidbody>();
            rbB.mass = 1.2f;
            var colB = pieceB.AddComponent<BoxCollider>();

            // Apply separation impulse along plane normal
            rbA.AddForce(planeNormal * 3.5f + Vector3.up * 1.5f, ForceMode.Impulse);
            rbA.AddTorque(Random.insideUnitSphere * 8f, ForceMode.Impulse);

            rbB.AddForce(-planeNormal * 3.5f + Vector3.up * 1.5f, ForceMode.Impulse);
            rbB.AddTorque(Random.insideUnitSphere * 8f, ForceMode.Impulse);

            // Add cleanup coroutine to both pieces
            var cleanupA = pieceA.AddComponent<SliceCleanup>();
            var cleanupB = pieceB.AddComponent<SliceCleanup>();

            result.success = true;
            result.pieceA = pieceA;
            result.pieceB = pieceB;

            return result;
        }

        private static bool IntersectsBounds(Plane plane, Bounds bounds)
        {
            float r = bounds.extents.x * Mathf.Abs(plane.normal.x) +
                      bounds.extents.y * Mathf.Abs(plane.normal.y) +
                      bounds.extents.z * Mathf.Abs(plane.normal.z);
            float dist = plane.GetDistanceToPoint(bounds.center);
            return Mathf.Abs(dist) <= r;
        }

        private static void SplitTriangle(
            Plane plane,
            Vector3 v0, Vector3 n0, Vector2 u0, bool s0,
            Vector3 v1, Vector3 n1, Vector2 u1, bool s1,
            Vector3 v2, Vector3 n2, Vector2 u2, bool s2,
            MeshData meshA, MeshData meshB,
            List<Vector3> cutA, List<Vector3> cutB)
        {
            // Re-order so v0 is the lone vertex on its side
            if (s0 == s1)
            {
                // v2 is alone
                Swap(ref v0, ref v2); Swap(ref n0, ref n2); Swap(ref u0, ref u2); Swap(ref s0, ref s2);
                Swap(ref v1, ref v2); Swap(ref n1, ref n2); Swap(ref u1, ref u2); Swap(ref s1, ref s2);
            }
            else if (s0 == s2)
            {
                // v1 is alone
                Swap(ref v0, ref v1); Swap(ref n0, ref n1); Swap(ref u0, ref u1); Swap(ref s0, ref s1);
                Swap(ref v1, ref v2); Swap(ref n1, ref n2); Swap(ref u1, ref u2); Swap(ref s1, ref s2);
            }

            // v0 is alone on side s0. v1 and v2 are on opposite side !s0.
            float t01 = GetIntersection(plane, v0, v1);
            float t02 = GetIntersection(plane, v0, v2);

            Vector3 cut01 = Vector3.Lerp(v0, v1, t01);
            Vector3 n01 = Vector3.Lerp(n0, n1, t01);
            Vector2 u01 = Vector2.Lerp(u0, u1, t01);

            Vector3 cut02 = Vector3.Lerp(v0, v2, t02);
            Vector3 n02 = Vector3.Lerp(n0, n2, t02);
            Vector2 u02 = Vector2.Lerp(u0, u2, t02);

            MeshData loneMesh = s0 ? meshA : meshB;
            MeshData pairMesh = s0 ? meshB : meshA;

            // Lone vertex gets 1 triangle: (v0, cut01, cut02)
            loneMesh.AddTriangle(v0, n0, u0, cut01, n01, u01, cut02, n02, u02);

            // Pair side gets 2 triangles (quad): (cut01, v1, v2) and (cut01, v2, cut02)
            pairMesh.AddTriangle(cut01, n01, u01, v1, n1, u1, v2, n2, u2);
            pairMesh.AddTriangle(cut01, n01, u01, v2, n2, u2, cut02, n02, u02);

            if (s0)
            {
                cutA.Add(cut01); cutA.Add(cut02);
                cutB.Add(cut02); cutB.Add(cut01);
            }
            else
            {
                cutB.Add(cut01); cutB.Add(cut02);
                cutA.Add(cut02); cutA.Add(cut01);
            }
        }

        private static float GetIntersection(Plane plane, Vector3 a, Vector3 b)
        {
            Vector3 dir = b - a;
            float dot = Vector3.Dot(plane.normal, dir);
            if (Mathf.Abs(dot) < 1e-6f) return 0.5f;
            return Mathf.Clamp01(-plane.GetDistanceToPoint(a) / dot);
        }

        private static void CapCutFaces(Plane plane, List<Vector3> points, MeshData mesh, bool isSideA)
        {
            if (points.Count < 3) return;

            Vector3 center = Vector3.zero;
            for (int i = 0; i < points.Count; i++) center += points[i];
            center /= points.Count;

            Vector3 capNormal = isSideA ? -plane.normal : plane.normal;
            Vector2 centerUV = new Vector2(0.5f, 0.5f);

            for (int i = 0; i < points.Count; i += 2)
            {
                Vector3 p0 = points[i];
                Vector3 p1 = (i + 1 < points.Count) ? points[i + 1] : points[0];

                if (isSideA)
                {
                    mesh.AddTriangle(center, capNormal, centerUV, p1, capNormal, centerUV, p0, capNormal, centerUV);
                }
                else
                {
                    mesh.AddTriangle(center, capNormal, centerUV, p0, capNormal, centerUV, p1, capNormal, centerUV);
                }
            }
        }

        private static void Swap<T>(ref T a, ref T b)
        {
            T temp = a;
            a = b;
            b = temp;
        }

        private class MeshData
        {
            public List<Vector3> vertices = new List<Vector3>();
            public List<Vector3> normals = new List<Vector3>();
            public List<Vector2> uvs = new List<Vector2>();
            public List<int> triangles = new List<int>();

            public void AddTriangle(Vector3 v0, Vector3 n0, Vector2 u0, Vector3 v1, Vector3 n1, Vector2 u1, Vector3 v2, Vector3 n2, Vector2 u2)
            {
                int start = vertices.Count;
                vertices.Add(v0); normals.Add(n0); uvs.Add(u0);
                vertices.Add(v1); normals.Add(n1); uvs.Add(u1);
                vertices.Add(v2); normals.Add(n2); uvs.Add(u2);

                triangles.Add(start);
                triangles.Add(start + 1);
                triangles.Add(start + 2);
            }

            public Mesh BuildMesh(string name)
            {
                Mesh m = new Mesh();
                m.name = name;
                m.SetVertices(vertices);
                m.SetNormals(normals);
                m.SetUVs(0, uvs);
                m.SetTriangles(triangles, 0);
                m.RecalculateBounds();
                m.RecalculateTangents();
                return m;
            }
        }
    }

    public class SliceCleanup : MonoBehaviour
    {
        private float _delay = 3.5f;

        private void Start()
        {
            StartCoroutine(ShrinkRoutine());
        }

        private System.Collections.IEnumerator ShrinkRoutine()
        {
            yield return new WaitForSeconds(_delay);

            Vector3 startScale = transform.localScale;
            float elapsed = 0f;
            float duration = 0.4f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                transform.localScale = Vector3.Lerp(startScale, Vector3.zero, elapsed / duration);
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
