// ============================================================================
//  SmoothNormalBaker.cs
//  ----------------------------------------------------------------------------
//  Bakes averaged object-space normals into UV4 (TEXCOORD3).
//
//  DToon consumes this channel in the outline pass when _USE_SMOOTH_NORMAL
//  is enabled. xyz stores the baked object-space normal; w=1 marks the
//  channel as valid so shaders can fall back to authored normals otherwise.
// ============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace DToon.Editor.Tools
{
    public static class SmoothNormalBaker
    {
        private const int SmoothNormalUvChannel = 3; // Unity UV4 / shader TEXCOORD3
        private const string OutputFolder = "Assets/DToonBakedMeshes";

        private enum BakeTarget
        {
            Uv4,
            MeshNormals
        }

        [MenuItem("Tools/DToon/Smooth Normals/Bake Selected Renderers To UV4", priority = 100)]
        public static void BakeSelectedRenderersToUv4()
        {
            BakeSelectedRenderers(BakeTarget.Uv4);
        }

        [MenuItem("Tools/DToon/Smooth Normals/Bake Selected Renderers To Mesh Normals", priority = 101)]
        public static void BakeSelectedRenderersToMeshNormals()
        {
            BakeSelectedRenderers(BakeTarget.MeshNormals);
        }

        public static string WriteCubeBakeReportForHarness()
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string outputDir = Path.Combine(projectRoot, "HarnessOutput");
            Directory.CreateDirectory(outputDir);

            string reportPath = Path.Combine(outputDir, "SmoothNormal_CubeReport.txt");
            Mesh cube = CreateHardEdgedCubeForHarness();

            try
            {
                Vector3[] vertices = cube.vertices;
                Vector3[] originalNormals = cube.normals;
                BakeSmoothNormalsToUv4(cube);

                List<Vector4> smoothUv4 = new List<Vector4>(vertices.Length);
                cube.GetUVs(SmoothNormalUvChannel, smoothUv4);
                AssertTrue(smoothUv4.Count == vertices.Length, "UV4 count must match vertex count.");

                Dictionary<VertexPositionKey, List<int>> groups = BuildPositionGroups(vertices);
                AssertTrue(groups.Count == 8, "Hard-edged cube must have 8 shared corner positions.");

                List<List<int>> sortedGroups = new List<List<int>>(groups.Values);
                sortedGroups.Sort((a, b) => CompareVector3(vertices[a[0]], vertices[b[0]]));

                StringBuilder report = new StringBuilder();
                report.AppendLine("DToon SmoothNormalBaker cube report");
                report.AppendLine("Channel: UV4 / TEXCOORD3");
                report.AppendLine("Contract: xyz = object-space smooth normal, w = 1 valid marker");
                report.AppendLine("VertexCount: " + vertices.Length);
                report.AppendLine("CornerGroups: " + groups.Count);
                report.AppendLine();

                for (int groupIndex = 0; groupIndex < sortedGroups.Count; groupIndex++)
                {
                    List<int> indices = sortedGroups[groupIndex];
                    AssertTrue(indices.Count == 3, "Each hard-edged cube corner must have 3 split vertices.");

                    Vector3 expected = Vector3.zero;
                    for (int i = 0; i < indices.Count; i++)
                    {
                        expected += originalNormals[indices[i]];
                    }
                    expected.Normalize();

                    Vector4 firstSmooth = smoothUv4[indices[0]];
                    Vector3 firstSmoothNormal = new Vector3(firstSmooth.x, firstSmooth.y, firstSmooth.z);
                    AssertApproximately(firstSmoothNormal, expected, 0.0001f, "Smoothed corner normal must match normalized face-normal sum.");

                    report.AppendLine("Corner " + groupIndex + " position " + FormatVector3(vertices[indices[0]]));
                    report.AppendLine("  expected " + FormatVector3(expected));

                    for (int i = 0; i < indices.Count; i++)
                    {
                        int vertexIndex = indices[i];
                        Vector4 smooth = smoothUv4[vertexIndex];
                        Vector3 smoothNormal = new Vector3(smooth.x, smooth.y, smooth.z);

                        AssertApproximately(smoothNormal, firstSmoothNormal, 0.0001f, "Split vertices at one corner must share identical smooth normals.");
                        AssertApproximately(smoothNormal, expected, 0.0001f, "Every split vertex must match the expected corner normal.");
                        AssertTrue(Mathf.Abs(smooth.w - 1.0f) <= 0.0001f, "Smooth normal UV4 w marker must be 1.");

                        report.AppendLine(
                            "  v" + vertexIndex +
                            " original " + FormatVector3(originalNormals[vertexIndex]) +
                            " smooth " + FormatVector3(smoothNormal) +
                            " w " + smooth.w.ToString("0.000")
                        );
                    }

                    report.AppendLine();
                }

                report.AppendLine("Assertions: PASS");
                File.WriteAllText(reportPath, report.ToString());
                return reportPath;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(cube);
            }
        }

        private static void BakeSelectedRenderers(BakeTarget bakeTarget)
        {
            GameObject[] selectedObjects = Selection.gameObjects;
            if (selectedObjects == null || selectedObjects.Length == 0)
            {
                EditorUtility.DisplayDialog(
                    "DToon Smooth Normals",
                    "Select one or more GameObjects with MeshRenderer or SkinnedMeshRenderer components.",
                    "OK"
                );
                return;
            }

            EnsureOutputFolder();

            int bakedCount = 0;
            string targetLabel = bakeTarget == BakeTarget.Uv4 ? "UV4" : "mesh normals";
            HashSet<int> processedComponents = new HashSet<int>();
            List<string> failures = new List<string>();

            try
            {
                for (int i = 0; i < selectedObjects.Length; i++)
                {
                    GameObject selectedObject = selectedObjects[i];
                    EditorUtility.DisplayProgressBar(
                        "DToon Smooth Normals",
                        selectedObject.name,
                        (float)i / selectedObjects.Length
                    );

                    MeshFilter[] meshFilters = selectedObject.GetComponentsInChildren<MeshFilter>(true);
                    for (int j = 0; j < meshFilters.Length; j++)
                    {
                        MeshFilter meshFilter = meshFilters[j];
                        if (meshFilter == null || !processedComponents.Add(meshFilter.GetInstanceID()))
                        {
                            continue;
                        }

                        if (BakeMeshFilter(meshFilter, bakeTarget, failures))
                        {
                            bakedCount++;
                        }
                    }

                    SkinnedMeshRenderer[] skinnedRenderers = selectedObject.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                    for (int j = 0; j < skinnedRenderers.Length; j++)
                    {
                        SkinnedMeshRenderer skinnedRenderer = skinnedRenderers[j];
                        if (skinnedRenderer == null || !processedComponents.Add(skinnedRenderer.GetInstanceID()))
                        {
                            continue;
                        }

                        if (BakeSkinnedMeshRenderer(skinnedRenderer, bakeTarget, failures))
                        {
                            bakedCount++;
                        }
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string message = bakedCount + " renderer(s) baked to " + targetLabel + ".";
            if (failures.Count > 0)
            {
                message += "\n\nSkipped:\n" + string.Join("\n", failures);
            }

            EditorUtility.DisplayDialog("DToon Smooth Normals", message, "OK");
        }

        [MenuItem("Tools/DToon/Smooth Normals/Bake Selected Renderers To UV4", true)]
        private static bool ValidateBakeSelectedRenderersToUv4()
        {
            return Selection.gameObjects != null && Selection.gameObjects.Length > 0;
        }

        [MenuItem("Tools/DToon/Smooth Normals/Bake Selected Renderers To Mesh Normals", true)]
        private static bool ValidateBakeSelectedRenderersToMeshNormals()
        {
            return Selection.gameObjects != null && Selection.gameObjects.Length > 0;
        }

        private static bool BakeMeshFilter(MeshFilter meshFilter, BakeTarget bakeTarget, List<string> failures)
        {
            Mesh sourceMesh = meshFilter.sharedMesh;
            if (sourceMesh == null)
            {
                return false;
            }

            Mesh bakedMesh;
            if (!TryCreateBakedMeshCopy(sourceMesh, meshFilter.gameObject.name, bakeTarget, out bakedMesh, failures))
            {
                return false;
            }

            string assetPath = CreateUniqueMeshAssetPath(sourceMesh, meshFilter.gameObject.name, bakeTarget);
            AssetDatabase.CreateAsset(bakedMesh, assetPath);

            Undo.RecordObject(meshFilter, "Assign DToon Smooth Normal Mesh");
            meshFilter.sharedMesh = bakedMesh;
            EditorUtility.SetDirty(meshFilter);
            ConfigureSmoothNormalMaterials(meshFilter.GetComponent<Renderer>(), bakeTarget);
            return true;
        }

        private static bool BakeSkinnedMeshRenderer(SkinnedMeshRenderer skinnedRenderer, BakeTarget bakeTarget, List<string> failures)
        {
            Mesh sourceMesh = skinnedRenderer.sharedMesh;
            if (sourceMesh == null)
            {
                return false;
            }

            Mesh bakedMesh;
            if (!TryCreateBakedMeshCopy(sourceMesh, skinnedRenderer.gameObject.name, bakeTarget, out bakedMesh, failures))
            {
                return false;
            }

            string assetPath = CreateUniqueMeshAssetPath(sourceMesh, skinnedRenderer.gameObject.name, bakeTarget);
            AssetDatabase.CreateAsset(bakedMesh, assetPath);

            Undo.RecordObject(skinnedRenderer, "Assign DToon Smooth Normal Mesh");
            skinnedRenderer.sharedMesh = bakedMesh;
            EditorUtility.SetDirty(skinnedRenderer);
            ConfigureSmoothNormalMaterials(skinnedRenderer, bakeTarget);
            return true;
        }

        private static bool TryCreateBakedMeshCopy(
            Mesh sourceMesh,
            string ownerName,
            BakeTarget bakeTarget,
            out Mesh bakedMesh,
            List<string> failures
        )
        {
            bakedMesh = null;

            if (!sourceMesh.isReadable)
            {
                failures.Add(ownerName + ": " + sourceMesh.name + " is not readable. Enable Read/Write in the model import settings.");
                return false;
            }

            try
            {
                bakedMesh = UnityEngine.Object.Instantiate(sourceMesh);
                bakedMesh.name = MakeSafeFileName(sourceMesh.name) + "_DToonSmoothNormals";
                if (bakeTarget == BakeTarget.Uv4)
                {
                    BakeSmoothNormalsToUv4(bakedMesh);
                }
                else
                {
                    BakeSmoothNormalsToMeshNormals(bakedMesh);
                }
                return true;
            }
            catch (Exception ex)
            {
                if (bakedMesh != null)
                {
                    UnityEngine.Object.DestroyImmediate(bakedMesh);
                }

                failures.Add(ownerName + ": " + sourceMesh.name + " failed (" + ex.Message + ")");
                return false;
            }
        }

        private static void BakeSmoothNormalsToUv4(Mesh mesh)
        {
            Vector3[] smoothNormalArray = ComputeSmoothNormals(mesh);
            List<Vector4> smoothNormals = new List<Vector4>(smoothNormalArray.Length);
            for (int i = 0; i < smoothNormalArray.Length; i++)
            {
                Vector3 smoothNormal = smoothNormalArray[i];
                smoothNormals.Add(new Vector4(smoothNormal.x, smoothNormal.y, smoothNormal.z, 1.0f));
            }

            mesh.SetUVs(SmoothNormalUvChannel, smoothNormals);
            mesh.UploadMeshData(false);
            EditorUtility.SetDirty(mesh);
        }

        private static void BakeSmoothNormalsToMeshNormals(Mesh mesh)
        {
            mesh.normals = ComputeSmoothNormals(mesh);
            mesh.UploadMeshData(false);
            EditorUtility.SetDirty(mesh);
        }

        private static Vector3[] ComputeSmoothNormals(Mesh mesh)
        {
            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;

            if (vertices == null || vertices.Length == 0)
            {
                throw new InvalidOperationException("Mesh has no vertices.");
            }

            if (normals == null || normals.Length != vertices.Length)
            {
                mesh.RecalculateNormals();
                normals = mesh.normals;
            }

            Dictionary<VertexPositionKey, Vector3> normalSums = new Dictionary<VertexPositionKey, Vector3>(vertices.Length);
            for (int i = 0; i < vertices.Length; i++)
            {
                VertexPositionKey key = new VertexPositionKey(vertices[i]);
                Vector3 normal = normals[i];

                Vector3 sum;
                if (normalSums.TryGetValue(key, out sum))
                {
                    normalSums[key] = sum + normal;
                }
                else
                {
                    normalSums.Add(key, normal);
                }
            }

            Vector3[] smoothNormals = new Vector3[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                VertexPositionKey key = new VertexPositionKey(vertices[i]);
                Vector3 smoothNormal = normalSums[key].normalized;

                if (smoothNormal.sqrMagnitude < 0.000001f)
                {
                    smoothNormal = normals[i].normalized;
                }

                smoothNormals[i] = smoothNormal;
            }

            return smoothNormals;
        }

        private static void EnsureOutputFolder()
        {
            if (AssetDatabase.IsValidFolder(OutputFolder))
            {
                return;
            }

            AssetDatabase.CreateFolder("Assets", "DToonBakedMeshes");
        }

        private static string CreateUniqueMeshAssetPath(Mesh sourceMesh, string ownerName, BakeTarget bakeTarget)
        {
            string sourceName = string.IsNullOrEmpty(sourceMesh.name) ? ownerName : sourceMesh.name;
            string suffix = bakeTarget == BakeTarget.Uv4 ? "_DToonSmoothNormalsUV4" : "_DToonSmoothNormalsMeshNormals";
            string safeName = MakeSafeFileName(ownerName + "_" + sourceName + suffix);
            string path = Path.Combine(OutputFolder, safeName + ".asset").Replace("\\", "/");
            return AssetDatabase.GenerateUniqueAssetPath(path);
        }

        private static string MakeSafeFileName(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "Mesh";
            }

            char[] invalidChars = Path.GetInvalidFileNameChars();
            string safeName = value;
            for (int i = 0; i < invalidChars.Length; i++)
            {
                safeName = safeName.Replace(invalidChars[i], '_');
            }

            return safeName;
        }

        private static void ConfigureSmoothNormalMaterials(Renderer renderer, BakeTarget bakeTarget)
        {
            if (renderer == null)
            {
                return;
            }

            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                Material material = materials[i];
                if (material == null || !material.HasProperty("_UseSmoothNormal"))
                {
                    continue;
                }

                Undo.RecordObject(material, "Configure DToon Smooth Normal");
                bool useSmoothNormal = bakeTarget == BakeTarget.Uv4;
                material.SetFloat("_UseSmoothNormal", useSmoothNormal ? 1.0f : 0.0f);
                SetKeyword(material, "_USE_SMOOTH_NORMAL", useSmoothNormal);

                if (material.HasProperty("_SmoothNormalStrength"))
                {
                    material.SetFloat("_SmoothNormalStrength", useSmoothNormal ? 1.0f : 0.0f);
                }

                EditorUtility.SetDirty(material);
            }
        }

        private readonly struct VertexPositionKey : IEquatable<VertexPositionKey>
        {
            private const float Precision = 100000.0f;

            private readonly int x;
            private readonly int y;
            private readonly int z;

            public VertexPositionKey(Vector3 position)
            {
                x = Mathf.RoundToInt(position.x * Precision);
                y = Mathf.RoundToInt(position.y * Precision);
                z = Mathf.RoundToInt(position.z * Precision);
            }

            public bool Equals(VertexPositionKey other)
            {
                return x == other.x && y == other.y && z == other.z;
            }

            public override bool Equals(object obj)
            {
                return obj is VertexPositionKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = 17;
                    hash = hash * 31 + x;
                    hash = hash * 31 + y;
                    hash = hash * 31 + z;
                    return hash;
                }
            }
        }

        private static Dictionary<VertexPositionKey, List<int>> BuildPositionGroups(Vector3[] vertices)
        {
            Dictionary<VertexPositionKey, List<int>> groups = new Dictionary<VertexPositionKey, List<int>>(vertices.Length);
            for (int i = 0; i < vertices.Length; i++)
            {
                VertexPositionKey key = new VertexPositionKey(vertices[i]);
                List<int> indices;
                if (!groups.TryGetValue(key, out indices))
                {
                    indices = new List<int>(3);
                    groups.Add(key, indices);
                }

                indices.Add(i);
            }

            return groups;
        }

        private static Mesh CreateHardEdgedCubeForHarness()
        {
            Vector3[] vertices =
            {
                new Vector3( 0.5f, -0.5f, -0.5f), new Vector3( 0.5f, -0.5f,  0.5f), new Vector3( 0.5f,  0.5f,  0.5f), new Vector3( 0.5f,  0.5f, -0.5f),
                new Vector3(-0.5f, -0.5f,  0.5f), new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(-0.5f,  0.5f, -0.5f), new Vector3(-0.5f,  0.5f,  0.5f),
                new Vector3(-0.5f,  0.5f, -0.5f), new Vector3( 0.5f,  0.5f, -0.5f), new Vector3( 0.5f,  0.5f,  0.5f), new Vector3(-0.5f,  0.5f,  0.5f),
                new Vector3(-0.5f, -0.5f,  0.5f), new Vector3( 0.5f, -0.5f,  0.5f), new Vector3( 0.5f, -0.5f, -0.5f), new Vector3(-0.5f, -0.5f, -0.5f),
                new Vector3( 0.5f, -0.5f,  0.5f), new Vector3(-0.5f, -0.5f,  0.5f), new Vector3(-0.5f,  0.5f,  0.5f), new Vector3( 0.5f,  0.5f,  0.5f),
                new Vector3(-0.5f, -0.5f, -0.5f), new Vector3( 0.5f, -0.5f, -0.5f), new Vector3( 0.5f,  0.5f, -0.5f), new Vector3(-0.5f,  0.5f, -0.5f),
            };

            Vector3[] normals =
            {
                Vector3.right, Vector3.right, Vector3.right, Vector3.right,
                Vector3.left, Vector3.left, Vector3.left, Vector3.left,
                Vector3.up, Vector3.up, Vector3.up, Vector3.up,
                Vector3.down, Vector3.down, Vector3.down, Vector3.down,
                Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward,
                Vector3.back, Vector3.back, Vector3.back, Vector3.back,
            };

            int[] triangles =
            {
                0, 1, 2, 0, 2, 3,
                4, 5, 6, 4, 6, 7,
                8, 9, 10, 8, 10, 11,
                12, 13, 14, 12, 14, 15,
                16, 17, 18, 16, 18, 19,
                20, 21, 22, 20, 22, 23
            };

            Mesh mesh = new Mesh
            {
                name = "DToon_HardEdgedCube_ForSmoothNormalHarness"
            };
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.triangles = triangles;
            return mesh;
        }

        private static int CompareVector3(Vector3 a, Vector3 b)
        {
            int x = a.x.CompareTo(b.x);
            if (x != 0) return x;
            int y = a.y.CompareTo(b.y);
            if (y != 0) return y;
            return a.z.CompareTo(b.z);
        }

        private static void AssertApproximately(Vector3 actual, Vector3 expected, float tolerance, string message)
        {
            if ((actual - expected).magnitude > tolerance)
            {
                throw new InvalidOperationException(message + " actual=" + FormatVector3(actual) + " expected=" + FormatVector3(expected));
            }
        }

        private static void AssertTrue(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void SetKeyword(Material material, string keyword, bool enabled)
        {
            if (enabled)
            {
                material.EnableKeyword(keyword);
            }
            else
            {
                material.DisableKeyword(keyword);
            }
        }

        private static string FormatVector3(Vector3 value)
        {
            return "(" + value.x.ToString("0.000") + ", " + value.y.ToString("0.000") + ", " + value.z.ToString("0.000") + ")";
        }
    }
}
