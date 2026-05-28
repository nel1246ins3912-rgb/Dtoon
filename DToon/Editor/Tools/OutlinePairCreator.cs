using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DToon.Editor.Tools
{
    public static class OutlinePairCreator
    {
        private const string MenuPath = "Tools/DToon/Create Outline Pair from Selected Material";
        private const string CharacterShaderName = "DToon/Character";
        private const string OutlineShaderName = "DToon/Outline";

        [MenuItem(MenuPath, priority = 120)]
        public static void CreateOutlinePairFromSelectedMaterial()
        {
            Material source = Selection.activeObject as Material;
            if (source == null)
            {
                EditorUtility.DisplayDialog("DToon Outline Pair", "Select a DToon/Character material first.", "OK");
                return;
            }

            if (source.shader == null || source.shader.name != CharacterShaderName)
            {
                EditorUtility.DisplayDialog("DToon Outline Pair", "Selected material must use DToon/Character.", "OK");
                return;
            }

            try
            {
                Material outlineMaterial = CreateOrUpdateOutlinePair(source);
                Selection.activeObject = outlineMaterial;
                EditorGUIUtility.PingObject(outlineMaterial);
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("DToon Outline Pair Failed", e.Message, "OK");
                Debug.LogException(e);
            }
        }

        [MenuItem(MenuPath, true)]
        public static bool ValidateCreateOutlinePairFromSelectedMaterial()
        {
            return Selection.activeObject is Material;
        }

        public static Material CreateOrUpdateOutlinePair(Material source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            Shader outlineShader = Shader.Find(OutlineShaderName);
            if (outlineShader == null)
            {
                throw new InvalidOperationException("Shader not found: " + OutlineShaderName);
            }

            string sourcePath = AssetDatabase.GetAssetPath(source);
            if (string.IsNullOrEmpty(sourcePath))
            {
                throw new InvalidOperationException("Selected material must be saved as a project asset.");
            }

            string folder = Path.GetDirectoryName(sourcePath);
            string baseName = Path.GetFileNameWithoutExtension(sourcePath);
            string outlinePath = (string.IsNullOrEmpty(folder) ? string.Empty : folder.Replace("\\", "/") + "/")
                + baseName + " (OutlineHull).mat";

            Material outlineMaterial = AssetDatabase.LoadAssetAtPath<Material>(outlinePath);
            if (outlineMaterial == null)
            {
                outlineMaterial = new Material(outlineShader);
                AssetDatabase.CreateAsset(outlineMaterial, outlinePath);
            }

            ApplyOutlinePairSettings(source, outlineMaterial);
            AssetDatabase.SaveAssets();
            return outlineMaterial;
        }

        public static void ApplyOutlinePairSettings(Material source, Material outlineMaterial)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (outlineMaterial == null)
            {
                throw new ArgumentNullException(nameof(outlineMaterial));
            }

            Shader outlineShader = Shader.Find(OutlineShaderName);
            if (outlineShader == null)
            {
                throw new InvalidOperationException("Shader not found: " + OutlineShaderName);
            }

            outlineMaterial.shader = outlineShader;

            CopyTexture(source, outlineMaterial, "_BaseMap");
            CopyColor(source, outlineMaterial, "_BaseColor", Color.white);
            CopyFloat(source, outlineMaterial, "_Cutoff", 0.5f);

            outlineMaterial.SetColor("_OutlineColor", Color.black);
            CopyFloat(source, outlineMaterial, "_OutlineWidth", 0.005f);
            CopyFloat(source, outlineMaterial, "_OutlineDarkening", 0.3f);
            CopyFloat(source, outlineMaterial, "_OutlineDistanceScale", 1.0f);
            CopyFloat(source, outlineMaterial, "_OutlineMaxWidth", 0.05f);

            outlineMaterial.SetFloat("_OutlineAlphaClip", 1.0f);
            outlineMaterial.SetFloat("_OutlineCull", 0.0f);
            outlineMaterial.SetFloat("_OutlineZTest", 8.0f);
            outlineMaterial.EnableKeyword("_OUTLINE_ALPHACLIP");
            outlineMaterial.renderQueue = 2001;

            EditorUtility.SetDirty(outlineMaterial);
        }

        private static void CopyTexture(Material source, Material target, string propertyName)
        {
            if (!source.HasProperty(propertyName) || !target.HasProperty(propertyName))
            {
                return;
            }

            target.SetTexture(propertyName, source.GetTexture(propertyName));
            target.SetTextureScale(propertyName, source.GetTextureScale(propertyName));
            target.SetTextureOffset(propertyName, source.GetTextureOffset(propertyName));
        }

        private static void CopyColor(Material source, Material target, string propertyName, Color fallback)
        {
            if (!target.HasProperty(propertyName))
            {
                return;
            }

            target.SetColor(propertyName, source.HasProperty(propertyName) ? source.GetColor(propertyName) : fallback);
        }

        private static void CopyFloat(Material source, Material target, string propertyName, float fallback)
        {
            if (!target.HasProperty(propertyName))
            {
                return;
            }

            target.SetFloat(propertyName, source.HasProperty(propertyName) ? source.GetFloat(propertyName) : fallback);
        }
    }
}
