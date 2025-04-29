using UnityEngine;
using UnityEditor;
using System.Reflection;
using Codice.Client.BaseCommands.TubeClient;

namespace WataOfuton.Tools
{
    public class CustomAssetImportWindow : EditorWindow
    {
        private const string MENU_ITEM_PATH = "Window/WataOfuton/Custom Asset Importer";
        private static bool bakeAxisConversion;
        private static bool importBlendShapes;
        private static bool importDeformPercent;
        private static bool importVisibility;
        private static bool importCameras;
        private static bool importLights;
        private static bool preserveHierarchy;
        private static bool sortHierarchyByName;
        private static int meshCompressionIndex;
        private static string[] meshCompressionOptions = new string[] { "Off", "Low", "Medium", "High" };
        private static bool isReadable;
        private static int optimizeMeshIndex;
        private static string[] optimizeMeshOptions = new string[] { "Nothing", "Everything", "Polygon Order", "Vertex Order" };
        private static bool generateColliders;
        private static bool keepQuads;
        private static bool weldVertices;
        private static int indexFormatIndex;
        private static string[] indexFormatOptions = new string[] { "Auto", "16 bits", "32 bits" };
        private static bool legacyBlendShapeNormals;
        private static int modelImporterNormalsIndex;
        private static string[] modelImporterNormalsOptions = new string[] { "Import", "Calculate", "None" };
        private static int blendShapeNormalsIndex;
        private static string[] blendShapeNormalsOptions = new string[] { "Import", "Calculate", "None" };
        private static int normalsModeIndex;
        private static string[] normalsModeOptions = new string[] { "Unweighted (Legacy)", "Unweighted", "Area Weighted", "Angle Weighted", "Area And Angle Weighted" };
        private static int smoothnessSourceIndex;
        private static string[] smoothnessSourceOptions = new string[] { "Prefer Smoothing Groups", "From Smoothing Groups", "From Angle", "None" };
        private static int smoothingAngle;
        private static int tangentsIndex;
        private static string[] tangentsOptions = new string[] { "Import", "Calculate Legacy", "Calculate Legacy With Split Tangents", "Calculate Mikktspace", "None" };
        private static bool swapUVs;
        private static bool generateLightmapUVs;
        private static bool strictVertexDataChecks;

        private static int maxTextureSizeIndex;
        public static string[] maxTextureSizeOptions = new string[] { "2048", "4096", "8192" };
        private static bool generateMipmaps;
        private static bool streamingMipmaps;
        private static bool crunchedCompression;
        private static int compressionQualityIndex;
        private static string[] compressionQualityOptions = new string[] { "10", "50", "100" };

        private bool[] checkEnables;
        private int arrayNum;
        private Vector2 scrollPosition;


        [MenuItem(MENU_ITEM_PATH)]
        public static void ShowWindow()
        {
            GetWindow<CustomAssetImportWindow>("Custom Asset Import");
        }

        private void OnEnable()
        {
            checkEnables = new bool[30];
            LoadSettings();
        }

        private void OnGUI()
        {
            GUILayout.Label("Custom Asset Import Settings", EditorStyles.boldLabel);
            string text = "3Dモデルやテクスチャをインポートする際の Import Settings を一括して設定するエディタ拡張です。\n"
                        + "一括設定を行いたいパラメータ名の左にあるチェックボックスにチェックを入れ、それぞれの値を設定した後、最下部の [Save Settings] を押してください。\n"
                        + "Importer を実行する際に自動で設定を上書きします。";
            EditorGUILayout.HelpBox(text, MessageType.Info);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            EditorGUILayout.Space(10);
            var rect = EditorGUILayout.GetControlRect(false, 2);
            rect.height = 2;
            EditorGUI.DrawRect(rect, Color.gray);

            EditorGUILayout.LabelField("[Model]", EditorStyles.boldLabel);

            arrayNum = 0;

            EditorGUILayout.Space(3);
            GUILayout.Label("Scene");
            EditorUtil.HorizontalFieldBool("Bake Axis Conversion", ref bakeAxisConversion, ref checkEnables[arrayNum++]);
            EditorUtil.HorizontalFieldBool("Import Blendshapes", ref importBlendShapes, ref checkEnables[arrayNum++]);
            EditorUtil.HorizontalFieldBool("Import Deform Percent", ref importDeformPercent, ref checkEnables[arrayNum++]);
            EditorUtil.HorizontalFieldBool("Import Visibility", ref importVisibility, ref checkEnables[arrayNum++]);
            EditorUtil.HorizontalFieldBool("Import Cameras", ref importCameras, ref checkEnables[arrayNum++]);
            EditorUtil.HorizontalFieldBool("Import Lights", ref importLights, ref checkEnables[arrayNum++]);
            EditorUtil.HorizontalFieldBool("Preserve Hierarchy", ref preserveHierarchy, ref checkEnables[arrayNum++]);
            EditorUtil.HorizontalFieldBool("Sort Hierarchy By Name", ref sortHierarchyByName, ref checkEnables[arrayNum++]);

            EditorGUILayout.Space(3);
            GUILayout.Label("Meshes");
            EditorUtil.HorizontalFieldPopup("Mesh Compression", ref meshCompressionIndex, meshCompressionOptions, ref checkEnables[arrayNum++]);
            EditorUtil.HorizontalFieldBool("Read / Write", ref isReadable, ref checkEnables[arrayNum++]);
            EditorUtil.HorizontalFieldPopup("Optimize Mesh", ref optimizeMeshIndex, optimizeMeshOptions, ref checkEnables[arrayNum++]);
            EditorUtil.HorizontalFieldBool("Generate Colliders", ref generateColliders, ref checkEnables[arrayNum++]);

            EditorGUILayout.Space(3);
            GUILayout.Label("Geometry");
            EditorUtil.HorizontalFieldBool("Keep Quads", ref keepQuads, ref checkEnables[arrayNum++]);
            EditorUtil.HorizontalFieldBool("Weld Vertices", ref weldVertices, ref checkEnables[arrayNum++]);
            EditorUtil.HorizontalFieldPopup("Index Format", ref indexFormatIndex, indexFormatOptions, ref checkEnables[arrayNum++]);
            EditorUtil.HorizontalFieldBool("Legacy Blend Shape Normals", ref legacyBlendShapeNormals, ref checkEnables[arrayNum++]);
            EditorUtil.HorizontalFieldPopup("Model Importer Normals", ref modelImporterNormalsIndex, modelImporterNormalsOptions, ref checkEnables[arrayNum++]);
            EditorUtil.HorizontalFieldPopup("Blend Shape Normals", ref blendShapeNormalsIndex, blendShapeNormalsOptions, ref checkEnables[arrayNum++]);
            EditorUtil.HorizontalFieldPopup("Normals Mode", ref normalsModeIndex, normalsModeOptions, ref checkEnables[arrayNum++]);
            EditorUtil.HorizontalFieldPopup("Smoothness Source", ref smoothnessSourceIndex, smoothnessSourceOptions, ref checkEnables[arrayNum++]);
            EditorUtil.HorizontalFieldIntSlider("Smoothing Angle", ref smoothingAngle, ref checkEnables[arrayNum++], 0, 180);
            EditorUtil.HorizontalFieldPopup("Tangents", ref tangentsIndex, tangentsOptions, ref checkEnables[arrayNum++]);
            EditorUtil.HorizontalFieldBool("Swap UVs", ref swapUVs, ref checkEnables[arrayNum++]);
            EditorUtil.HorizontalFieldBool("Generate Lightmap UVs", ref generateLightmapUVs, ref checkEnables[arrayNum++]);
            EditorUtil.HorizontalFieldBool("Strict Vertex Data Checks", ref strictVertexDataChecks, ref checkEnables[arrayNum++]);

            EditorGUILayout.Space(10);
            rect = EditorGUILayout.GetControlRect(false, 2);
            rect.height = 2;
            EditorGUI.DrawRect(rect, Color.gray);

            EditorGUILayout.LabelField("[Texture]", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorUtil.HorizontalFieldPopup("Texture Max Size Limit", ref maxTextureSizeIndex, maxTextureSizeOptions, ref checkEnables[arrayNum++]);
            EditorUtil.HorizontalFieldBool("Generate Mipmaps", ref generateMipmaps, ref checkEnables[arrayNum++]);
            EditorUtil.HorizontalFieldBool("Streaming Mipmaps", ref streamingMipmaps, ref checkEnables[arrayNum++]);
            EditorUtil.HorizontalFieldBool("Crunched Compression", ref crunchedCompression, ref checkEnables[arrayNum++]);
            if (crunchedCompression)
            {
                EditorUtil.HorizontalFieldPopup("  Compression Quality", ref compressionQualityIndex, compressionQualityOptions, ref checkEnables[arrayNum++]);
            }

            EditorGUILayout.Space(5);
            if (GUILayout.Button("Save Settings"))
            {
                SaveSettings();
            }
            EditorGUILayout.Space(5);
            if (GUILayout.Button("Initialize Settings"))
            {
                // 警告ダイアログを表示
                bool userClickedOK = EditorUtility.DisplayDialog(
                    "確認", // タイトル
                    "設定を初期化します。よろしいですか？", // メッセージ
                    "はい", // OK ボタンのテキスト
                    "いいえ" // キャンセルボタンのテキスト
                );
                if (userClickedOK)
                {
                    InitSettings();
                    LoadSettings();
                }
            }

            EditorGUILayout.Space(20);
            EditorGUILayout.EndScrollView();
        }

        private void LoadSettings()
        {
            bakeAxisConversion = EditorPrefs.GetBool("CustomAssetImport.BakeAxisConversion", true);
            importBlendShapes = EditorPrefs.GetBool("CustomAssetImport.ImportBlendshapes", true);
            importDeformPercent = EditorPrefs.GetBool("CustomAssetImport.ImportDeformPercent", true);
            importVisibility = EditorPrefs.GetBool("CustomAssetImport.ImportVisibility", true);
            importCameras = EditorPrefs.GetBool("CustomAssetImport.ImportCameras", true);
            importLights = EditorPrefs.GetBool("CustomAssetImport.ImportLights", true);
            preserveHierarchy = EditorPrefs.GetBool("CustomAssetImport.PreserveHierarchy", true);
            sortHierarchyByName = EditorPrefs.GetBool("CustomAssetImport.SortHierarchyByName", true);

            meshCompressionIndex = EditorPrefs.GetInt("CustomAssetImport.MeshCompression", 0);
            isReadable = EditorPrefs.GetBool("CustomAssetImport.IsReadable", true);
            optimizeMeshIndex = EditorPrefs.GetInt("CustomAssetImport.OptimizeMesh", 1);
            generateColliders = EditorPrefs.GetBool("CustomAssetImport.GenerateColliders", false);

            keepQuads = EditorPrefs.GetBool("CustomAssetImport.KeepQuads", false);
            weldVertices = EditorPrefs.GetBool("CustomAssetImport.WeldVertices", true);
            indexFormatIndex = EditorPrefs.GetInt("CustomAssetImport.IndexFormat", 0);
            legacyBlendShapeNormals = EditorPrefs.GetBool("CustomAssetImport.LegacyBlendShapeNormals", false);
            modelImporterNormalsIndex = EditorPrefs.GetInt("CustomAssetImport.ModelImporterNormals", 0);
            blendShapeNormalsIndex = EditorPrefs.GetInt("CustomAssetImport.BlendShapeNormals", 2);
            normalsModeIndex = EditorPrefs.GetInt("CustomAssetImport.NormalsMode", 4);
            smoothnessSourceIndex = EditorPrefs.GetInt("CustomAssetImport.SmoothnessSource", 0);
            smoothingAngle = EditorPrefs.GetInt("CustomAssetImport.SmoothingAngle", 60);
            tangentsIndex = EditorPrefs.GetInt("CustomAssetImport.Tangent", 3);
            swapUVs = EditorPrefs.GetBool("CustomAssetImport.SwapUVs", false);
            generateLightmapUVs = EditorPrefs.GetBool("CustomAssetImport.GenerateLightmapUVs", false);
            strictVertexDataChecks = EditorPrefs.GetBool("CustomAssetImport.StrictVertexDataChecks", false);

            maxTextureSizeIndex = EditorPrefs.GetInt("CustomAssetImport.MaxTextureSize", 2);
            generateMipmaps = EditorPrefs.GetBool("CustomAssetImport.GenerateMipmaps", true);
            streamingMipmaps = EditorPrefs.GetBool("CustomAssetImport.StreamingMipmaps", true);
            crunchedCompression = EditorPrefs.GetBool("CustomAssetImport.CrunchedCompression", false);
            compressionQualityIndex = EditorPrefs.GetInt("CustomAssetImport.CompressionQuality", 2);

            checkEnables = EditorUtil.LoadBoolArray("CustomAssetImport.CheckEnables", checkEnables.Length);

            Debug.Log("[Custom Asset Importer] Load Settings.");
        }

        private void SaveSettings()
        {
            EditorPrefs.SetBool("CustomAssetImport.BakeAxisConversion", bakeAxisConversion);
            EditorPrefs.SetBool("CustomAssetImport.ImportBlendshapes", importBlendShapes);
            EditorPrefs.SetBool("CustomAssetImport.ImportDeformPercent", importDeformPercent);
            EditorPrefs.SetBool("CustomAssetImport.ImportVisibility", importVisibility);
            EditorPrefs.SetBool("CustomAssetImport.ImportCameras", importCameras);
            EditorPrefs.SetBool("CustomAssetImport.ImportLights", importLights);
            EditorPrefs.SetBool("CustomAssetImport.PreserveHierarchy", preserveHierarchy);
            EditorPrefs.SetBool("CustomAssetImport.SortHierarchyByName", sortHierarchyByName);

            EditorPrefs.SetInt("CustomAssetImport.MeshCompression", meshCompressionIndex);
            EditorPrefs.SetBool("CustomAssetImport.IsReadable", isReadable);
            EditorPrefs.SetInt("CustomAssetImport.OptimizeMesh", optimizeMeshIndex);
            EditorPrefs.SetBool("CustomAssetImport.GenerateColliders", generateColliders);

            EditorPrefs.SetBool("CustomAssetImport.KeepQuads", keepQuads);
            EditorPrefs.SetBool("CustomAssetImport.WeldVertices", weldVertices);
            EditorPrefs.SetInt("CustomAssetImport.IndexFormat", indexFormatIndex);
            EditorPrefs.SetBool("CustomAssetImport.LegacyBlendShapeNormals", legacyBlendShapeNormals);
            EditorPrefs.SetInt("CustomAssetImport.ModelImporterNormals", modelImporterNormalsIndex);
            EditorPrefs.SetInt("CustomAssetImport.BlendShapeNormals", blendShapeNormalsIndex);
            EditorPrefs.SetInt("CustomAssetImport.NormalsMode", normalsModeIndex);
            EditorPrefs.SetInt("CustomAssetImport.SmoothnessSource", smoothnessSourceIndex);
            EditorPrefs.SetInt("CustomAssetImport.SmoothingAngle", smoothingAngle);
            EditorPrefs.SetInt("CustomAssetImport.Tangent", tangentsIndex);
            EditorPrefs.SetBool("CustomAssetImport.SwapUVs", swapUVs);
            EditorPrefs.SetBool("CustomAssetImport.GenerateLightmapUVs", generateLightmapUVs);
            EditorPrefs.SetBool("CustomAssetImport.StrictVertexDataChecks", strictVertexDataChecks);

            EditorPrefs.SetInt("CustomAssetImport.MaxTextureSize", maxTextureSizeIndex);
            EditorPrefs.SetBool("CustomAssetImport.StreamingMipmaps", streamingMipmaps);
            EditorPrefs.SetBool("CustomAssetImport.CrunchedCompression", crunchedCompression);
            EditorPrefs.SetInt("CustomAssetImport.CompressionQuality", compressionQualityIndex);

            EditorUtil.SaveBoolArray("CustomAssetImport.CheckEnables", checkEnables);

            Debug.Log("[Custom Asset Importer] Settings Saved.");
        }
        private void InitSettings()
        {
            EditorPrefs.DeleteKey("CustomAssetImport.BakeAxisConversion");
            EditorPrefs.DeleteKey("CustomAssetImport.ImportBlendshapes");
            EditorPrefs.DeleteKey("CustomAssetImport.ImportDeformPercent");
            EditorPrefs.DeleteKey("CustomAssetImport.ImportVisibility");
            EditorPrefs.DeleteKey("CustomAssetImport.ImportCameras");
            EditorPrefs.DeleteKey("CustomAssetImport.ImportLights");
            EditorPrefs.DeleteKey("CustomAssetImport.PreserveHierarchy");
            EditorPrefs.DeleteKey("CustomAssetImport.SortHierarchyByName");

            EditorPrefs.DeleteKey("CustomAssetImport.MeshCompression");
            EditorPrefs.DeleteKey("CustomAssetImport.IsReadable");
            EditorPrefs.DeleteKey("CustomAssetImport.OptimizeMesh");
            EditorPrefs.DeleteKey("CustomAssetImport.GenerateColliders");

            EditorPrefs.DeleteKey("CustomAssetImport.KeepQuads");
            EditorPrefs.DeleteKey("CustomAssetImport.WeldVertices");
            EditorPrefs.DeleteKey("CustomAssetImport.IndexFormat");
            EditorPrefs.DeleteKey("CustomAssetImport.LegacyBlendShapeNormals");
            EditorPrefs.DeleteKey("CustomAssetImport.ModelImporterNormals");
            EditorPrefs.DeleteKey("CustomAssetImport.BlendShapeNormals");
            EditorPrefs.DeleteKey("CustomAssetImport.NormalsMode");
            EditorPrefs.DeleteKey("CustomAssetImport.SmoothnessSource");
            EditorPrefs.DeleteKey("CustomAssetImport.SmoothingAngle");
            EditorPrefs.DeleteKey("CustomAssetImport.Tangent");
            EditorPrefs.DeleteKey("CustomAssetImport.SwapUVs");
            EditorPrefs.DeleteKey("CustomAssetImport.GenerateLightmapUVs");
            EditorPrefs.DeleteKey("CustomAssetImport.StrictVertexDataChecks");

            EditorPrefs.DeleteKey("CustomAssetImport.MaxTextureSize");
            EditorPrefs.DeleteKey("CustomAssetImport.GenerateMipmaps");
            EditorPrefs.DeleteKey("CustomAssetImport.StreamingMipmaps");
            EditorPrefs.DeleteKey("CustomAssetImport.CrunchedCompression");
            EditorPrefs.DeleteKey("CustomAssetImport.CompressionQuality");

            // EditorUtil.SaveBoolArray("CustomAssetImport.CheckEnables", new bool[checkEnables.Length]);
            EditorPrefs.DeleteKey("CustomAssetImport.CheckEnables");

            Debug.Log("[Custom Asset Importer] Settings Initialized.");
        }
    }

    public class CustomAssetImportProcessor : AssetPostprocessor
    {
        // モデルがインポートされたときの処理
        void OnPostprocessModel(GameObject g)
        {
            ModelImporter modelImporter = (ModelImporter)assetImporter;

            if (System.IO.Path.GetExtension(assetPath).ToLower() != ".fbx") return;

            bool[] enables = EditorUtil.LoadBoolArray("CustomAssetImport.CheckEnables");
            if (enables == null) return;

            if (enables[0]) modelImporter.bakeAxisConversion = EditorPrefs.GetBool("CustomAssetImport.BakeAxisConversion");
            if (enables[1]) modelImporter.importBlendShapes = EditorPrefs.GetBool("CustomAssetImport.ImportBlendshapes");
            if (enables[2]) modelImporter.importBlendShapeDeformPercent = EditorPrefs.GetBool("CustomAssetImport.ImportBlendshapes");
            if (enables[3]) modelImporter.importVisibility = EditorPrefs.GetBool("CustomAssetImport.ImportVisibility");
            if (enables[4]) modelImporter.importCameras = EditorPrefs.GetBool("CustomAssetImport.ImportCameras");
            if (enables[5]) modelImporter.importLights = EditorPrefs.GetBool("CustomAssetImport.ImportLights");
            if (enables[6]) modelImporter.preserveHierarchy = EditorPrefs.GetBool("CustomAssetImport.PreserveHierarchy");
            if (enables[7]) modelImporter.sortHierarchyByName = EditorPrefs.GetBool("CustomAssetImport.SortHierarchyByName");

            if (enables[8]) modelImporter.meshCompression = (ModelImporterMeshCompression)EditorPrefs.GetInt("CustomAssetImport.MeshCompression");
            if (enables[9]) modelImporter.isReadable = EditorPrefs.GetBool("CustomAssetImport.IsReadable");
            if (enables[10])
            {
                // Optimize Mesh の設定を適用する新しい方法
                int optimizeMeshSetting = EditorPrefs.GetInt("CustomAssetImport.OptimizeMesh", 0);
                switch (optimizeMeshSetting)
                {
                    case 0: // "Nothing" の場合
                        // None がないみたいなので保留...
                        // modelImporter.meshOptimizationFlags = MeshOptimizationFlags.None;
                        break;
                    case 1: // "Everything" の場合
                        modelImporter.meshOptimizationFlags = MeshOptimizationFlags.Everything;
                        break;
                    case 2: // "Polygon Order" の場合
                        modelImporter.meshOptimizationFlags = MeshOptimizationFlags.PolygonOrder;
                        break;
                    case 3: // "Vertex Order" の場合
                        modelImporter.meshOptimizationFlags = MeshOptimizationFlags.VertexOrder;
                        break;
                    default:
                        // modelImporter.meshOptimizationFlags = MeshOptimizationFlags.None;
                        break;
                }
            }
            if (enables[11]) modelImporter.addCollider = EditorPrefs.GetBool("CustomAssetImport.GenerateColliders");

            if (enables[12]) modelImporter.keepQuads = EditorPrefs.GetBool("CustomAssetImport.KeepQuads");
            if (enables[13]) modelImporter.weldVertices = EditorPrefs.GetBool("CustomAssetImport.WeldVertices");
            if (enables[14]) modelImporter.indexFormat = (ModelImporterIndexFormat)EditorPrefs.GetInt("CustomAssetImport.IndexFormat");
            if (enables[15])
            {
                // https://forum.unity.com/threads/legacy-blend-shape-normals-missing-on-modelimporter.1166324/
                string pName = "legacyComputeAllNormalsFromSmoothingGroupsWhenMeshHasBlendShapes";
                PropertyInfo prop = modelImporter.GetType().GetProperty(pName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                prop?.SetValue(modelImporter, EditorPrefs.GetBool("CustomAssetImport.LegacyBlendShapeNormals", false));
            }
            if (enables[16]) modelImporter.importNormals = (ModelImporterNormals)EditorPrefs.GetInt("CustomAssetImport.ModelImporterNormals");
            if (enables[17]) modelImporter.importBlendShapeNormals = (ModelImporterNormals)EditorPrefs.GetInt("CustomAssetImport.BlendShapeNormals");
            if (enables[18]) modelImporter.normalCalculationMode = (ModelImporterNormalCalculationMode)EditorPrefs.GetInt("CustomAssetImport.NormalsMode");
            if (enables[19]) modelImporter.normalSmoothingSource = (ModelImporterNormalSmoothingSource)EditorPrefs.GetInt("CustomAssetImport.SmoothnessSource");
            if (enables[20]) modelImporter.normalSmoothingAngle = EditorPrefs.GetInt("CustomAssetImport.SmoothingAngle");
            if (enables[21]) modelImporter.importTangents = (ModelImporterTangents)EditorPrefs.GetInt("CustomAssetImport.Tangent");
            if (enables[22]) modelImporter.swapUVChannels = EditorPrefs.GetBool("CustomAssetImport.SwapUVs", false);
            if (enables[23]) modelImporter.generateSecondaryUV = EditorPrefs.GetBool("CustomAssetImport.GenerateLightmapUVs");
            if (enables[24]) modelImporter.extraExposedTransformPaths = EditorPrefs.GetBool("CustomAssetImport.StrictVertexDataChecks") ? new string[] { "YourSpecificPathsHere" } : new string[0]; // This line needs customization based on your specific needs.
        }

        // テクスチャがインポートされたときの処理
        void OnPreprocessTexture()
        {
            TextureImporter textureImporter = (TextureImporter)assetImporter;

            bool[] enables = EditorUtil.LoadBoolArray("CustomAssetImport.CheckEnables");

            int maxTextureSize = int.Parse(CustomAssetImportWindow.maxTextureSizeOptions[EditorPrefs.GetInt("CustomAssetImport.MaxTextureSize")]);
            if (maxTextureSize < textureImporter.maxTextureSize)
            {
                if (enables[25]) textureImporter.maxTextureSize = maxTextureSize;
            }

            if (enables[26]) textureImporter.streamingMipmaps = EditorPrefs.GetBool("CustomAssetImport.StreamingMipmaps", true);
            if (enables[27]) textureImporter.crunchedCompression = EditorPrefs.GetBool("CustomAssetImport.CrunchedCompression", false);
            if (textureImporter.crunchedCompression)
            {
                if (enables[28]) textureImporter.compressionQuality = EditorPrefs.GetInt("CustomAssetImport.CompressionQuality");
            }
        }

    }

    public static class EditorUtil
    {
        public static void HorizontalFieldBool(string labelText, ref bool parametor, ref bool checkEnable)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUIUtility.labelWidth = 20;
            checkEnable = EditorGUILayout.Toggle("", checkEnable, GUILayout.Width(20));
            EditorGUIUtility.labelWidth = 200;
            parametor = EditorGUILayout.Toggle(labelText, parametor);
            EditorGUIUtility.labelWidth = 0;
            EditorGUILayout.EndHorizontal();
        }

        public static void HorizontalFieldPopup(string labelText, ref int index, string[] settingText, ref bool checkEnable)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUIUtility.labelWidth = 20;
            checkEnable = EditorGUILayout.Toggle("", checkEnable, GUILayout.Width(20));
            EditorGUIUtility.labelWidth = 200;
            index = EditorGUILayout.Popup(labelText, index, settingText);
            EditorGUIUtility.labelWidth = 0;
            EditorGUILayout.EndHorizontal();
        }

        public static void HorizontalFieldIntSlider(string labelText, ref int parametor, ref bool checkEnable, int left, int right)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUIUtility.labelWidth = 20;
            checkEnable = EditorGUILayout.Toggle("", checkEnable, GUILayout.Width(20));
            EditorGUIUtility.labelWidth = 200;
            parametor = EditorGUILayout.IntSlider(labelText, parametor, left, right);
            EditorGUIUtility.labelWidth = 0;
            EditorGUILayout.EndHorizontal();
        }

        public static void SaveBoolArray(string key, bool[] array)
        {
            string json = JsonUtility.ToJson(new BoolArrayWrapper { Array = array });
            EditorPrefs.SetString(key, json);
        }
        public static bool[] LoadBoolArray(string key, int defaultSize = 0)
        {
            if (EditorPrefs.HasKey(key))
            {
                string json = EditorPrefs.GetString(key);
                BoolArrayWrapper wrapper = JsonUtility.FromJson<BoolArrayWrapper>(json);
                return wrapper.Array;
            }
            return new bool[defaultSize];
        }
        [System.Serializable]
        private class BoolArrayWrapper
        {
            public bool[] Array;
        }
    }
}