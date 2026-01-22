using Michsky.MUIP;
using UnityEngine;
using UnityEngine.UI;

public class MenuController : MonoBehaviour
{
    [SerializeField] private ButtonManager playBtn;
    [SerializeField] private ButtonManager editorBtn;
    [SerializeField] private ButtonManager exitBtn;

    /// <summary>
    /// Initializes button listeners.
    /// </summary>
    private void Start()
    {
        playBtn.onClick.AddListener(OnPlayButtonClicked);
        editorBtn.onClick.AddListener(OnEditorButtonClicked);
        exitBtn.onClick.AddListener(OnExitButtonClicked);
    }

    /// <summary>
    /// Handles the play button click event.
    /// </summary>
    private void OnPlayButtonClicked()
    {
        GameController.Instance.IsEditorMode = false;
        SceneController.Instance.LoadSceneAsync(SceneType.Gameplay);
    }

    /// <summary>
    /// Handles the editor button click event.
    /// </summary>
    private void OnEditorButtonClicked()
    {
        GameController.Instance.IsEditorMode = true;
        SceneController.Instance.LoadSceneAsync(SceneType.Editor);
    }

    /// <summary>
    /// Handles the exit button click event.
    /// </summary>
    private void OnExitButtonClicked()
    {
        Application.Quit();
    }
}
