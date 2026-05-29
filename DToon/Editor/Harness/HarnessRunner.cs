// ============================================================================
//  HarnessRunner.cs
//  ----------------------------------------------------------------------------
//  Entry point invoked from Unity batch mode by the test_shader scripts.
//  Loads a named test scene, renders the harness camera into a RenderTexture,
//  encodes the result to PNG, and exits with a clean status code.
//
//  Usage from CLI (handled by tools/test_shader.sh / .ps1):
//      Unity.exe -batchmode -projectPath <proj> \
//                -executeMethod DToon.Editor.Harness.HarnessRunner.RenderTest \
//                -testName <name> \
//                -outputPath <abs-path-to-png> \
//                -logFile <abs-path-to-log> \
//                -quit
//
//  The agent should NEVER call Unity directly. It calls the wrapper scripts
//  in /tools, which set the right paths and parse the exit code.
// ============================================================================

using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using DToon.URP14;
using DToon.Editor.Tools;

namespace DToon.Editor.Harness
{
    public static class HarnessRunner
    {
        private const string Step1RampLitFrontlitTestName = "Step1_RampLit_Frontlit";
        private const string Step2AlphaClipHairTestName = "Step2_AlphaClip_Hair";
        private const string Step2ReceiveShadowOnOffTestName = "Step2_ReceiveShadow_OnOff";
        private const string Step3OutlineCloseupTestName = "Step3_Outline_Closeup";
        private const string Step3OutlineHairTestName = "Step3_Outline_Hair";
        private const string Step4RimCloseupTestName = "Step4_Rim_Closeup";
        private const string Step4RimCloseupNoOutlineTestName = "Step4_Rim_Closeup_NoOutline";
        private const string Step4MatcapCloseupTestName = "Step4_Matcap_Closeup";
        private const string Step4SpecularCloseupTestName = "Step4_Specular_Closeup";
        private const string Step6SmoothNormalCubeBakeTestName = "Step6_SmoothNormal_CubeBake";
        private const string DToonTestMaterialPath = "Assets/DToon/Samples/Harness/_Common/Materials/M_DToon_Test_Generic.mat";
        private const string DToonHairMaterialPath = "Assets/DToon/Samples/Harness/_Common/Materials/M_DToon_Test_Hair.mat";
        private const string DToonOutlineMaterialPath = "Assets/DToon/Samples/Harness/_Common/Materials/M_DToon_Test_Outline.mat";
        private const string DToonOutlineHullMaterialPath = "Assets/DToon/Samples/Harness/_Common/Materials/M_DToon_Test_OutlineHull.mat";
        private const string DToonOutlineCloseupHullMaterialPath = "Assets/DToon/Samples/Harness/_Common/Materials/M_DToon_Test_OutlineCloseupHull.mat";
        private const string DToonHairOutlinedMaterialPath = "Assets/DToon/Samples/Harness/_Common/Materials/M_DToon_Test_Hair_Outlined.mat";
        private const string DToonHairOutlineHullMaterialPath = "Assets/DToon/Samples/Harness/_Common/Materials/M_DToon_Test_Hair_OutlineHull.mat";
        private const string DToonRimMaterialPath = "Assets/DToon/Samples/Harness/_Common/Materials/M_DToon_Test_Rim.mat";
        private const string DToonRimNoOutlineMaterialPath = "Assets/DToon/Samples/Harness/_Common/Materials/M_DToon_Test_Rim_NoOutline.mat";
        private const string DToonRimOutlineHullMaterialPath = "Assets/DToon/Samples/Harness/_Common/Materials/M_DToon_Test_Rim_OutlineHull.mat";
        private const string DToonMatcapEyeMaterialPath = "Assets/DToon/Samples/Harness/_Common/Materials/M_DToon_Test_Matcap_Eye.mat";
        private const string DToonMatcapMetalMaterialPath = "Assets/DToon/Samples/Harness/_Common/Materials/M_DToon_Test_Matcap_Metal.mat";
        private const string DToonMatcapSkinMaterialPath = "Assets/DToon/Samples/Harness/_Common/Materials/M_DToon_Test_Matcap_Skin.mat";
        private const string DToonMatcapClothMaterialPath = "Assets/DToon/Samples/Harness/_Common/Materials/M_DToon_Test_Matcap_Cloth.mat";
        private const string DToonMatcapEyeOutlineHullMaterialPath = "Assets/DToon/Samples/Harness/_Common/Materials/M_DToon_Test_Matcap_Eye_OutlineHull.mat";
        private const string DToonMatcapMetalOutlineHullMaterialPath = "Assets/DToon/Samples/Harness/_Common/Materials/M_DToon_Test_Matcap_Metal_OutlineHull.mat";
        private const string DToonMatcapSkinOutlineHullMaterialPath = "Assets/DToon/Samples/Harness/_Common/Materials/M_DToon_Test_Matcap_Skin_OutlineHull.mat";
        private const string DToonMatcapClothOutlineHullMaterialPath = "Assets/DToon/Samples/Harness/_Common/Materials/M_DToon_Test_Matcap_Cloth_OutlineHull.mat";
        private const string DToonSpecularMetalMaterialPath = "Assets/DToon/Samples/Harness/_Common/Materials/M_DToon_Test_Specular_Metal.mat";
        private const string DToonSpecularSkinMaterialPath = "Assets/DToon/Samples/Harness/_Common/Materials/M_DToon_Test_Specular_Skin.mat";
        private const string DToonSpecularPlasticMaterialPath = "Assets/DToon/Samples/Harness/_Common/Materials/M_DToon_Test_Specular_Plastic.mat";
        private const string DToonSpecularHairMaterialPath = "Assets/DToon/Samples/Harness/_Common/Materials/M_DToon_Test_Specular_Hair.mat";
        private const string DToonSpecularMetalOutlineHullMaterialPath = "Assets/DToon/Samples/Harness/_Common/Materials/M_DToon_Test_Specular_Metal_OutlineHull.mat";
        private const string DToonSpecularSkinOutlineHullMaterialPath = "Assets/DToon/Samples/Harness/_Common/Materials/M_DToon_Test_Specular_Skin_OutlineHull.mat";
        private const string DToonSpecularPlasticOutlineHullMaterialPath = "Assets/DToon/Samples/Harness/_Common/Materials/M_DToon_Test_Specular_Plastic_OutlineHull.mat";
        private const string DToonSpecularHairOutlineHullMaterialPath = "Assets/DToon/Samples/Harness/_Common/Materials/M_DToon_Test_Specular_Hair_OutlineHull.mat";
        private const string HairTestTexturePath = "Assets/DToon/Samples/TestAssets/HairStrands_Alpha.png";

        // ----- Public entry points ------------------------------------------

        [MenuItem("Tools/DToon/Harness/Capture Step1 RampLit Frontlit", priority = 200)]
        public static void CaptureStep1RampLitFrontlitFromMenu()
        {
            CaptureStep1FromMenu(Step1RampLitFrontlitTestName);
        }

        [MenuItem("Tools/DToon/Generate Hair Test Texture", priority = 91)]
        public static void GenerateHairTestTextureFromMenu()
        {
            CreateOrUpdateHairTestTexture();
        }

        /// <summary>
        /// Render a single named test scene to PNG.
        /// </summary>
        public static void RenderTest()
        {
            int exitCode = 1;
            try
            {
                string testName   = GetArg("-testName");
                string outputPath = GetArg("-outputPath");

                if (string.IsNullOrEmpty(testName))
                    throw new Exception("Missing required arg: -testName");
                if (string.IsNullOrEmpty(outputPath))
                    throw new Exception("Missing required arg: -outputPath");

                Log($"[Harness] RenderTest start. testName='{testName}' outputPath='{outputPath}'");

                // 1. Force synchronous shader compilation so we never capture a
                //    pink "still compiling" frame.
                ShaderUtil.allowAsyncCompilation = false;

                // 2. Resolve and load the harness scene.
                CreateGeneratedHarnessSceneIfNeeded(testName);
                string scenePath = ResolveScenePath(testName);
                if (!File.Exists(scenePath))
                    throw new FileNotFoundException($"Harness scene not found: {scenePath}");

                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                Log($"[Harness] Loaded scene: {scenePath}");

                // 3. Find the harness camera (must be tagged or named).
                Camera cam = FindHarnessCamera();
                if (cam == null)
                    throw new Exception("HarnessCamera not found in scene. " +
                                        "Tag a camera 'MainCamera' AND name it 'HarnessCamera'.");

                // 4. Render.
                // 5. Encode and write.
                WriteCameraCapture(cam, outputPath);

                Log($"[Harness] Wrote capture to {outputPath}");
                exitCode = 0;
            }
            catch (Exception e)
            {
                Log($"[Harness] FAILED: {e.GetType().Name}: {e.Message}");
                Log(e.StackTrace);
                exitCode = 2;
            }
            finally
            {
                Log($"[Harness] Exiting with code {exitCode}");
                EditorApplication.Exit(exitCode);
            }
        }

        /// <summary>
        /// Compile-only sanity check. Loads the scene, ensures shaders compile,
        /// but does not render. Faster (~5s) for tight inner-loop iterations.
        /// </summary>
        public static void CompileCheck()
        {
            int exitCode = 1;
            try
            {
                string testName = GetArg("-testName");
                if (string.IsNullOrEmpty(testName))
                    throw new Exception("Missing required arg: -testName");

                if (testName == "Step1_RampTextureInfrastructure")
                {
                    if (!RampTextureGenerator.HasGenerateDefaultRampsMenuForHarness())
                        throw new Exception("Ramp texture generator menu is missing.");

                    Log("[Harness] RampTextureGenerator menu OK");
                    exitCode = 0;
                    return;
                }

                if (testName == Step6SmoothNormalCubeBakeTestName)
                {
                    string reportPath = SmoothNormalBaker.WriteCubeBakeReportForHarness();
                    Log("[Harness] SmoothNormalBaker cube report -> " + reportPath);
                    exitCode = 0;
                    return;
                }

                ShaderUtil.allowAsyncCompilation = false;
                CreateGeneratedHarnessSceneIfNeeded(testName);
                string scenePath = ResolveScenePath(testName);
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

                // Touch every renderer's shaders to force a compile.
                foreach (var r in UnityEngine.Object.FindObjectsOfType<Renderer>())
                {
                    foreach (var m in r.sharedMaterials)
                    {
                        if (m == null || m.shader == null) continue;
                        if (ShaderUtil.ShaderHasError(m.shader))
                            throw new Exception($"Shader has errors: {m.shader.name}");
                    }
                }

                Log("[Harness] CompileCheck OK");
                exitCode = 0;
            }
            catch (Exception e)
            {
                Log($"[Harness] CompileCheck FAILED: {e.Message}");
                exitCode = 2;
            }
            finally
            {
                EditorApplication.Exit(exitCode);
            }
        }

        // ----- Internals ----------------------------------------------------

        private static string ResolveScenePath(string testName)
        {
            // Convention: Samples~/Harness/<testName>.unity
            // (Note: Samples~ is hidden from Unity import; for harness scenes
            //  we mirror them under Assets/DToon/Samples/Harness/.)
            return $"Assets/DToon/Samples/Harness/{testName}.unity";
        }

        private static string ResolveHarnessOutputPath(string testName)
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            return Path.Combine(projectRoot, "HarnessOutput", testName + ".png");
        }

        private static void CreateGeneratedHarnessSceneIfNeeded(string testName)
        {
            if (testName == Step1RampLitFrontlitTestName)
            {
                CreateStep1RampLitFrontlitScene(ResolveScenePath(testName));
                return;
            }

            if (testName == Step2AlphaClipHairTestName)
            {
                CreateStep2AlphaClipHairScene(ResolveScenePath(testName));
                return;
            }

            if (testName == Step2ReceiveShadowOnOffTestName)
            {
                CreateStep2ReceiveShadowOnOffScene(ResolveScenePath(testName));
                return;
            }

            if (testName == Step3OutlineCloseupTestName)
            {
                CreateStep3OutlineCloseupScene(ResolveScenePath(testName));
                return;
            }

            if (testName == Step3OutlineHairTestName)
            {
                CreateStep3OutlineHairScene(ResolveScenePath(testName));
                return;
            }

            if (testName == Step4RimCloseupTestName)
            {
                CreateStep4RimCloseupScene(ResolveScenePath(testName));
                return;
            }

            if (testName == Step4RimCloseupNoOutlineTestName)
            {
                CreateStep4RimCloseupNoOutlineScene(ResolveScenePath(testName));
                return;
            }

            if (testName == Step4MatcapCloseupTestName)
            {
                CreateStep4MatcapCloseupScene(ResolveScenePath(testName));
                return;
            }

            if (testName == Step4SpecularCloseupTestName)
            {
                CreateStep4SpecularCloseupScene(ResolveScenePath(testName));
            }
        }

        private static void CreateStep1RampLitFrontlitScene(string scenePath)
        {
            EnsureAssetFolder("Assets", "DToon");
            EnsureAssetFolder("Assets/DToon", "Samples");
            EnsureAssetFolder("Assets/DToon/Samples", "Harness");
            EnsureAssetFolder("Assets/DToon/Samples/Harness", "_Common");
            EnsureAssetFolder("Assets/DToon/Samples/Harness/_Common", "Materials");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.1f, 0.1f, 0.12f, 1.0f);
            QualitySettings.shadowDistance = Mathf.Max(QualitySettings.shadowDistance, 20.0f);

            GameObject cameraObject = new GameObject("HarnessCamera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0.0f, 1.0f, -3.0f);
            cameraObject.transform.rotation = Quaternion.Euler(5.0f, 0.0f, 0.0f);

            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 30.0f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.18f, 0.18f, 0.20f, 1.0f);
            camera.allowHDR = false;
            camera.allowMSAA = false;

            UniversalAdditionalCameraData cameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
            cameraData.renderPostProcessing = false;
            cameraData.antialiasing = AntialiasingMode.None;

            GameObject lightObject = new GameObject("MainLight");
            lightObject.transform.rotation = Quaternion.Euler(30.0f, 60.0f, 0.0f);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.0f;
            light.color = Color.white;
            light.shadows = LightShadows.Soft;
            light.bounceIntensity = 0.0f;

            GameObject outlineHull = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            outlineHull.name = "TestSphere_OutlineHull";
            outlineHull.transform.position = new Vector3(0.0f, 1.0f, 0.0f);
            outlineHull.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
            outlineHull.GetComponent<Renderer>().sharedMaterial = CreateOrUpdateDToonOutlineHullMaterial();

            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "TestSphere";
            sphere.transform.position = new Vector3(0.0f, 1.0f, 0.0f);
            sphere.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
            sphere.GetComponent<Renderer>().sharedMaterial = CreateOrUpdateDToonTestMaterial();

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "GroundPlane";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = Vector3.one;
            ground.GetComponent<Renderer>().sharedMaterial = CreateOrUpdateGroundMaterial();

            EditorSceneManager.SaveScene(scene, scenePath);
            Log($"[Harness] Generated scene: {scenePath}");
        }

        private static void CreateStep3OutlineCloseupScene(string scenePath)
        {
            EnsureHarnessFolders();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            ConfigureHarnessRenderSettings();
            EnsureDToonOutlineRendererFeature();

            CreateHarnessCamera(new Vector3(0.0f, 1.0f, -3.0f), Quaternion.Euler(5.0f, 0.0f, 0.0f), 30.0f);
            CreateMainLight(Quaternion.Euler(30.0f, 60.0f, 0.0f));

            GameObject outlineHull = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            outlineHull.name = "TestSphere_OutlineHull";
            outlineHull.transform.position = new Vector3(0.0f, 1.0f, 0.0f);
            outlineHull.transform.localScale = Vector3.one;
            outlineHull.GetComponent<Renderer>().sharedMaterial = CreateOrUpdateDToonOutlineCloseupHullMaterial();

            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "TestSphere";
            sphere.transform.position = new Vector3(0.0f, 1.0f, 0.0f);
            sphere.transform.localScale = Vector3.one;
            sphere.GetComponent<Renderer>().sharedMaterial = CreateOrUpdateDToonOutlineMaterial();

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "GroundPlane";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = Vector3.one;
            ground.GetComponent<Renderer>().sharedMaterial = CreateOrUpdateGroundMaterial();

            EditorSceneManager.SaveScene(scene, scenePath);
            Log($"[Harness] Generated scene: {scenePath}");
        }

        private static void CreateStep3OutlineHairScene(string scenePath)
        {
            EnsureHarnessFolders();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            ConfigureHarnessRenderSettings();
            EnsureDToonOutlineRendererFeature();

            Camera camera = CreateHarnessCamera(new Vector3(0.0f, 1.0f, -1.5f), Quaternion.identity, 30.0f);
            camera.backgroundColor = new Color(0.42f, 0.42f, 0.44f, 1.0f);
            CreateMainLight(Quaternion.Euler(30.0f, 60.0f, 0.0f));

            GameObject outlineHull = GameObject.CreatePrimitive(PrimitiveType.Quad);
            outlineHull.name = "HairTestQuad_OutlineHull";
            outlineHull.transform.position = new Vector3(0.0f, 1.0f, 0.0f);
            outlineHull.transform.rotation = Quaternion.identity;
            outlineHull.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
            outlineHull.GetComponent<Renderer>().sharedMaterial = CreateOrUpdateDToonHairOutlineHullMaterial();

            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "HairTestQuad";
            quad.transform.position = new Vector3(0.0f, 1.0f, 0.0f);
            quad.transform.rotation = Quaternion.identity;
            quad.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
            quad.GetComponent<Renderer>().sharedMaterial = CreateOrUpdateDToonHairOutlinedMaterial();

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "GroundPlane";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = Vector3.one;
            ground.GetComponent<Renderer>().sharedMaterial = CreateOrUpdateGroundMaterial();

            EditorSceneManager.SaveScene(scene, scenePath);
            Log($"[Harness] Generated scene: {scenePath}");
        }

        private static void CreateStep4RimCloseupScene(string scenePath)
        {
            EnsureHarnessFolders();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            ConfigureHarnessRenderSettings();
            EnsureDToonOutlineRendererFeature();

            CreateHarnessCamera(new Vector3(0.0f, 1.0f, -2.0f), Quaternion.identity, 35.0f);
            CreateMainLight(Quaternion.Euler(30.0f, 120.0f, 0.0f));

            GameObject outlineHull = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            outlineHull.name = "TestSphere_OutlineHull";
            outlineHull.transform.position = new Vector3(0.0f, 1.0f, 0.0f);
            outlineHull.transform.localScale = Vector3.one;
            outlineHull.GetComponent<Renderer>().sharedMaterial = CreateOrUpdateDToonRimOutlineHullMaterial();

            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "TestSphere";
            sphere.transform.position = new Vector3(0.0f, 1.0f, 0.0f);
            sphere.transform.localScale = Vector3.one;
            sphere.GetComponent<Renderer>().sharedMaterial = CreateOrUpdateDToonRimMaterial();

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "GroundPlane";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = Vector3.one;
            ground.GetComponent<Renderer>().sharedMaterial = CreateOrUpdateGroundMaterial();

            EditorSceneManager.SaveScene(scene, scenePath);
            Log($"[Harness] Generated scene: {scenePath}");
        }

        private static void CreateStep4RimCloseupNoOutlineScene(string scenePath)
        {
            EnsureHarnessFolders();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            ConfigureHarnessRenderSettings();

            CreateHarnessCamera(new Vector3(0.0f, 1.0f, -2.0f), Quaternion.identity, 35.0f);
            CreateMainLight(Quaternion.Euler(30.0f, 120.0f, 0.0f));

            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "TestSphere";
            sphere.transform.position = new Vector3(0.0f, 1.0f, 0.0f);
            sphere.transform.localScale = Vector3.one;
            sphere.GetComponent<Renderer>().sharedMaterial = CreateOrUpdateDToonRimNoOutlineMaterial();

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "GroundPlane";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = Vector3.one;
            ground.GetComponent<Renderer>().sharedMaterial = CreateOrUpdateGroundMaterial();

            EditorSceneManager.SaveScene(scene, scenePath);
            Log($"[Harness] Generated scene: {scenePath}");
        }

        private static void CreateStep4MatcapCloseupScene(string scenePath)
        {
            EnsureHarnessFolders();
            MatcapGenerator.GenerateDefaultMatcapTextures();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            ConfigureHarnessRenderSettings();
            EnsureDToonOutlineRendererFeature();

            // The requested -3.5 camera distance crops a four-sphere row at
            // square 1024 output and FOV 35, so this keeps the same angle while
            // honoring the "see all 4 spheres" verification intent.
            CreateHarnessCamera(new Vector3(0.0f, 1.5f, -16.0f), Quaternion.Euler(10.0f, 0.0f, 0.0f), 35.0f);
            CreateMainLight(Quaternion.Euler(30.0f, 60.0f, 0.0f));

            CreateMatcapSpherePair(
                "Sphere_Eye",
                new Vector3(-3.75f, 1.0f, 0.0f),
                CreateOrUpdateDToonMatcapEyeMaterial(),
                CreateOrUpdateDToonMatcapOutlineHullMaterial(DToonMatcapEyeMaterialPath, DToonMatcapEyeOutlineHullMaterialPath)
            );

            CreateMatcapSpherePair(
                "Sphere_Metal",
                new Vector3(-1.25f, 1.0f, 0.0f),
                CreateOrUpdateDToonMatcapMetalMaterial(),
                CreateOrUpdateDToonMatcapOutlineHullMaterial(DToonMatcapMetalMaterialPath, DToonMatcapMetalOutlineHullMaterialPath)
            );

            CreateMatcapSpherePair(
                "Sphere_Skin",
                new Vector3(1.25f, 1.0f, 0.0f),
                CreateOrUpdateDToonMatcapSkinMaterial(),
                CreateOrUpdateDToonMatcapOutlineHullMaterial(DToonMatcapSkinMaterialPath, DToonMatcapSkinOutlineHullMaterialPath)
            );

            CreateMatcapSpherePair(
                "Sphere_Cloth",
                new Vector3(3.75f, 1.0f, 0.0f),
                CreateOrUpdateDToonMatcapClothMaterial(),
                CreateOrUpdateDToonMatcapOutlineHullMaterial(DToonMatcapClothMaterialPath, DToonMatcapClothOutlineHullMaterialPath)
            );

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "GroundPlane";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = Vector3.one;
            ground.GetComponent<Renderer>().sharedMaterial = CreateOrUpdateGroundMaterial();

            EditorSceneManager.SaveScene(scene, scenePath);
            Log($"[Harness] Generated scene: {scenePath}");
        }

        private static void CreateMatcapSpherePair(string name, Vector3 position, Material material, Material outlineMaterial)
        {
            GameObject outlineHull = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            outlineHull.name = name + "_OutlineHull";
            outlineHull.transform.position = position;
            outlineHull.transform.localScale = Vector3.one;
            outlineHull.GetComponent<Renderer>().sharedMaterial = outlineMaterial;

            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = name;
            sphere.transform.position = position;
            sphere.transform.localScale = Vector3.one;
            sphere.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static void CreateStep4SpecularCloseupScene(string scenePath)
        {
            EnsureHarnessFolders();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            ConfigureHarnessRenderSettings();
            EnsureDToonOutlineRendererFeature();

            // Match the four-sphere comparison framing from Step4_Matcap_Closeup.
            CreateHarnessCamera(new Vector3(0.0f, 1.5f, -16.0f), Quaternion.Euler(10.0f, 0.0f, 0.0f), 35.0f);
            CreateMainLight(Quaternion.Euler(30.0f, 60.0f, 0.0f));

            CreateMatcapSpherePair(
                "Sphere_Sharp_Metal",
                new Vector3(-3.75f, 1.0f, 0.0f),
                CreateOrUpdateDToonSpecularMetalMaterial(),
                CreateOrUpdateDToonMatcapOutlineHullMaterial(DToonSpecularMetalMaterialPath, DToonSpecularMetalOutlineHullMaterialPath)
            );

            CreateMatcapSpherePair(
                "Sphere_Soft_Skin",
                new Vector3(-1.25f, 1.0f, 0.0f),
                CreateOrUpdateDToonSpecularSkinMaterial(),
                CreateOrUpdateDToonMatcapOutlineHullMaterial(DToonSpecularSkinMaterialPath, DToonSpecularSkinOutlineHullMaterialPath)
            );

            CreateMatcapSpherePair(
                "Sphere_Plastic",
                new Vector3(1.25f, 1.0f, 0.0f),
                CreateOrUpdateDToonSpecularPlasticMaterial(),
                CreateOrUpdateDToonMatcapOutlineHullMaterial(DToonSpecularPlasticMaterialPath, DToonSpecularPlasticOutlineHullMaterialPath)
            );

            CreateMatcapSpherePair(
                "Sphere_Hair_Preview",
                new Vector3(3.75f, 1.0f, 0.0f),
                CreateOrUpdateDToonSpecularHairMaterial(),
                CreateOrUpdateDToonMatcapOutlineHullMaterial(DToonSpecularHairMaterialPath, DToonSpecularHairOutlineHullMaterialPath)
            );

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "GroundPlane";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = Vector3.one;
            ground.GetComponent<Renderer>().sharedMaterial = CreateOrUpdateGroundMaterial();

            EditorSceneManager.SaveScene(scene, scenePath);
            Log($"[Harness] Generated scene: {scenePath}");
        }

        private static void CreateStep2AlphaClipHairScene(string scenePath)
        {
            EnsureHarnessFolders();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            ConfigureHarnessRenderSettings();

            CreateHarnessCamera(new Vector3(0.0f, 1.0f, -3.0f), Quaternion.Euler(5.0f, 0.0f, 0.0f), 30.0f);
            CreateMainLight(Quaternion.Euler(50.0f, 60.0f, 0.0f));

            GameObject outlineHull = GameObject.CreatePrimitive(PrimitiveType.Quad);
            outlineHull.name = "HairTestQuad_OutlineHull";
            outlineHull.transform.position = new Vector3(0.0f, 1.0f, 0.0f);
            outlineHull.transform.rotation = Quaternion.identity;
            outlineHull.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
            outlineHull.GetComponent<Renderer>().sharedMaterial = CreateOrUpdateDToonHairOutlineHullMaterial();

            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "HairTestQuad";
            quad.transform.position = new Vector3(0.0f, 1.0f, 0.0f);
            quad.transform.rotation = Quaternion.identity;
            quad.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
            quad.GetComponent<Renderer>().sharedMaterial = CreateOrUpdateDToonHairMaterial();

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "GroundPlane";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = Vector3.one;
            ground.GetComponent<Renderer>().sharedMaterial = CreateOrUpdateGroundMaterial();

            EditorSceneManager.SaveScene(scene, scenePath);
            Log($"[Harness] Generated scene: {scenePath}");
        }

        private static void CreateStep2ReceiveShadowOnOffScene(string scenePath)
        {
            EnsureHarnessFolders();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            ConfigureHarnessRenderSettings();

            CreateHarnessCamera(new Vector3(0.0f, 1.35f, -7.0f), Quaternion.Euler(6.0f, 0.0f, 0.0f), 38.0f);
            Light mainLight = CreateMainLight(Quaternion.Euler(50.0f, 30.0f, 0.0f));

            GameObject leftSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            leftSphere.name = "Sphere_ReceiveOn";
            leftSphere.transform.position = new Vector3(-1.2f, 1.0f, 0.0f);
            leftSphere.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
            leftSphere.GetComponent<Renderer>().sharedMaterial = CreateOrUpdateDToonReceiveShadowMaterial(
                "Assets/DToon/Samples/Harness/_Common/Materials/M_DToon_Test_ReceiveOn.mat",
                1.0f
            );

            GameObject rightSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rightSphere.name = "Sphere_ReceiveOff";
            rightSphere.transform.position = new Vector3(1.2f, 1.0f, 0.0f);
            rightSphere.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
            rightSphere.GetComponent<Renderer>().sharedMaterial = CreateOrUpdateDToonReceiveShadowMaterial(
                "Assets/DToon/Samples/Harness/_Common/Materials/M_DToon_Test_ReceiveOff.mat",
                0.0f
            );

            GameObject shadowCaster = GameObject.CreatePrimitive(PrimitiveType.Quad);
            shadowCaster.name = "ShadowCaster_Ceiling";
            shadowCaster.transform.position = new Vector3(0.0f, 3.5f, 0.0f);
            shadowCaster.transform.rotation = Quaternion.Euler(90.0f, 0.0f, 0.0f);
            shadowCaster.transform.localScale = new Vector3(10.0f, 10.0f, 1.0f);
            Renderer shadowCasterRenderer = shadowCaster.GetComponent<Renderer>();
            shadowCasterRenderer.sharedMaterial = CreateOrUpdateGroundMaterial();
            shadowCasterRenderer.shadowCastingMode = ShadowCastingMode.TwoSided;

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "GroundPlane";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = Vector3.one;
            ground.GetComponent<Renderer>().sharedMaterial = CreateOrUpdateGroundMaterial();

            EditorSceneManager.SaveScene(scene, scenePath);
            Log($"[Harness] Generated scene: {scenePath}");
        }

        private static Material CreateOrUpdateDToonTestMaterial()
        {
            const string rampPath = RampTextureGenerator.GeneratedRampOutputFolder + "/Ramp_Generic_Cool.png";

            Shader shader = Shader.Find("DToon/Character");
            if (shader == null)
            {
                throw new Exception("Shader not found: DToon/Character");
            }

            Texture2D rampTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(rampPath);
            if (rampTexture == null)
            {
                RampTextureGenerator.GenerateDefaultRampTextures();
                rampTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(rampPath);
            }

            if (rampTexture == null)
            {
                throw new Exception("Ramp texture not found: " + rampPath);
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(DToonTestMaterialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, DToonTestMaterialPath);
            }

            material.shader = shader;
            material.SetColor("_BaseColor", new Color(0.85f, 0.55f, 0.55f, 1.0f));
            material.SetTexture("_RampMap", rampTexture);
            material.SetColor("_ShadowTint", Color.white);
            material.SetFloat("_ReceiveShadowsStrength", 0.0f);
            SetOutlineProperties(material, true);
            material.SetFloat("_OutlineWidth", 0.005f);
            material.SetFloat("_OutlineDarkening", 0.3f);
            SetRimProperties(material, false);
            SetMatcapProperties(material, false, null, Color.white, 1.0f, 0.0f);
            SetSpecularProperties(material, false);
            material.SetShaderPassEnabled("ForwardLit", true);
            material.SetShaderPassEnabled("Outline", false);
            material.SetShaderPassEnabled("ShadowCaster", true);
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            return material;
        }

        private static Material CreateOrUpdateGroundMaterial()
        {
            const string materialPath = "Assets/DToon/Samples/Harness/_Common/Materials/M_URP_Lit_Ground.mat";

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            if (shader == null)
            {
                throw new Exception("Ground shader not found.");
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, materialPath);
            }

            material.shader = shader;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", new Color(0.5f, 0.5f, 0.5f, 1.0f));
            }
            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", new Color(0.5f, 0.5f, 0.5f, 1.0f));
            }

            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            return material;
        }

        private static Material CreateOrUpdateDToonHairMaterial()
        {
            const string rampPath = RampTextureGenerator.GeneratedRampOutputFolder + "/Ramp_Hair_Default.png";

            Shader shader = Shader.Find("DToon/Character");
            if (shader == null)
            {
                throw new Exception("Shader not found: DToon/Character");
            }

            Texture2D hairTexture = CreateOrUpdateHairTestTexture();
            Texture2D rampTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(rampPath);
            if (rampTexture == null)
            {
                RampTextureGenerator.GenerateDefaultRampTextures();
                rampTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(rampPath);
            }

            if (rampTexture == null)
            {
                throw new Exception("Ramp texture not found: " + rampPath);
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(DToonHairMaterialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, DToonHairMaterialPath);
            }

            material.shader = shader;
            material.SetTexture("_BaseMap", hairTexture);
            material.SetColor("_BaseColor", new Color(0.4f, 0.3f, 0.25f, 1.0f));
            material.SetTexture("_RampMap", rampTexture);
            material.SetColor("_ShadowTint", Color.white);
            material.SetFloat("_ReceiveShadowsStrength", 1.0f);
            material.SetFloat("_AlphaClip", 1.0f);
            material.SetFloat("_Cutoff", 0.5f);
            SetOutlineProperties(material, true);
            material.SetFloat("_OutlineWidth", 0.003f);
            material.SetFloat("_OutlineDarkening", 0.3f);
            SetRimProperties(material, false);
            SetMatcapProperties(material, false, null, Color.white, 1.0f, 0.0f);
            SetSpecularProperties(material, false);
            material.SetShaderPassEnabled("ForwardLit", true);
            material.SetShaderPassEnabled("Outline", false);
            material.SetShaderPassEnabled("ShadowCaster", true);
            material.EnableKeyword("_ALPHACLIP");
            material.EnableKeyword("_ALPHATEST_ON");
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            return material;
        }

        private static Material CreateOrUpdateDToonHairOutlinedMaterial()
        {
            const string rampPath = RampTextureGenerator.GeneratedRampOutputFolder + "/Ramp_Hair_Default.png";

            Shader shader = Shader.Find("DToon/Character");
            if (shader == null)
            {
                throw new Exception("Shader not found: DToon/Character");
            }

            Texture2D hairTexture = CreateOrUpdateHairTestTexture();
            Texture2D rampTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(rampPath);
            if (rampTexture == null)
            {
                RampTextureGenerator.GenerateDefaultRampTextures();
                rampTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(rampPath);
            }

            if (rampTexture == null)
            {
                throw new Exception("Ramp texture not found: " + rampPath);
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(DToonHairOutlinedMaterialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, DToonHairOutlinedMaterialPath);
            }

            material.shader = shader;
            material.SetTexture("_BaseMap", hairTexture);
            material.SetColor("_BaseColor", new Color(0.4f, 0.3f, 0.25f, 1.0f));
            material.SetTexture("_RampMap", rampTexture);
            material.SetFloat("_RampOffset", 0.0f);
            material.SetColor("_ShadowTint", Color.white);
            material.SetFloat("_ReceiveShadowsStrength", 1.0f);
            material.SetFloat("_AlphaClip", 1.0f);
            material.SetFloat("_Cutoff", 0.5f);
            material.EnableKeyword("_ALPHACLIP");
            material.EnableKeyword("_ALPHATEST_ON");
            SetOutlineProperties(material, true);
            material.SetFloat("_OutlineWidth", 0.003f);
            material.SetFloat("_OutlineDarkening", 0.3f);
            material.SetFloat("_OutlineDistanceScale", 1.0f);
            material.SetFloat("_OutlineMaxWidth", 0.05f);
            SetRimProperties(material, false);
            SetMatcapProperties(material, false, null, Color.white, 1.0f, 0.0f);
            SetSpecularProperties(material, false);
            material.SetShaderPassEnabled("ForwardLit", true);
            material.SetShaderPassEnabled("Outline", false);
            material.SetShaderPassEnabled("ShadowCaster", true);
            material.renderQueue = -1;
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            return material;
        }

        private static Material CreateOrUpdateDToonHairOutlineHullMaterial()
        {
            Shader shader = Shader.Find("DToon/Outline");
            if (shader == null)
            {
                throw new Exception("Shader not found: DToon/Outline");
            }

            Material sourceMaterial = CreateOrUpdateDToonHairOutlinedMaterial();
            Material material = AssetDatabase.LoadAssetAtPath<Material>(DToonHairOutlineHullMaterialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, DToonHairOutlineHullMaterialPath);
            }

            OutlinePairCreator.ApplyOutlinePairSettings(sourceMaterial, material);
            material.SetFloat("_OutlineWidth", 0.003f);
            material.SetFloat("_OutlineAlphaClip", 1.0f);
            material.SetFloat("_OutlineCull", 0.0f);
            material.SetFloat("_OutlineZTest", 8.0f);
            material.EnableKeyword("_OUTLINE_ALPHACLIP");
            material.renderQueue = 2001;
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            return material;
        }

        private static Material CreateOrUpdateDToonRimMaterial()
        {
            const string rampPath = RampTextureGenerator.GeneratedRampOutputFolder + "/Ramp_Generic_Cool.png";

            Shader shader = Shader.Find("DToon/Character");
            if (shader == null)
            {
                throw new Exception("Shader not found: DToon/Character");
            }

            Texture2D rampTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(rampPath);
            if (rampTexture == null)
            {
                RampTextureGenerator.GenerateDefaultRampTextures();
                rampTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(rampPath);
            }

            if (rampTexture == null)
            {
                throw new Exception("Ramp texture not found: " + rampPath);
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(DToonRimMaterialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, DToonRimMaterialPath);
            }

            material.shader = shader;
            material.SetColor("_BaseColor", new Color(0.85f, 0.55f, 0.55f, 1.0f));
            material.SetTexture("_RampMap", rampTexture);
            material.SetFloat("_RampOffset", 0.0f);
            material.SetColor("_ShadowTint", Color.white);
            material.SetFloat("_ReceiveShadowsStrength", 0.0f);
            material.SetFloat("_AlphaClip", 0.0f);
            material.SetFloat("_Cutoff", 0.5f);
            material.DisableKeyword("_ALPHACLIP");
            material.DisableKeyword("_ALPHATEST_ON");
            SetOutlineProperties(material, true);
            material.SetFloat("_OutlineWidth", 0.005f);
            material.SetFloat("_OutlineDarkening", 0.3f);
            SetRimProperties(
                material,
                true,
                false,
                new Color(0.5f, 0.8f, 1.0f, 1.0f),
                4.0f,
                0.7f,
                0.05f
            );
            SetMatcapProperties(material, false, null, Color.white, 1.0f, 0.0f);
            SetSpecularProperties(material, false);
            material.SetShaderPassEnabled("ForwardLit", true);
            material.SetShaderPassEnabled("Outline", false);
            material.SetShaderPassEnabled("ShadowCaster", true);
            material.renderQueue = -1;
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            return material;
        }

        private static Material CreateOrUpdateDToonRimNoOutlineMaterial()
        {
            Material material = CreateOrUpdateDToonRimMaterial();
            Material diagnosticMaterial = AssetDatabase.LoadAssetAtPath<Material>(DToonRimNoOutlineMaterialPath);
            if (diagnosticMaterial == null)
            {
                diagnosticMaterial = new Material(material);
                AssetDatabase.CreateAsset(diagnosticMaterial, DToonRimNoOutlineMaterialPath);
            }

            diagnosticMaterial.CopyPropertiesFromMaterial(material);
            diagnosticMaterial.shader = material.shader;
            SetOutlineProperties(diagnosticMaterial, false);
            diagnosticMaterial.SetShaderPassEnabled("ForwardLit", true);
            diagnosticMaterial.SetShaderPassEnabled("Outline", false);
            diagnosticMaterial.SetShaderPassEnabled("ShadowCaster", true);
            diagnosticMaterial.renderQueue = -1;
            EditorUtility.SetDirty(diagnosticMaterial);
            AssetDatabase.SaveAssets();
            return diagnosticMaterial;
        }

        private static Material CreateOrUpdateDToonRimOutlineHullMaterial()
        {
            Shader shader = Shader.Find("DToon/Outline");
            if (shader == null)
            {
                throw new Exception("Shader not found: DToon/Outline");
            }

            Material sourceMaterial = CreateOrUpdateDToonRimMaterial();
            Material material = AssetDatabase.LoadAssetAtPath<Material>(DToonRimOutlineHullMaterialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, DToonRimOutlineHullMaterialPath);
            }

            OutlinePairCreator.ApplyOutlinePairSettings(sourceMaterial, material);
            material.SetFloat("_OutlineWidth", 0.005f);
            material.SetFloat("_OutlineDarkening", 0.3f);
            material.SetFloat("_OutlineAlphaClip", 0.0f);
            material.SetFloat("_OutlineCull", 1.0f);
            material.SetFloat("_OutlineZTest", 4.0f);
            material.DisableKeyword("_OUTLINE_ALPHACLIP");
            material.renderQueue = 2001;
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            return material;
        }

        private static Material CreateOrUpdateDToonMatcapEyeMaterial()
        {
            return CreateOrUpdateDToonMatcapMaterial(
                DToonMatcapEyeMaterialPath,
                "Matcap_Eye_Glossy",
                RampTextureGenerator.GeneratedRampOutputFolder + "/Ramp_Generic_Cool.png",
                new Color(0.05f, 0.05f, 0.1f, 1.0f),
                0.0f,
                1.5f
            );
        }

        private static Material CreateOrUpdateDToonMatcapMetalMaterial()
        {
            return CreateOrUpdateDToonMatcapMaterial(
                DToonMatcapMetalMaterialPath,
                "Matcap_Metal_Chrome",
                RampTextureGenerator.GeneratedRampOutputFolder + "/Ramp_Generic_Cool.png",
                new Color(0.7f, 0.7f, 0.75f, 1.0f),
                1.0f,
                1.0f
            );
        }

        private static Material CreateOrUpdateDToonMatcapSkinMaterial()
        {
            return CreateOrUpdateDToonMatcapMaterial(
                DToonMatcapSkinMaterialPath,
                "Matcap_Skin_Soft",
                RampTextureGenerator.GeneratedRampOutputFolder + "/Ramp_Skin_Default.png",
                new Color(0.85f, 0.65f, 0.55f, 1.0f),
                2.0f,
                0.4f
            );
        }

        private static Material CreateOrUpdateDToonMatcapClothMaterial()
        {
            return CreateOrUpdateDToonMatcapMaterial(
                DToonMatcapClothMaterialPath,
                "Matcap_Cloth_Velvet",
                RampTextureGenerator.GeneratedRampOutputFolder + "/Ramp_Generic_Cool.png",
                new Color(0.15f, 0.1f, 0.2f, 1.0f),
                0.0f,
                1.0f
            );
        }

        private static Material CreateOrUpdateDToonMatcapMaterial(
            string materialPath,
            string matcapName,
            string rampPath,
            Color baseColor,
            float matcapMode,
            float matcapIntensity
        )
        {
            Shader shader = Shader.Find("DToon/Character");
            if (shader == null)
            {
                throw new Exception("Shader not found: DToon/Character");
            }

            Texture2D rampTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(rampPath);
            if (rampTexture == null)
            {
                RampTextureGenerator.GenerateDefaultRampTextures();
                rampTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(rampPath);
            }

            if (rampTexture == null)
            {
                throw new Exception("Ramp texture not found: " + rampPath);
            }

            string matcapPath = MatcapGenerator.GeneratedMatcapOutputFolder + "/" + matcapName + ".png";
            Texture2D matcapTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(matcapPath);
            if (matcapTexture == null)
            {
                MatcapGenerator.GenerateDefaultMatcapTextures();
                matcapTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(matcapPath);
            }

            if (matcapTexture == null)
            {
                throw new Exception("Matcap texture not found: " + matcapPath);
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, materialPath);
            }

            material.shader = shader;
            material.SetColor("_BaseColor", baseColor);
            material.SetTexture("_RampMap", rampTexture);
            material.SetFloat("_RampOffset", 0.0f);
            material.SetColor("_ShadowTint", Color.white);
            material.SetFloat("_ReceiveShadowsStrength", 0.0f);
            material.SetFloat("_AlphaClip", 0.0f);
            material.SetFloat("_Cutoff", 0.5f);
            material.DisableKeyword("_ALPHACLIP");
            material.DisableKeyword("_ALPHATEST_ON");
            SetOutlineProperties(material, true);
            material.SetFloat("_OutlineWidth", 0.005f);
            material.SetFloat("_OutlineDarkening", 0.3f);
            SetRimProperties(material, false);
            SetMatcapProperties(material, true, matcapTexture, Color.white, matcapIntensity, matcapMode);
            SetSpecularProperties(material, false);
            material.SetShaderPassEnabled("ForwardLit", true);
            material.SetShaderPassEnabled("Outline", false);
            material.SetShaderPassEnabled("ShadowCaster", true);
            material.renderQueue = -1;
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            return material;
        }

        private static Material CreateOrUpdateDToonMatcapOutlineHullMaterial(string sourceMaterialPath, string outlineMaterialPath)
        {
            Shader shader = Shader.Find("DToon/Outline");
            if (shader == null)
            {
                throw new Exception("Shader not found: DToon/Outline");
            }

            Material sourceMaterial = AssetDatabase.LoadAssetAtPath<Material>(sourceMaterialPath);
            if (sourceMaterial == null)
            {
                throw new Exception("Matcap source material not found: " + sourceMaterialPath);
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(outlineMaterialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, outlineMaterialPath);
            }

            OutlinePairCreator.ApplyOutlinePairSettings(sourceMaterial, material);
            material.SetFloat("_OutlineWidth", 0.005f);
            material.SetFloat("_OutlineDarkening", 0.3f);
            material.SetFloat("_OutlineAlphaClip", 0.0f);
            material.SetFloat("_OutlineCull", 1.0f);
            material.SetFloat("_OutlineZTest", 4.0f);
            material.DisableKeyword("_OUTLINE_ALPHACLIP");
            material.renderQueue = 2001;
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            return material;
        }

        private static Material CreateOrUpdateDToonSpecularMetalMaterial()
        {
            return CreateOrUpdateDToonSpecularMaterial(
                DToonSpecularMetalMaterialPath,
                RampTextureGenerator.GeneratedRampOutputFolder + "/Ramp_Generic_Cool.png",
                new Color(0.7f, 0.7f, 0.75f, 1.0f),
                Color.white,
                3.0f,
                128.0f,
                0.3f,
                0.05f
            );
        }

        private static Material CreateOrUpdateDToonSpecularSkinMaterial()
        {
            return CreateOrUpdateDToonSpecularMaterial(
                DToonSpecularSkinMaterialPath,
                RampTextureGenerator.GeneratedRampOutputFolder + "/Ramp_Skin_Default.png",
                new Color(0.85f, 0.65f, 0.55f, 1.0f),
                new Color(1.0f, 0.95f, 0.9f, 1.0f),
                1.2f,
                16.0f,
                0.5f,
                0.1f
            );
        }

        private static Material CreateOrUpdateDToonSpecularPlasticMaterial()
        {
            return CreateOrUpdateDToonSpecularMaterial(
                DToonSpecularPlasticMaterialPath,
                RampTextureGenerator.GeneratedRampOutputFolder + "/Ramp_Generic_Cool.png",
                new Color(0.8f, 0.2f, 0.3f, 1.0f),
                Color.white,
                2.4f,
                64.0f,
                0.4f,
                0.05f
            );
        }

        private static Material CreateOrUpdateDToonSpecularHairMaterial()
        {
            return CreateOrUpdateDToonSpecularMaterial(
                DToonSpecularHairMaterialPath,
                RampTextureGenerator.GeneratedRampOutputFolder + "/Ramp_Hair_Default.png",
                new Color(0.4f, 0.3f, 0.25f, 1.0f),
                new Color(1.0f, 0.85f, 0.7f, 1.0f),
                2.0f,
                48.0f,
                0.45f,
                0.08f
            );
        }

        private static Material CreateOrUpdateDToonSpecularMaterial(
            string materialPath,
            string rampPath,
            Color baseColor,
            Color specularColor,
            float specularIntensity,
            float specularPower,
            float specularThreshold,
            float specularSoftness
        )
        {
            Shader shader = Shader.Find("DToon/Character");
            if (shader == null)
            {
                throw new Exception("Shader not found: DToon/Character");
            }

            Texture2D rampTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(rampPath);
            if (rampTexture == null)
            {
                RampTextureGenerator.GenerateDefaultRampTextures();
                rampTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(rampPath);
            }

            if (rampTexture == null)
            {
                throw new Exception("Ramp texture not found: " + rampPath);
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, materialPath);
            }

            material.shader = shader;
            material.SetColor("_BaseColor", baseColor);
            material.SetTexture("_RampMap", rampTexture);
            material.SetFloat("_RampOffset", 0.0f);
            material.SetColor("_ShadowTint", Color.white);
            material.SetFloat("_ReceiveShadowsStrength", 0.0f);
            material.SetFloat("_AlphaClip", 0.0f);
            material.SetFloat("_Cutoff", 0.5f);
            material.DisableKeyword("_ALPHACLIP");
            material.DisableKeyword("_ALPHATEST_ON");
            SetOutlineProperties(material, true);
            material.SetFloat("_OutlineWidth", 0.005f);
            material.SetFloat("_OutlineDarkening", 0.3f);
            SetRimProperties(material, false);
            SetMatcapProperties(material, false, null, Color.white, 1.0f, 0.0f);
            SetSpecularProperties(
                material,
                true,
                specularColor,
                specularIntensity,
                specularPower,
                specularThreshold,
                specularSoftness
            );
            material.SetShaderPassEnabled("ForwardLit", true);
            material.SetShaderPassEnabled("Outline", false);
            material.SetShaderPassEnabled("ShadowCaster", true);
            material.renderQueue = -1;
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            return material;
        }

        private static Material CreateOrUpdateDToonReceiveShadowMaterial(string materialPath, float receiveShadowStrength)
        {
            const string rampPath = RampTextureGenerator.GeneratedRampOutputFolder + "/Ramp_Generic_Cool.png";

            Shader shader = Shader.Find("DToon/Character");
            if (shader == null)
            {
                throw new Exception("Shader not found: DToon/Character");
            }

            Texture2D rampTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(rampPath);
            if (rampTexture == null)
            {
                RampTextureGenerator.GenerateDefaultRampTextures();
                rampTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(rampPath);
            }

            if (rampTexture == null)
            {
                throw new Exception("Ramp texture not found: " + rampPath);
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, materialPath);
            }

            material.shader = shader;
            material.SetColor("_BaseColor", new Color(0.85f, 0.55f, 0.55f, 1.0f));
            material.SetTexture("_RampMap", rampTexture);
            material.SetFloat("_RampOffset", 0.0f);
            material.SetColor("_ShadowTint", Color.white);
            material.SetFloat("_ReceiveShadowsStrength", receiveShadowStrength);
            material.SetFloat("_AlphaClip", 0.0f);
            material.SetFloat("_Cutoff", 0.5f);
            SetOutlineProperties(material, false);
            SetRimProperties(material, false);
            SetMatcapProperties(material, false, null, Color.white, 1.0f, 0.0f);
            SetSpecularProperties(material, false);
            material.DisableKeyword("_ALPHACLIP");
            material.DisableKeyword("_ALPHATEST_ON");
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            return material;
        }

        private static Material CreateOrUpdateDToonOutlineMaterial()
        {
            const string rampPath = RampTextureGenerator.GeneratedRampOutputFolder + "/Ramp_Generic_Cool.png";

            Shader shader = Shader.Find("DToon/Character");
            if (shader == null)
            {
                throw new Exception("Shader not found: DToon/Character");
            }

            Texture2D rampTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(rampPath);
            if (rampTexture == null)
            {
                RampTextureGenerator.GenerateDefaultRampTextures();
                rampTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(rampPath);
            }

            if (rampTexture == null)
            {
                throw new Exception("Ramp texture not found: " + rampPath);
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(DToonOutlineMaterialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, DToonOutlineMaterialPath);
            }

            material.shader = shader;
            material.SetColor("_BaseColor", new Color(0.85f, 0.55f, 0.55f, 1.0f));
            material.SetTexture("_RampMap", rampTexture);
            material.SetFloat("_RampOffset", 0.0f);
            material.SetColor("_ShadowTint", Color.white);
            material.SetFloat("_ReceiveShadowsStrength", 0.0f);
            material.SetFloat("_AlphaClip", 0.0f);
            material.SetFloat("_Cutoff", 0.5f);
            material.DisableKeyword("_ALPHACLIP");
            material.DisableKeyword("_ALPHATEST_ON");
            SetOutlineProperties(material, false);
            SetRimProperties(material, false);
            SetMatcapProperties(material, false, null, Color.white, 1.0f, 0.0f);
            SetSpecularProperties(material, false);
            material.SetShaderPassEnabled("ForwardLit", true);
            material.SetShaderPassEnabled("Outline", false);
            material.SetShaderPassEnabled("ShadowCaster", true);
            material.renderQueue = -1;
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            return material;
        }

        private static Material CreateOrUpdateDToonOutlineHullMaterial()
        {
            return CreateOrUpdateDToonSolidOutlineHullMaterial(
                DToonOutlineHullMaterialPath,
                1.0f,
                4.0f,
                2001
            );
        }

        private static Material CreateOrUpdateDToonOutlineCloseupHullMaterial()
        {
            return CreateOrUpdateDToonSolidOutlineHullMaterial(
                DToonOutlineCloseupHullMaterialPath,
                0.0f,
                8.0f,
                1999
            );
        }

        private static Material CreateOrUpdateDToonSolidOutlineHullMaterial(
            string materialPath,
            float outlineCull,
            float outlineZTest,
            int renderQueue
        )
        {
            Shader shader = Shader.Find("DToon/Outline");
            if (shader == null)
            {
                throw new Exception("Shader not found: DToon/Outline");
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, materialPath);
            }

            material.shader = shader;
            material.SetColor("_OutlineColor", Color.black);
            material.SetColor("_BaseColor", new Color(0.85f, 0.55f, 0.55f, 1.0f));
            material.SetFloat("_Cutoff", 0.5f);
            material.SetFloat("_OutlineWidth", 0.005f);
            material.SetFloat("_OutlineDarkening", 0.3f);
            material.SetFloat("_OutlineDistanceScale", 1.0f);
            material.SetFloat("_OutlineMaxWidth", 0.05f);
            material.SetFloat("_OutlineAlphaClip", 0.0f);
            material.SetFloat("_OutlineCull", outlineCull);
            material.SetFloat("_OutlineZTest", outlineZTest);
            material.DisableKeyword("_OUTLINE_ALPHACLIP");
            material.renderQueue = renderQueue;
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            return material;
        }

        private static void SetOutlineProperties(Material material, bool enabled)
        {
            material.SetFloat("_OutlineEnable", enabled ? 1.0f : 0.0f);
            material.SetFloat("_OutlineWidth", 0.005f);
            material.SetFloat("_OutlineDarkening", 0.3f);
            material.SetFloat("_OutlineDistanceScale", 1.0f);
            material.SetFloat("_OutlineMaxWidth", 0.05f);

            if (enabled)
            {
                material.EnableKeyword("_OUTLINE");
            }
            else
            {
                material.DisableKeyword("_OUTLINE");
            }
        }

        private static void SetRimProperties(Material material, bool enabled)
        {
            SetRimProperties(material, enabled, enabled, Color.white, 1.0f, 4.0f, 0.05f);
        }

        private static void SetRimProperties(
            Material material,
            bool enabled,
            bool lightAware,
            Color color,
            float intensity,
            float power,
            float softness
        )
        {
            material.SetFloat("_RimEnable", enabled ? 1.0f : 0.0f);
            material.SetFloat("_RimLightAware", lightAware ? 1.0f : 0.0f);
            material.SetColor("_RimColor", color);
            material.SetFloat("_RimIntensity", intensity);
            material.SetFloat("_RimPower", power);
            material.SetFloat("_RimSoftness", softness);

            if (enabled)
            {
                material.EnableKeyword("_RIM");
            }
            else
            {
                material.DisableKeyword("_RIM");
            }

            if (enabled && lightAware)
            {
                material.EnableKeyword("_RIM_LIGHT_AWARE");
            }
            else
            {
                material.DisableKeyword("_RIM_LIGHT_AWARE");
            }
        }

        private static void SetMatcapProperties(
            Material material,
            bool enabled,
            Texture matcapTexture,
            Color tint,
            float intensity,
            float mode
        )
        {
            material.SetFloat("_MatcapEnable", enabled ? 1.0f : 0.0f);
            if (matcapTexture != null)
            {
                material.SetTexture("_MatcapTex", matcapTexture);
            }

            material.SetColor("_MatcapColor", tint);
            material.SetFloat("_MatcapIntensity", intensity);
            material.SetFloat("_MatcapMode", mode);

            if (enabled)
            {
                material.EnableKeyword("_MATCAP");
            }
            else
            {
                material.DisableKeyword("_MATCAP");
            }

        }

        private static void SetSpecularProperties(Material material, bool enabled)
        {
            SetSpecularProperties(material, enabled, Color.white, 1.0f, 32.0f, 0.5f, 0.05f);
        }

        private static void SetSpecularProperties(
            Material material,
            bool enabled,
            Color color,
            float intensity,
            float power,
            float threshold,
            float softness
        )
        {
            material.SetFloat("_SpecularEnable", enabled ? 1.0f : 0.0f);
            material.SetColor("_SpecularColor", color);
            material.SetFloat("_SpecularIntensity", intensity);
            material.SetFloat("_SpecularPower", power);
            material.SetFloat("_SpecularThreshold", threshold);
            material.SetFloat("_SpecularSoftness", softness);

            if (enabled)
            {
                material.EnableKeyword("_SPECULAR");
            }
            else
            {
                material.DisableKeyword("_SPECULAR");
            }
        }

        private static Texture2D CreateOrUpdateHairTestTexture()
        {
            EnsureAssetFolder("Assets/DToon/Samples", "TestAssets");

            const int size = 256;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false, false)
            {
                name = "HairStrands_Alpha",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            Color32[] pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                float v = (float)y / (size - 1);
                for (int x = 0; x < size; x++)
                {
                    float u = (float)x / (size - 1);
                    float alpha = 0.0f;

                    for (int strand = 0; strand < 8; strand++)
                    {
                        float center = (strand + 0.5f) / 8.0f;
                        center += Mathf.Sin(v * Mathf.PI * 3.0f + strand * 0.7f) * 0.008f;
                        float distance = Mathf.Abs(u - center);
                        float strandAlpha = Mathf.InverseLerp(0.055f, 0.032f, distance);
                        alpha = Mathf.Max(alpha, Mathf.Clamp01(strandAlpha));
                    }

                    byte a = (byte)Mathf.RoundToInt(alpha * 255.0f);
                    pixels[y * size + x] = new Color32(255, 255, 255, a);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);

            string absolutePath = AssetPathToAbsolutePath(HairTestTexturePath);
            string absoluteFolder = Path.GetDirectoryName(absolutePath);
            if (!string.IsNullOrEmpty(absoluteFolder))
            {
                Directory.CreateDirectory(absoluteFolder);
            }

            File.WriteAllBytes(absolutePath, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(HairTestTexturePath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);

            TextureImporter importer = AssetImporter.GetAtPath(HairTestTexturePath) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException("Failed to import hair test texture: " + HairTestTexturePath);
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

            Texture2D importedTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(HairTestTexturePath);
            if (importedTexture == null)
            {
                throw new Exception("Hair test texture not found after import: " + HairTestTexturePath);
            }

            return importedTexture;
        }

        private static void EnsureHarnessFolders()
        {
            EnsureAssetFolder("Assets", "DToon");
            EnsureAssetFolder("Assets/DToon", "Samples");
            EnsureAssetFolder("Assets/DToon/Samples", "Harness");
            EnsureAssetFolder("Assets/DToon/Samples", "TestAssets");
            EnsureAssetFolder("Assets/DToon/Samples/Harness", "_Common");
            EnsureAssetFolder("Assets/DToon/Samples/Harness/_Common", "Materials");
        }

        private static void ConfigureHarnessRenderSettings()
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.1f, 0.1f, 0.12f, 1.0f);
            QualitySettings.shadowDistance = Mathf.Max(QualitySettings.shadowDistance, 20.0f);
        }

        private static void EnsureDToonOutlineRendererFeature()
        {
            UniversalRenderPipelineAsset urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urpAsset == null)
            {
                Log("[Harness] Active render pipeline is not URP; skipping DToon outline renderer feature setup.");
                return;
            }

            ScriptableRendererData rendererData = GetDefaultRendererData(urpAsset);
            if (rendererData == null)
            {
                Log("[Harness] Could not resolve URP renderer data; skipping DToon outline renderer feature setup.");
                return;
            }

            foreach (ScriptableRendererFeature feature in rendererData.rendererFeatures)
            {
                if (feature is URP14_RenderFeature outlineFeature)
                {
                    ConfigureDToonOutlineRendererFeature(outlineFeature);
                    rendererData.SetDirty();
                    EditorUtility.SetDirty(rendererData);
                    Log("[Harness] DToon outline renderer feature already present.");
                    return;
                }
            }

            URP14_RenderFeature createdOutlineFeature = ScriptableObject.CreateInstance<URP14_RenderFeature>();
            createdOutlineFeature.name = "DToon URP14 Render Feature";
            AssetDatabase.AddObjectToAsset(createdOutlineFeature, rendererData);
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(createdOutlineFeature, out string _, out long localId);

            SerializedObject rendererObject = new SerializedObject(rendererData);
            SerializedProperty featuresProperty = rendererObject.FindProperty("m_RendererFeatures");
            SerializedProperty featureMapProperty = rendererObject.FindProperty("m_RendererFeatureMap");

            int index = featuresProperty.arraySize;
            featuresProperty.InsertArrayElementAtIndex(index);
            featuresProperty.GetArrayElementAtIndex(index).objectReferenceValue = createdOutlineFeature;

            featureMapProperty.InsertArrayElementAtIndex(index);
            featureMapProperty.GetArrayElementAtIndex(index).longValue = localId;

            rendererObject.ApplyModifiedPropertiesWithoutUndo();
            ConfigureDToonOutlineRendererFeature(createdOutlineFeature);
            rendererData.SetDirty();
            EditorUtility.SetDirty(rendererData);
            AssetDatabase.SaveAssets();
            Log("[Harness] Added DToon outline renderer feature to active URP renderer data.");
        }

        private static void ConfigureDToonOutlineRendererFeature(URP14_RenderFeature outlineFeature)
        {
            SerializedObject featureObject = new SerializedObject(outlineFeature);
            SerializedProperty passEventProperty = featureObject.FindProperty("outlinePassEvent");
            SerializedProperty layerMaskProperty = featureObject.FindProperty("layerMask");

            if (passEventProperty != null)
            {
                passEventProperty.intValue = (int)RenderPassEvent.AfterRenderingOpaques;
            }

            if (layerMaskProperty != null)
            {
                layerMaskProperty.intValue = -1;
            }

            featureObject.ApplyModifiedPropertiesWithoutUndo();
            outlineFeature.SetActive(true);
            EditorUtility.SetDirty(outlineFeature);
        }

        private static ScriptableRendererData GetDefaultRendererData(UniversalRenderPipelineAsset urpAsset)
        {
            SerializedObject pipelineObject = new SerializedObject(urpAsset);
            SerializedProperty rendererDataList = pipelineObject.FindProperty("m_RendererDataList");
            SerializedProperty defaultRendererIndex = pipelineObject.FindProperty("m_DefaultRendererIndex");

            if (rendererDataList == null || defaultRendererIndex == null || rendererDataList.arraySize == 0)
            {
                return null;
            }

            int index = Mathf.Clamp(defaultRendererIndex.intValue, 0, rendererDataList.arraySize - 1);
            return rendererDataList.GetArrayElementAtIndex(index).objectReferenceValue as ScriptableRendererData;
        }

        private static Camera CreateHarnessCamera(Vector3 position, Quaternion rotation, float fieldOfView)
        {
            GameObject cameraObject = new GameObject("HarnessCamera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = position;
            cameraObject.transform.rotation = rotation;

            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = fieldOfView;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.18f, 0.18f, 0.20f, 1.0f);
            camera.allowHDR = false;
            camera.allowMSAA = false;

            UniversalAdditionalCameraData cameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
            cameraData.renderPostProcessing = false;
            cameraData.antialiasing = AntialiasingMode.None;

            return camera;
        }

        private static Light CreateMainLight(Quaternion rotation)
        {
            GameObject lightObject = new GameObject("MainLight");
            lightObject.transform.rotation = rotation;
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.0f;
            light.color = Color.white;
            light.shadows = LightShadows.Soft;
            light.bounceIntensity = 0.0f;
            return light;
        }

        private static void EnsureAssetFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        private static string AssetPathToAbsolutePath(string assetPath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            return Path.GetFullPath(Path.Combine(projectRoot, assetPath));
        }

        private static Camera FindHarnessCamera()
        {
            var go = GameObject.Find("HarnessCamera");
            return go != null ? go.GetComponent<Camera>() : null;
        }

        private static Texture2D RenderCameraToTexture(Camera cam, int width, int height)
        {
            var rtDesc = new RenderTextureDescriptor(width, height, RenderTextureFormat.ARGB32, 24)
            {
                sRGB = true,
                msaaSamples = 1
            };
            var rt = RenderTexture.GetTemporary(rtDesc);
            var prevTarget   = cam.targetTexture;
            var prevActive   = RenderTexture.active;

            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
            tex.Apply(false, false);

            cam.targetTexture = prevTarget;
            RenderTexture.active = prevActive;
            RenderTexture.ReleaseTemporary(rt);
            return tex;
        }

        private static void WriteCameraCapture(Camera cam, string outputPath)
        {
            Texture2D captured = RenderCameraToTexture(cam, 1024, 1024);
            try
            {
                byte[] png = captured.EncodeToPNG();
                string outputDir = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }

                File.WriteAllBytes(outputPath, png);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(captured);
            }
        }

        private static void CaptureStep1FromMenu(string testName)
        {
            string previousScenePath = EditorSceneManager.GetActiveScene().path;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            string outputPath = ResolveHarnessOutputPath(testName);

            try
            {
                ShaderUtil.allowAsyncCompilation = false;
                CreateGeneratedHarnessSceneIfNeeded(testName);

                string scenePath = ResolveScenePath(testName);
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

                Camera cam = FindHarnessCamera();
                if (cam == null)
                {
                    throw new Exception("HarnessCamera not found in generated scene.");
                }

                WriteCameraCapture(cam, outputPath);
                string lightRotation = GetMainLightRotationText();
                Log($"[DToon] Captured {testName} to {outputPath}");
                Log("[DToon] " + lightRotation);
                EditorUtility.RevealInFinder(outputPath);
                EditorUtility.DisplayDialog(
                    "DToon Harness Capture",
                    "Capture complete.\n\n" + outputPath + "\n\n" + lightRotation,
                    "OK"
                );
            }
            catch (Exception e)
            {
                Debug.LogError("[DToon] Harness capture failed: " + e);
                EditorUtility.DisplayDialog(
                    "DToon Harness Capture Failed",
                    e.Message,
                    "OK"
                );
            }
            finally
            {
                if (!string.IsNullOrEmpty(previousScenePath))
                {
                    try
                    {
                        EditorSceneManager.OpenScene(previousScenePath, OpenSceneMode.Single);
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning("[DToon] Could not restore previous scene: " + e.Message);
                    }
                }
            }
        }

        private static string GetMainLightRotationText()
        {
            GameObject lightObject = GameObject.Find("MainLight");
            if (lightObject == null)
            {
                return "MainLight Rotation: not found";
            }

            Vector3 euler = lightObject.transform.rotation.eulerAngles;
            return string.Format(
                "MainLight Rotation: X={0:0.###}, Y={1:0.###}, Z={2:0.###}",
                euler.x,
                NormalizeInspectorAngle(euler.y),
                euler.z
            );
        }

        private static float NormalizeInspectorAngle(float value)
        {
            return value > 180.0f ? value - 360.0f : value;
        }

        private static string GetArg(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name) return args[i + 1];
            }
            return null;
        }

        private static void Log(string msg)
        {
            // UnityEngine.Debug routes to the -logFile in batch mode.
            UnityEngine.Debug.Log(msg);
        }
    }
}
