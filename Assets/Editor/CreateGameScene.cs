using EscapeTheDark;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CreateGameScene
{
    [MenuItem("Escape The Dark/Criar cena jogavel")]
    public static void Create()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        new GameObject("Escape The Dark").AddComponent<GameDirector>();
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Game.unity");
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/Game.unity", true) };
        AssetDatabase.SaveAssets();
        Debug.Log("Escape The Dark: cena Game criada e adicionada ao Build Settings.");
    }
}
