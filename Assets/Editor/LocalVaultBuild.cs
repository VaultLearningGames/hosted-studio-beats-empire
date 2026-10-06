using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;

// Local WebGL builds for tools/unity-build.sh (CI uses game-ci's own build method).
public static class LocalVaultBuild {
    // Opens each build scene so TextMesh Pro upgrades its font assets, and saves them, outside a build.
    public static void Prepare() {
        foreach (var s in EditorBuildSettings.scenes.Where(s => s.enabled)) {
            EditorSceneManager.OpenScene(s.path);
        }
        AssetDatabase.SaveAssets();
        UnityEngine.Debug.Log("LOCAL PREPARE DONE");
    }

    public static void Build() {
        string path = Path.GetFullPath("build/WebGL");
        string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        BuildReport r = BuildPipeline.BuildPlayer(scenes, path, BuildTarget.WebGL, BuildOptions.None);
        UnityEngine.Debug.Log("LOCAL BUILD RESULT: " + r.summary.result + " size " + r.summary.totalSize);
        if (r.summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
    }
}
