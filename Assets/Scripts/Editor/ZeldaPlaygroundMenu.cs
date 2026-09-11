#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using ZeldaOoT.Environment;

namespace ZeldaOoT.Editor
{
    /// <summary>
    /// Editor Menu utility to generate or reset the Zelda Showcase Playground directly in the active scene.
    /// </summary>
    public static class ZeldaPlaygroundMenu
    {
        [MenuItem("Zelda/Build Showcase Test Playground", false, 10)]
        public static void BuildPlaygroundInScene()
        {
            // Clear existing if present
            ClearPlaygroundFromScene();

            GameObject spawnerObj = new GameObject("Playground_Spawner");
            var builder = spawnerObj.AddComponent<ZeldaPlaygroundBuilder>();
            builder.BuildPlayground();

            Undo.RegisterCreatedObjectUndo(spawnerObj, "Build Zelda Playground");
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            Debug.Log("<b>[Zelda Showcase]</b> Successfully generated Test Playground with Arena, Water Swimming Basin, Platforms, Combat Dummies, and UI Toolkit HUD!");
        }

        [MenuItem("Zelda/Clear Playground", false, 11)]
        public static void ClearPlaygroundFromScene()
        {
            string[] namesToClear = {
                "Playground_Spawner",
                "Zelda_Test_Playground",
                "Player_Link",
                "Zelda_UI_Toolkit_Canvas",
                "ZeldaInputReader",
                "Death_Floor_Hazard"
            };

            foreach (var name in namesToClear)
            {
                var obj = GameObject.Find(name);
                if (obj != null)
                {
                    Undo.DestroyObjectImmediate(obj);
                }
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("<b>[Zelda Showcase]</b> Playground cleared from scene.");
        }

        [MenuItem("Zelda/Create UI Panel Settings Asset", false, 12)]
        public static void CreatePanelSettingsAsset()
        {
            if (!System.IO.Directory.Exists("Assets/UI"))
            {
                System.IO.Directory.CreateDirectory("Assets/UI");
            }

            string path = "Assets/UI/ZeldaPanelSettings.asset";
            var existing = AssetDatabase.LoadAssetAtPath<UnityEngine.UIElements.PanelSettings>(path);
            if (existing == null)
            {
                var settings = ScriptableObject.CreateInstance<UnityEngine.UIElements.PanelSettings>();
                settings.scaleMode = UnityEngine.UIElements.PanelScaleMode.ScaleWithScreenSize;
                settings.referenceResolution = new Vector2Int(1920, 1080);
                settings.match = 0.5f;

                AssetDatabase.CreateAsset(settings, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Selection.activeObject = settings;
                Debug.Log($"<b>[Zelda Showcase]</b> Created PanelSettings asset at: {path}");
            }
            else
            {
                Selection.activeObject = existing;
                Debug.Log($"<b>[Zelda Showcase]</b> PanelSettings asset already exists at: {path}");
            }
        }
    }
}
#endif
