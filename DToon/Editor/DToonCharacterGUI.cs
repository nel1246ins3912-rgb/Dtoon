// ============================================================================
//  DToonCharacterGUI.cs
//  ----------------------------------------------------------------------------
//  Custom material inspector for DToon/Character.
// ============================================================================

using UnityEditor;
using UnityEngine;

namespace DToon
{
    public sealed class DToonCharacterGUI : ShaderGUI
    {
        private bool baseFoldout = true;
        private bool celShadingFoldout = true;
        private bool outlineFoldout = true;
        private bool rimFoldout;
        private bool matcapFoldout;
        private bool specularFoldout;
        private bool alphaClipFoldout;

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            MaterialProperty baseMap = FindProperty("_BaseMap", properties);
            MaterialProperty baseColor = FindProperty("_BaseColor", properties);

            MaterialProperty rampMap = FindProperty("_RampMap", properties);
            MaterialProperty rampOffset = FindProperty("_RampOffset", properties);
            MaterialProperty shadowTint = FindProperty("_ShadowTint", properties);
            MaterialProperty receiveShadowsStrength = FindProperty("_ReceiveShadowsStrength", properties);

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

            MaterialProperty alphaClip = FindProperty("_AlphaClip", properties);
            MaterialProperty cutoff = FindProperty("_Cutoff", properties);

            DrawBase(materialEditor, baseMap, baseColor);
            DrawCelShading(materialEditor, rampMap, rampOffset, shadowTint, receiveShadowsStrength);
            DrawOutline(materialEditor, outlineEnable, outlineWidth, outlineDarkening, outlineDistanceScale, outlineMaxWidth);
            DrawRim(materialEditor, rimEnable, rimLightAware, rimColor, rimIntensity, rimPower, rimSoftness);
            DrawMatcap(materialEditor, matcapEnable, matcapMode, matcapTex, matcapColor, matcapIntensity);
            DrawSpecular(materialEditor, specularEnable, specularColor, specularIntensity, specularPower, specularThreshold, specularSoftness);
            DrawAlphaClip(materialEditor, alphaClip, cutoff);

            SyncKeywords(materialEditor);
        }

        private void DrawBase(MaterialEditor materialEditor, MaterialProperty baseMap, MaterialProperty baseColor)
        {
            baseFoldout = DrawFoldout(baseFoldout, "Base");
            if (!baseFoldout)
            {
                return;
            }

            using (new EditorGUI.IndentLevelScope())
            {
                materialEditor.TexturePropertySingleLine(new GUIContent("Base Map"), baseMap, baseColor);
            }
        }

        private void DrawCelShading(
            MaterialEditor materialEditor,
            MaterialProperty rampMap,
            MaterialProperty rampOffset,
            MaterialProperty shadowTint,
            MaterialProperty receiveShadowsStrength
        )
        {
            celShadingFoldout = DrawFoldout(celShadingFoldout, "Cel Shading");
            if (!celShadingFoldout)
            {
                return;
            }

            using (new EditorGUI.IndentLevelScope())
            {
                materialEditor.TexturePropertySingleLine(new GUIContent("Ramp Map (1D)"), rampMap);
                materialEditor.ShaderProperty(rampOffset, rampOffset.displayName);
                materialEditor.ShaderProperty(shadowTint, shadowTint.displayName);
                materialEditor.ShaderProperty(receiveShadowsStrength, receiveShadowsStrength.displayName);
            }
        }

        private void DrawOutline(
            MaterialEditor materialEditor,
            MaterialProperty outlineEnable,
            MaterialProperty outlineWidth,
            MaterialProperty outlineDarkening,
            MaterialProperty outlineDistanceScale,
            MaterialProperty outlineMaxWidth
        )
        {
            outlineFoldout = DrawFoldout(outlineFoldout, "Outline");
            if (!outlineFoldout)
            {
                return;
            }

            using (new EditorGUI.IndentLevelScope())
            {
                materialEditor.ShaderProperty(outlineEnable, outlineEnable.displayName);
                SyncOutlineKeywords(materialEditor);

                EditorGUI.BeginDisabledGroup(!IsToggleEnabled(outlineEnable));
                materialEditor.ShaderProperty(outlineWidth, outlineWidth.displayName);
                materialEditor.ShaderProperty(outlineDarkening, outlineDarkening.displayName);
                materialEditor.ShaderProperty(outlineDistanceScale, outlineDistanceScale.displayName);
                materialEditor.ShaderProperty(outlineMaxWidth, outlineMaxWidth.displayName);
                EditorGUI.EndDisabledGroup();
            }
        }

        private void DrawRim(
            MaterialEditor materialEditor,
            MaterialProperty rimEnable,
            MaterialProperty rimLightAware,
            MaterialProperty rimColor,
            MaterialProperty rimIntensity,
            MaterialProperty rimPower,
            MaterialProperty rimSoftness
        )
        {
            rimFoldout = DrawFoldout(rimFoldout, "Rim");
            if (!rimFoldout)
            {
                return;
            }

            using (new EditorGUI.IndentLevelScope())
            {
                materialEditor.ShaderProperty(rimEnable, rimEnable.displayName);
                SyncRimKeywords(materialEditor);

                EditorGUI.BeginDisabledGroup(!IsToggleEnabled(rimEnable));
                materialEditor.ShaderProperty(rimLightAware, rimLightAware.displayName);
                SyncRimKeywords(materialEditor);
                materialEditor.ShaderProperty(rimColor, rimColor.displayName);
                materialEditor.ShaderProperty(rimIntensity, rimIntensity.displayName);
                materialEditor.ShaderProperty(rimPower, rimPower.displayName);
                materialEditor.ShaderProperty(rimSoftness, rimSoftness.displayName);
                EditorGUI.EndDisabledGroup();
            }
        }

        private void DrawMatcap(
            MaterialEditor materialEditor,
            MaterialProperty matcapEnable,
            MaterialProperty matcapMode,
            MaterialProperty matcapTex,
            MaterialProperty matcapColor,
            MaterialProperty matcapIntensity
        )
        {
            matcapFoldout = DrawFoldout(matcapFoldout, "Matcap");
            if (!matcapFoldout)
            {
                return;
            }

            using (new EditorGUI.IndentLevelScope())
            {
                materialEditor.ShaderProperty(matcapEnable, matcapEnable.displayName);
                SyncMatcapKeywords(materialEditor);

                EditorGUI.BeginDisabledGroup(!IsToggleEnabled(matcapEnable));
                materialEditor.ShaderProperty(matcapMode, matcapMode.displayName);
                materialEditor.TexturePropertySingleLine(new GUIContent("Matcap Texture"), matcapTex, matcapColor);
                materialEditor.ShaderProperty(matcapIntensity, matcapIntensity.displayName);
                EditorGUI.EndDisabledGroup();
            }
        }

        private void DrawSpecular(
            MaterialEditor materialEditor,
            MaterialProperty specularEnable,
            MaterialProperty specularColor,
            MaterialProperty specularIntensity,
            MaterialProperty specularPower,
            MaterialProperty specularThreshold,
            MaterialProperty specularSoftness
        )
        {
            specularFoldout = DrawFoldout(specularFoldout, "Specular");
            if (!specularFoldout)
            {
                return;
            }

            using (new EditorGUI.IndentLevelScope())
            {
                materialEditor.ShaderProperty(specularEnable, specularEnable.displayName);
                SyncSpecularKeywords(materialEditor);

                EditorGUI.BeginDisabledGroup(!IsToggleEnabled(specularEnable));
                materialEditor.ShaderProperty(specularColor, specularColor.displayName);
                materialEditor.ShaderProperty(specularIntensity, specularIntensity.displayName);
                materialEditor.ShaderProperty(specularPower, specularPower.displayName);
                materialEditor.ShaderProperty(specularThreshold, specularThreshold.displayName);
                materialEditor.ShaderProperty(specularSoftness, specularSoftness.displayName);
                EditorGUI.EndDisabledGroup();
            }
        }

        private void DrawAlphaClip(MaterialEditor materialEditor, MaterialProperty alphaClip, MaterialProperty cutoff)
        {
            alphaClipFoldout = DrawFoldout(alphaClipFoldout, "Alpha Clip");
            if (!alphaClipFoldout)
            {
                return;
            }

            using (new EditorGUI.IndentLevelScope())
            {
                materialEditor.ShaderProperty(alphaClip, alphaClip.displayName);
                SyncAlphaClipKeywords(materialEditor);

                EditorGUI.BeginDisabledGroup(!IsToggleEnabled(alphaClip));
                materialEditor.ShaderProperty(cutoff, cutoff.displayName);
                EditorGUI.EndDisabledGroup();
            }
        }

        private static bool DrawFoldout(bool value, string title)
        {
            EditorGUILayout.Space(6.0f);
            return EditorGUILayout.Foldout(value, title, true, EditorStyles.foldoutHeader);
        }

        private static bool IsToggleEnabled(MaterialProperty property)
        {
            return property.hasMixedValue || property.floatValue > 0.5f;
        }

        private static void SyncKeywords(MaterialEditor materialEditor)
        {
            SyncOutlineKeywords(materialEditor);
            SyncRimKeywords(materialEditor);
            SyncMatcapKeywords(materialEditor);
            SyncSpecularKeywords(materialEditor);
            SyncAlphaClipKeywords(materialEditor);
        }

        private static void SyncOutlineKeywords(MaterialEditor materialEditor)
        {
            SyncKeyword(materialEditor, "_OutlineEnable", "_OUTLINE");
        }

        private static void SyncRimKeywords(MaterialEditor materialEditor)
        {
            foreach (Object target in materialEditor.targets)
            {
                Material material = target as Material;
                if (material == null)
                {
                    continue;
                }

                bool rimEnabled = IsMaterialToggleEnabled(material, "_RimEnable");
                bool lightAware = rimEnabled && IsMaterialToggleEnabled(material, "_RimLightAware");
                SetKeyword(material, "_RIM", rimEnabled);
                SetKeyword(material, "_RIM_LIGHT_AWARE", lightAware);
            }
        }

        private static void SyncMatcapKeywords(MaterialEditor materialEditor)
        {
            SyncKeyword(materialEditor, "_MatcapEnable", "_MATCAP");
        }

        private static void SyncSpecularKeywords(MaterialEditor materialEditor)
        {
            SyncKeyword(materialEditor, "_SpecularEnable", "_SPECULAR");
        }

        private static void SyncAlphaClipKeywords(MaterialEditor materialEditor)
        {
            foreach (Object target in materialEditor.targets)
            {
                Material material = target as Material;
                if (material == null)
                {
                    continue;
                }

                bool enabled = IsMaterialToggleEnabled(material, "_AlphaClip");
                SetKeyword(material, "_ALPHACLIP", enabled);
                SetKeyword(material, "_ALPHATEST_ON", enabled);
            }
        }

        private static void SyncKeyword(MaterialEditor materialEditor, string propertyName, string keyword)
        {
            foreach (Object target in materialEditor.targets)
            {
                Material material = target as Material;
                if (material == null)
                {
                    continue;
                }

                SetKeyword(material, keyword, IsMaterialToggleEnabled(material, propertyName));
            }
        }

        private static bool IsMaterialToggleEnabled(Material material, string propertyName)
        {
            return material.HasProperty(propertyName) && material.GetFloat(propertyName) > 0.5f;
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
    }
}
