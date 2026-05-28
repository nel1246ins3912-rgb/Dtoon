// ============================================================================
//  RampTextureGenerator.cs
//  ----------------------------------------------------------------------------
//  Generates DToon's default 1D ramp textures and applies strict importer
//  settings so cel-shading ramps stay reproducible across projects.
// ============================================================================

using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace DToon.Editor.Tools
{
    public static class RampTextureGenerator
    {
        public const string GenerateDefaultRampsMenuPath = "Tools/DToon/Generate Default Ramp Textures";
        public const string GeneratedRampOutputFolder = "Assets/DToon/Samples/RampTextures";

        private const int RampWidth = 256;
        private const int RampHeight = 4;

        [MenuItem(GenerateDefaultRampsMenuPath, priority = 90)]
        public static void GenerateDefaultRampTextures()
        {
            string outputFolder = EnsureOutputFolder();

            RampDefinition[] ramps =
            {
                new RampDefinition(
                    "Ramp_Generic_Cool",
                    FilterMode.Bilinear,
                    new ColorStop(0.00f, 60, 70, 90),
                    new ColorStop(0.45f, 95, 105, 130),
                    new ColorStop(0.50f, 160, 165, 175),
                    new ColorStop(0.55f, 225, 225, 230),
                    new ColorStop(1.00f, 255, 255, 255)
                ),
                new RampDefinition(
                    "Ramp_Generic_Warm",
                    FilterMode.Bilinear,
                    new ColorStop(0.00f, 75, 60, 55),
                    new ColorStop(0.35f, 115, 95, 85),
                    new ColorStop(0.50f, 180, 165, 150),
                    new ColorStop(0.65f, 235, 225, 215),
                    new ColorStop(1.00f, 255, 250, 245)
                ),
                new RampDefinition(
                    "Ramp_Skin_Default",
                    FilterMode.Bilinear,
                    new ColorStop(0.00f, 110, 75, 80),
                    new ColorStop(0.35f, 160, 115, 115),
                    new ColorStop(0.50f, 210, 175, 170),
                    new ColorStop(0.65f, 240, 215, 205),
                    new ColorStop(1.00f, 255, 240, 230)
                ),
                new RampDefinition(
                    "Ramp_Hair_Default",
                    FilterMode.Bilinear,
                    new ColorStop(0.00f, 40, 45, 60),
                    new ColorStop(0.35f, 80, 85, 105),
                    new ColorStop(0.50f, 140, 140, 155),
                    new ColorStop(0.65f, 215, 215, 220),
                    new ColorStop(1.00f, 255, 255, 255)
                ),
                new RampDefinition(
                    "Ramp_Cloth_Default",
                    FilterMode.Bilinear,
                    new ColorStop(0.00f, 75, 85, 100),
                    new ColorStop(0.35f, 110, 120, 135),
                    new ColorStop(0.50f, 170, 175, 185),
                    new ColorStop(0.65f, 225, 225, 230),
                    new ColorStop(1.00f, 250, 250, 250)
                ),
                new RampDefinition(
                    "Ramp_HardCel_2Tone",
                    FilterMode.Point,
                    new ColorStop(0.000f, 60, 70, 90),
                    new ColorStop(0.499f, 60, 70, 90),
                    new ColorStop(0.501f, 255, 255, 255),
                    new ColorStop(1.000f, 255, 255, 255)
                )
            };

            for (int i = 0; i < ramps.Length; i++)
            {
                GenerateRamp(ramps[i], outputFolder);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            UnityEngine.Object outputFolderAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(outputFolder);
            if (outputFolderAsset != null)
            {
                EditorGUIUtility.PingObject(outputFolderAsset);
            }

            Debug.Log("[DToon] Generated " + ramps.Length + " ramp textures at " + outputFolder + ".");
        }

        internal static bool HasGenerateDefaultRampsMenuForHarness()
        {
            MethodInfo method = typeof(RampTextureGenerator).GetMethod(
                nameof(GenerateDefaultRampTextures),
                BindingFlags.Public | BindingFlags.Static
            );

            if (method == null)
            {
                return false;
            }

            object[] attributes = method.GetCustomAttributes(typeof(MenuItem), false);
            for (int i = 0; i < attributes.Length; i++)
            {
                MenuItem menuItem = attributes[i] as MenuItem;
                if (menuItem != null && menuItem.menuItem == GenerateDefaultRampsMenuPath)
                {
                    return true;
                }
            }

            return false;
        }

        private static void GenerateRamp(RampDefinition definition, string outputFolder)
        {
            Texture2D texture = new Texture2D(RampWidth, RampHeight, TextureFormat.RGBA32, false, false)
            {
                name = definition.Filename,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = definition.FilterMode
            };

            Color[] pixels = new Color[RampWidth * RampHeight];
            for (int x = 0; x < RampWidth; x++)
            {
                float t = (float)x / (RampWidth - 1);
                Color color = Evaluate(definition, t);

                for (int y = 0; y < RampHeight; y++)
                {
                    pixels[y * RampWidth + x] = color;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, false);

            string assetPath = outputFolder + "/" + definition.Filename + ".png";
            string absolutePath = AssetPathToAbsolutePath(assetPath);
            string absoluteFolder = Path.GetDirectoryName(absolutePath);
            if (!string.IsNullOrEmpty(absoluteFolder))
            {
                Directory.CreateDirectory(absoluteFolder);
            }

            byte[] png = texture.EncodeToPNG();
            File.WriteAllBytes(absolutePath, png);
            UnityEngine.Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);

            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException("Failed to import ramp texture: " + assetPath);
            }

            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = definition.FilterMode;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.isReadable = false;
            importer.SaveAndReimport();
        }

        private static Color Evaluate(RampDefinition definition, float t)
        {
            ColorStop[] stops = definition.ColorStops;
            if (stops.Length == 0)
            {
                return Color.white;
            }

            if (t <= stops[0].Position)
            {
                return stops[0].Color;
            }

            for (int i = 1; i < stops.Length; i++)
            {
                ColorStop previous = stops[i - 1];
                ColorStop next = stops[i];

                if (t <= next.Position)
                {
                    float range = Mathf.Max(next.Position - previous.Position, 0.0001f);
                    float u = Mathf.Clamp01((t - previous.Position) / range);
                    return Color.Lerp(previous.Color, next.Color, u);
                }
            }

            return stops[stops.Length - 1].Color;
        }

        private static string EnsureOutputFolder()
        {
            string absoluteFolder = AssetPathToAbsolutePath(GeneratedRampOutputFolder);
            Directory.CreateDirectory(absoluteFolder);
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            return GeneratedRampOutputFolder;
        }

        private static string AssetPathToAbsolutePath(string assetPath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            return Path.GetFullPath(Path.Combine(projectRoot, assetPath));
        }

        private readonly struct RampDefinition
        {
            public readonly string Filename;
            public readonly ColorStop[] ColorStops;
            public readonly FilterMode FilterMode;

            public RampDefinition(string filename, FilterMode filterMode, params ColorStop[] colorStops)
            {
                Filename = filename;
                FilterMode = filterMode;
                ColorStops = colorStops;
            }
        }

        private readonly struct ColorStop
        {
            public readonly float Position;
            public readonly Color Color;

            public ColorStop(float position, byte r, byte g, byte b)
            {
                Position = Mathf.Clamp01(position);
                Color = new Color32(r, g, b, 255);
            }
        }
    }
}
