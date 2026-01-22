using UnityEngine;
using UnityEngine.UI;

public class PauseUIPanel : MonoBehaviour
{
    [SerializeField] private Button continueBtn;
    [SerializeField] private Button resetBtn;
    [SerializeField] private Button settingBtn;
    [SerializeField] private Button exitToMenuBtn;

    private void Start()
    {
        continueBtn.onClick.AddListener(OnContinueClicked);
        resetBtn.onClick.AddListener(OnResetClicked);
        settingBtn.onClick.AddListener(OnSettingClicked);
        exitToMenuBtn.onClick.AddListener(OnExitToMenuClicked);
    }

    /// <summary>
    /// Xử lý khi nhấn nút tiếp tục chơi
    /// </summary>
    private void OnContinueClicked()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Xử lý khi nhấn nút đặt lại
    /// </summary>
    private void OnResetClicked()
    {
        // Thực hiện logic đặt lại trò chơi
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Xử lý khi nhấn nút cài đặt
    /// </summary>
    private void OnSettingClicked()
    {
        // Mở bảng cài đặt (nếu có)
        Debug.Log("Settings button clicked - functionality not implemented yet.");
    }

    /// <summary>
    /// Xử lý khi nhấn nút thoát về menu chính
    /// </summary>
    private void OnExitToMenuClicked()
    {
        GameController.Instance.IsEditorMode = false;
        SceneController.Instance.LoadSceneAsync(SceneType.MainMenu);
    }
}
