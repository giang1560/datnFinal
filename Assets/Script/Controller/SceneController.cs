using UnityEngine;

public class SceneController : Singleton<SceneController>
{
    [SerializeField,EnumBind(typeof(SceneType))] private string[] sceneNames;

    /// <summary>
    /// Loads a scene asynchronously based on the SceneType enum.
    /// </summary>
    /// <param name="sceneType"></param>
    public void LoadSceneAsync(SceneType sceneType)
    {
        string sceneName = sceneNames[(int)sceneType];
        UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);
    }
}

public enum SceneType
{
    Loading,
    MainMenu,
    Gameplay,
    Editor
}
