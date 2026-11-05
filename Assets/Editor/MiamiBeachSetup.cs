#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// First time the project opens: creates Assets/Scenes/MiamiBeach.unity and adds it to Build Settings,
// so you can press PLAY straight away (and build for Android / iOS without extra setup).
[InitializeOnLoad]
public static class MiamiBeachSetup
{
    const string ScenePath = "Assets/Scenes/MiamiBeach.unity";

    static MiamiBeachSetup() { EditorApplication.delayCall += Run; }

    static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null) return;
        if (EditorSceneManager.GetActiveScene().isDirty) return;
        try
        {
            if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("[Miami Beach] Scene created: " + ScenePath + "  -  press PLAY.");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[Miami Beach] Could not create the scene automatically (" + e.Message + "). Open any scene and press PLAY.");
        }
    }
}
#endif
