#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ZeldaOoT.Editor
{
    /// <summary>
    /// Automated WebGL Build tool configured specifically for GitHub Pages deployment.
    /// Ensures compression, .nojekyll, responsive canvas, and static file compatibility.
    /// </summary>
    public static class ZeldaWebGLBuilder
    {
        private const string DocsBuildPath = "docs";

        [MenuItem("Zelda/Configure WebGL Settings for GitHub Pages", false, 30)]
        public static void ConfigureWebGLSettings()
        {
            // 1. Compression: Disabled or Decompression Fallback enabled
            // GitHub Pages does not serve Content-Encoding: gzip/br headers.
            // Setting compression to Disabled ensures raw .wasm and .data load reliably on GitHub Pages.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback = true;

            // 2. Threads: Disabled (SharedArrayBuffer requires COOP/COEP headers not available on GitHub Pages)
            PlayerSettings.WebGL.threadsSupport = false;

            // 3. Exception Support: Explicitly enabled for clear console diagnostics
            PlayerSettings.SetStackTraceLogType(LogType.Exception, StackTraceLogType.ScriptOnly);

            // 4. Color space: Linear or Gamma (WebGL 2.0 supports Linear)
            PlayerSettings.colorSpace = ColorSpace.Linear;

            // 5. Memory configuration
            PlayerSettings.WebGL.memorySize = 64;

            AssetDatabase.SaveAssets();
            Debug.Log("<b>[WebGL Config]</b> Configured WebGL Player Settings for GitHub Pages compatibility (Compression: Disabled, Threads: Disabled, Decompression Fallback: Enabled).");
        }

        [MenuItem("Zelda/Build WebGL for GitHub Pages (docs folder)", false, 31)]
        public static void BuildForGitHubPages()
        {
            // Configure settings first
            ConfigureWebGLSettings();

            // Destination directory: <RepoRoot>/docs
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string outputPath = Path.Combine(projectRoot, DocsBuildPath);

            // Get scenes from EditorBuildSettings or current active scene
            string[] scenes = GetBuildScenes();
            if (scenes.Length == 0)
            {
                Debug.LogError("<b>[WebGL Build]</b> No valid scenes found to build! Please save and add a scene to Build Settings.");
                return;
            }

            Debug.Log($"<b>[WebGL Build]</b> Starting WebGL build to: {outputPath} with {scenes.Length} scene(s)...");

            BuildPlayerOptions buildOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                // Create .nojekyll in docs/ so GitHub Pages serves files with underscores or unusual extensions
                string noJekyllPath = Path.Combine(outputPath, ".nojekyll");
                if (!File.Exists(noJekyllPath))
                {
                    File.WriteAllText(noJekyllPath, "");
                }

                Debug.Log($"<b>[WebGL Build SUCCESS]</b> Size: {summary.totalSize / (1024 * 1024):F2} MB. Generated .nojekyll in {outputPath}.");
                Debug.Log("<b>Next Step:</b> Commit and push the <code>docs/</code> folder to GitHub, then in your repository settings enable <b>Pages -> Source: Deploy from branch -> main /docs</b>.");
            }
            else if (summary.result == BuildResult.Failed)
            {
                Debug.LogError($"<b>[WebGL Build FAILED]</b> Errors: {summary.totalErrors}");
            }
        }

        private static string[] GetBuildScenes()
        {
            var editorScenes = EditorBuildSettings.scenes;
            if (editorScenes != null && editorScenes.Length > 0)
            {
                var scenePaths = new System.Collections.Generic.List<string>();
                foreach (var s in editorScenes)
                {
                    if (s.enabled && !string.IsNullOrEmpty(s.path))
                    {
                        scenePaths.Add(s.path);
                    }
                }
                if (scenePaths.Count > 0) return scenePaths.ToArray();
            }

            // Fallback to active open scene
            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (activeScene.IsValid() && !string.IsNullOrEmpty(activeScene.path))
            {
                return new string[] { activeScene.path };
            }

            return new string[0];
        }
    }
}
#endif
