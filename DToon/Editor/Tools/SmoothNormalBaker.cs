// ============================================================================
//  SmoothNormalBaker.cs
//  ----------------------------------------------------------------------------
//  Bakes averaged object-space normals into UV4 (TEXCOORD3).
//
//  DToon uses this channel as a clean shadow-normal source so toon shadow
//  borders follow the character volume instead of hard mesh splits, UV seams,
//  or detailed normal-map noise.
// ============================================================================

using System;
using System.Collections.Generic;
using System.IO;
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
                material.SetFloat("_UseSmoothNormal", bakeTarget == BakeTarget.Uv4 ? 1.0f : 0.0f);

                if (material.HasProperty("_SmoothNormalStrength"))
                {
                    material.SetFloat("_SmoothNormalStrength", bakeTarget == BakeTarget.Uv4 ? 1.0f : 0.0f);
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
    }
}
