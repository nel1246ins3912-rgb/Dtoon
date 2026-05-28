// ============================================================================
//  DToonCharacterGUI.cs
//  ----------------------------------------------------------------------------
//  Custom material inspector for DToon/Character.
//  Step 1 focuses on base color plus ramp-texture cel shading controls.
// ============================================================================

using UnityEditor;
using UnityEngine;

namespace DToon.Editor
{
    public class DToonCharacterGUI : ShaderGUI
    {
        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            MaterialProperty baseMap = FindProperty("_BaseMap", properties);
            MaterialProperty baseColor = FindProperty("_BaseColor", properties);
            MaterialProperty rampMap = FindProperty("_RampMap", properties);
            MaterialProperty rampOffset = FindProperty("_RampOffset", properties);
            MaterialProperty shadowTint = FindProperty("_ShadowTint", properties);
            MaterialProperty receiveShadowsStrength = FindProperty("_ReceiveShadowsStrength", properties);
            MaterialProperty alphaClip = FindProperty("_AlphaClip", properties);
            MaterialProperty cutoff = FindProperty("_Cutoff", properties);
            MaterialProperty outlineEnable = FindProperty("_OutlineEnable", properties);
            MaterialProperty outlineWidth = FindProperty("_OutlineWidth", properties);
            MaterialProperty outlineDarkening = FindProperty("_OutlineDarkening", properties);
            MaterialProperty outlineDistanceScale = FindProperty("_OutlineDistanceScale", properties);
            MaterialProperty outlineMaxWidth = FindProperty("_OutlineMaxWidth", properties);
            MaterialProperty rimEnable = FindProperty("_RimEnable", properties);
            MaterialProperty rimLightAware = FindProperty("_RimLightAware", properties);
            MaterialProperty rimColor = FindProperty("_RimColor", properties);
            MaterialProperty rimIntensity = FindProperty("_RimIntensity", properties);
            MaterialProperty rimPower = FindProperty("_RimPower", properties);
            MaterialProperty rimSoftness = FindProperty("_RimSoftness", properties);
            MaterialProperty matcapEnable = FindProperty("_MatcapEnable", properties);
            MaterialProperty matcapMode = FindProperty("_MatcapMode", properties);
            MaterialProperty matcapTex = FindProperty("_MatcapTex", properties);
            MaterialProperty matcapColor = FindProperty("_MatcapColor", properties);
            MaterialProperty matcapIntensity = FindProperty("_MatcapIntensity", properties);
            MaterialProperty specularEnable = FindProperty("_SpecularEnable", properties);
            MaterialProperty specularColor = FindProperty("_SpecularColor", properties);
            MaterialProperty specularIntensity = FindProperty("_SpecularIntensity", properties);
            MaterialProperty specularPower = FindProperty("_SpecularPower", properties);
            MaterialProperty specularThreshold = FindProperty("_SpecularThreshold", properties);
            MaterialProperty specularSoftness = FindProperty("_SpecularSoftness", properties);

            EditorGUILayout.LabelField("Base", EditorStyles.boldLabel);
            materialEditor.TexturePropertySingleLine(new GUIContent("Base Map"), baseMap, baseColor);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Cel Shading", EditorStyles.boldLabel);
            materialEditor.TexturePropertySingleLine(new GUIContent("Ramp Map (1D)"), rampMap);
            materialEditor.ShaderProperty(rampOffset, "Ramp Offset");
            materialEditor.ShaderProperty(shadowTint, "Shadow Tint");
            materialEditor.ShaderProperty(receiveShadowsStrength, "Receive Shadows Strength");

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Alpha Clip", EditorStyles.boldLabel);
            materialEditor.ShaderProperty(alphaClip, "Enable Alpha Clip");
            materialEditor.ShaderProperty(cutoff, "Alpha Cutoff Threshold");

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Outline", EditorStyles.boldLabel);
            materialEditor.ShaderProperty(outlineEnable, "Enable Outline");
            materialEditor.ShaderProperty(outlineWidth, "Outline Width (world units)");
            materialEditor.ShaderProperty(outlineDarkening, "Outline Color Darkening");
            materialEditor.ShaderProperty(outlineDistanceScale, "Distance Scale (0=fixed world, 1=screen-space)");
            materialEditor.ShaderProperty(outlineMaxWidth, "Max Outline Width (world units)");

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Rim Light", EditorStyles.boldLabel);
            materialEditor.ShaderProperty(rimEnable, "Enable Rim Light");
            materialEditor.ShaderProperty(rimLightAware, "Light-Aware Rim");
            materialEditor.ShaderProperty(rimColor, "Rim Color");
            materialEditor.ShaderProperty(rimIntensity, "Rim Intensity");
            materialEditor.ShaderProperty(rimPower, "Rim Power (Falloff)");
            materialEditor.ShaderProperty(rimSoftness, "Rim Edge Softness");

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Matcap", EditorStyles.boldLabel);
            materialEditor.ShaderProperty(matcapEnable, "Enable Matcap");
            materialEditor.ShaderProperty(matcapMode, "Matcap Mode");
            materialEditor.TexturePropertySingleLine(new GUIContent("Matcap Texture"), matcapTex, matcapColor);
            materialEditor.ShaderProperty(matcapIntensity, "Matcap Intensity");

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Stepped Specular", EditorStyles.boldLabel);
            materialEditor.ShaderProperty(specularEnable, "Enable Specular");
            materialEditor.ShaderProperty(specularColor, "Specular Color");
            materialEditor.ShaderProperty(specularIntensity, "Specular Intensity");
            materialEditor.ShaderProperty(specularPower, "Specular Power (Sharpness)");
            materialEditor.ShaderProperty(specularThreshold, "Specular Threshold");
            materialEditor.ShaderProperty(specularSoftness, "Specular Edge Softness");

            SyncAlphaClipKeywords(materialEditor);
            SyncOutlineKeywords(materialEditor);
            SyncRimKeywords(materialEditor);
            SyncMatcapKeywords(materialEditor);
            SyncSpecularKeywords(materialEditor);
        }

        private static void SyncAlphaClipKeywords(MaterialEditor materialEditor)
        {
            foreach (UnityEngine.Object target in materialEditor.targets)
            {
                Material material = target as Material;
                if (material == null)
                {
                    continue;
                }

                bool enabled = material.HasProperty("_AlphaClip") && material.GetFloat("_AlphaClip") > 0.5f;
                SetKeyword(material, "_ALPHACLIP", enabled);
                SetKeyword(material, "_ALPHATEST_ON", enabled);
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

        private static void SyncOutlineKeywords(MaterialEditor materialEditor)
        {
            foreach (UnityEngine.Object target in materialEditor.targets)
            {
                Material material = target as Material;
                if (material == null)
                {
                    continue;
                }

                bool enabled = material.HasProperty("_OutlineEnable") && material.GetFloat("_OutlineEnable") > 0.5f;
                SetKeyword(material, "_OUTLINE", enabled);
            }
        }

        private static void SyncRimKeywords(MaterialEditor materialEditor)
        {
            foreach (UnityEngine.Object target in materialEditor.targets)
            {
                Material material = target as Material;
                if (material == null)
                {
                    continue;
                }

                bool rimEnabled = material.HasProperty("_RimEnable") && material.GetFloat("_RimEnable") > 0.5f;
                bool lightAware = material.HasProperty("_RimLightAware") && material.GetFloat("_RimLightAware") > 0.5f;
                SetKeyword(material, "_RIM", rimEnabled);
                SetKeyword(material, "_RIM_LIGHT_AWARE", rimEnabled && lightAware);
            }
        }

        private static void SyncMatcapKeywords(MaterialEditor materialEditor)
        {
            foreach (UnityEngine.Object target in materialEditor.targets)
            {
                Material material = target as Material;
                if (material == null)
                {
                    continue;
                }

                bool enabled = material.HasProperty("_MatcapEnable") && material.GetFloat("_MatcapEnable") > 0.5f;
                SetKeyword(material, "_MATCAP", enabled);
            }
        }

        private static void SyncSpecularKeywords(MaterialEditor materialEditor)
        {
            foreach (UnityEngine.Object target in materialEditor.targets)
            {
                Material material = target as Material;
                if (material == null)
                {
                    continue;
                }

                bool enabled = material.HasProperty("_SpecularEnable") && material.GetFloat("_SpecularEnable") > 0.5f;
                SetKeyword(material, "_SPECULAR", enabled);
            }
        }
    }
}
