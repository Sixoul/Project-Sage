#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using ZeldaOoT.Environment;
using ZeldaOoT.Kokiri;

namespace ZeldaOoT.Editor
{
    /// <summary>
    /// Editor Menu utility to generate or reset Kokiri Forest, Inside the Great Deku Tree,
    /// or the Zelda Showcase Playground directly in the active scene.
    /// </summary>
    public static class ZeldaPlaygroundMenu
    {
        [MenuItem("Zelda/Build Kokiri Forest & Great Deku Tree", false, 1)]
        public static void BuildKokiriForestInScene()
        {
            ClearAllFromScene();

            GameObject spawnerObj = new GameObject("KokiriForest_Spawner");
            var builder = spawnerObj.AddComponent<KokiriForestBuilder>();
            builder.BuildKokiriForest();

            Undo.RegisterCreatedObjectUndo(spawnerObj, "Build Kokiri Forest & Great Deku Tree");
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            Debug.Log("<b>[Zelda OoT]</b> Successfully generated Kokiri Forest and Inside the Great Deku Tree with all puzzles, shop, Mido, enemies, and Queen Gohma!");
        }

        [MenuItem("Zelda/Build Showcase Test Playground", false, 10)]
        public static void BuildPlaygroundInScene()
        {
            ClearAllFromScene();

            GameObject spawnerObj = new GameObject("Playground_Spawner");
            var builder = spawnerObj.AddComponent<ZeldaPlaygroundBuilder>();
            builder.BuildPlayground();

            Undo.RegisterCreatedObjectUndo(spawnerObj, "Build Zelda Playground");
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            Debug.Log("<b>[Zelda Showcase]</b> Successfully generated Test Playground with Arena, Water Swimming Basin, Platforms, Combat Dummies, and UI Toolkit HUD!");
        }

        [MenuItem("Zelda/Clear World & Reset", false, 12)]
        public static void ClearAllFromScene()
        {
            string[] namesToClear = {
                "KokiriForest_Spawner",
                "Kokiri_Forest_World",
                "Dungeon_Inside_Great_Deku_Tree",
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
