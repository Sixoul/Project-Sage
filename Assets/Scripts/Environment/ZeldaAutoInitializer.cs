using UnityEngine;
using ZeldaOoT.Kokiri;

namespace ZeldaOoT.Environment
{
    /// <summary>
    /// Ensures that whenever the game is played, Kokiri Forest and Inside the Great Deku Tree,
    /// player Link, Navi, camera rig, and HUD are automatically initialized if not already present in the scene.
    /// Prevents duplicate spawns.
    /// </summary>
    public static class ZeldaAutoInitializer
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnSceneLoaded()
        {
            var existingForest = GameObject.Find("Kokiri_Forest_World");
            var existingPlayground = GameObject.Find("Zelda_Test_Playground");
            var existingPlayer = GameObject.Find("Player_Link");

            if ((existingForest != null || existingPlayground != null) && existingPlayer != null)
            {
                return; // Everything is present!
            }

            Debug.Log("<b>[Zelda OoT]</b> Initializing Kokiri Forest & Inside the Great Deku Tree on Play...");
            GameObject spawner = new GameObject("KokiriForest_Spawner");
            var builder = spawner.AddComponent<KokiriForestBuilder>();
            builder.BuildKokiriForest();
        }
    }
}
