// ============================================================================
//  MatcapGenerator.cs
//  ----------------------------------------------------------------------------
//  Generates DToon's default matcap textures and importer settings.
// ============================================================================

using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DToon.Editor.Tools
{
    public static class MatcapGenerator
    {
        public const string GenerateDefaultMatcapsMenuPath = "Tools/DToon/Generate Default Matcap Textures";
        public const string GeneratedMatcapOutputFolder = "Assets/DToon/Samples/MatcapTextures";

        private const int TextureSize = 256;

        [MenuItem(GenerateDefaultMatcapsMenuPath, priority = 92)]
        public static void GenerateDefaultMatcapTextures()
        {
            string outputFolder = EnsureOutputFolder();

            GenerateMatcap("Matcap_Eye_Glossy", outputFolder, EvaluateEyeGlossy);
            GenerateMatcap("Matcap_Metal_Chrome", outputFolder, EvaluateMetalChrome);
            GenerateMatcap("Matcap_Skin_Soft", outputFolder, EvaluateSkinSoft);
            GenerateMatcap("Matcap_Cloth_Velvet", outputFolder, EvaluateClothVelvet);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            UnityEngine.Object outputFolderAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(outputFolder);
            if (outputFolderAsset != null)
            {
                EditorGUIUtility.PingObject(outputFolderAsset);
            }

            Debug.Log("[DToon] Generated 4 matcap textures at " + outputFolder + ".");
        }

        private static void GenerateMatcap(string filename, string outputFolder, Func<float, float, float, Color> evaluator)
        {
            Texture2D texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false, false)
            {
                name = filename,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            Color[] pixels = new Color[TextureSize * TextureSize];
            for (int y = 0; y < TextureSize; y++)
            {
                float v = (float)y / (TextureSize - 1);
                float dy = v * 2.0f - 1.0f;

                for (int x = 0; x < TextureSize; x++)
                {
                    float u = (float)x / (TextureSize - 1);
                    float dx = u * 2.0f - 1.0f;
                    float r2 = dx * dx + dy * dy;

                    if (r2 > 1.0f)
                    {
                        pixels[y * TextureSize + x] = new Color(0.0f, 0.0f, 0.0f, 0.0f);
                        continue;
                    }

                    float dz = Mathf.Sqrt(1.0f - r2);
                    pixels[y * TextureSize + x] = evaluator(dx, dy, dz);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, false);

            string assetPath = outputFolder + "/" + filename + ".png";
            string absolutePath = AssetPathToAbsolutePath(assetPath);
            string absoluteFolder = Path.GetDirectoryName(absolutePath);
            if (!string.IsNullOrEmpty(absoluteFolder))
            {
                Directory.CreateDirectory(absoluteFolder);
            }

            File.WriteAllBytes(absolutePath, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);

            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException("Failed to import matcap texture: " + assetPath);
            }

            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.isReadable = false;
            importer.SaveAndReimport();
        }

        private static Color EvaluateEyeGlossy(float dx, float dy, float dz)
        {
            float distToHighlight = Vector2.Distance(new Vector2(dx, dy), new Vector2(0.0f, 0.5f));
            float highlight = Mathf.Clamp01(1.0f - distToHighlight / 0.42f);
            highlight = Mathf.Pow(highlight, 2.5f);

            float distToSecondary = Vector2.Distance(new Vector2(dx, dy), new Vector2(0.2f, 0.6f));
            float secondary = Mathf.Clamp01(1.0f - distToSecondary / 0.14f) * 0.5f;

            float distToBloom = Vector2.Distance(new Vector2(dx, dy), new Vector2(-0.15f, 0.05f));
            float bloom = Mathf.Clamp01(1.0f - distToBloom / 0.65f);
            bloom = Mathf.Pow(bloom, 3.0f) * 0.55f;

            float value = Mathf.Clamp01(highlight + secondary + bloom);
            return new Color(value, value, value, 1.0f);
        }

        private static Color EvaluateMetalChrome(float dx, float dy, float dz)
        {
            float vertical = Mathf.Clamp01(dy * 0.5f + 0.5f);
            vertical = Mathf.Pow(vertical, 1.5f);

            float horizontal = Mathf.Clamp01(dx * 0.5f + 0.5f);
            Color topColor = new Color(0.9f, 0.95f, 1.0f, 1.0f);
            Color bottomColor = new Color(0.1f, 0.12f, 0.18f, 1.0f);
            Color finalColor = Color.Lerp(bottomColor, topColor, vertical);
            finalColor *= Mathf.Lerp(0.95f, 1.05f, horizontal);
            finalColor.a = 1.0f;
            return finalColor;
        }

        private static Color EvaluateSkinSoft(float dx, float dy, float dz)
        {
            Vector2 keyDirection = new Vector2(-0.5f, 0.7f).normalized;
            float warmKey = Mathf.Clamp01(Vector2.Dot(new Vector2(dx, dy), keyDirection));
            warmKey = Mathf.Clamp01(warmKey * 0.5f + 0.4f);

            Color highlightColor = new Color(1.0f, 0.92f, 0.85f, 1.0f);
            Color baseColor = new Color(0.95f, 0.75f, 0.7f, 1.0f);
            Color shadowColor = new Color(0.7f, 0.55f, 0.55f, 1.0f);

            Color finalColor;
            if (warmKey > 0.5f)
            {
                finalColor = Color.Lerp(baseColor, highlightColor, (warmKey - 0.5f) * 2.0f);
            }
            else
            {
                finalColor = Color.Lerp(shadowColor, baseColor, warmKey * 2.0f);
            }

            finalColor.a = 1.0f;
            return finalColor;
        }

        private static Color EvaluateClothVelvet(float dx, float dy, float dz)
        {
            float edgeFactor = 1.0f - dz;
            edgeFactor = Mathf.Pow(edgeFactor, 1.5f);

            Color centerColor = new Color(0.15f, 0.05f, 0.2f, 1.0f);
            Color edgeColor = new Color(0.9f, 0.4f, 0.7f, 1.0f);
            return Color.Lerp(centerColor, edgeColor, edgeFactor);
        }

        private static string EnsureOutputFolder()
        {
            string absoluteFolder = AssetPathToAbsolutePath(GeneratedMatcapOutputFolder);
            Directory.CreateDirectory(absoluteFolder);
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            return GeneratedMatcapOutputFolder;
        }

        private static string AssetPathToAbsolutePath(string assetPath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            return Path.GetFullPath(Path.Combine(projectRoot, assetPath));
        }
    }
}
