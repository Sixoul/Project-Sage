using UnityEngine;

namespace ZeldaOoT.Environment
{
    /// <summary>
    /// Ensures that whenever the game is played, the Zelda showcase playground,
    /// player, camera rig, and HUD are automatically initialized if not already present in the scene.
    /// Prevents duplicate spawns.
    /// </summary>
    public static class ZeldaAutoInitializer
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnSceneLoaded()
        {
            var existingPlayground = GameObject.Find("Zelda_Test_Playground");
            var existingPlayer = GameObject.Find("Player_Link");

            if (existingPlayground != null && existingPlayer != null)
            {
                return; // Everything is present!
            }

            Debug.Log("<b>[Zelda Showcase]</b> Initializing Test Playground on Play...");
            GameObject spawner = new GameObject("Playground_Spawner");
            var builder = spawner.AddComponent<ZeldaPlaygroundBuilder>();
            builder.BuildPlayground();
        }
    }
}
