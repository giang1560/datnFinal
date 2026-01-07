using UnityEngine;
using TMPro;
using Michsky.MUIP;
using DG.Tweening;

public class GameUIManager : Singleton<GameUIManager>
{
    [Header("UI References")]
    public MapCursor mapCursor;
    public RubikMap rubikMap;
    public GameObject winPanel;
    public GameObject losePanel;
    [SerializeField] private ModalWindowManager modalWindowManager;
    public TextMeshProUGUI loseReasonText;
    public TextMeshProUGUI moveCounterText;
    // ĐÃ XÓA: public GhostManager ghostManager;
    // ĐÃ XÓA: public Toggle ghostToggle;

    void Start()
    {
        HideAllPanels();
    }

    public void UpdateMoveCounter(int current, int max)
    {
        if (moveCounterText != null)
        {
            moveCounterText.text = $"MOVES: {current}/{max}";
            
            // Đổi màu đỏ nếu sắp hết lượt (ví dụ còn < 3)
            moveCounterText.color = (current <= 3) ? Color.red : Color.white;
        }
    }

    public void ShowWin()
    {
        modalWindowManager.onConfirm.RemoveAllListeners();
        modalWindowManager.onCancel.RemoveAllListeners();
        modalWindowManager.onConfirm.AddListener(OnNextLevelButton);
        modalWindowManager.onCancel.AddListener(OnRetryButton);
        modalWindowManager.onConfirm.AddListener(modalWindowManager.Close);
        modalWindowManager.onCancel.AddListener(modalWindowManager.Close);
        modalWindowManager.titleText = "LEVEL COMPLETE!";
        modalWindowManager.descriptionText = "Chúc mừng bạn đã hoàn thành cấp độ này!";
        modalWindowManager.confirmButton.SetText("Next Level");
        modalWindowManager.cancelButton.SetText("Retry");
        modalWindowManager.Open(false);
    }

    public void ShowLose(StopReason reason)
    {
        modalWindowManager.onConfirm.RemoveAllListeners();
        modalWindowManager.onConfirm.AddListener(OnRetryButton);
        modalWindowManager.onConfirm.AddListener(modalWindowManager.Close);
        modalWindowManager.titleText = "LEVEL FAILED!";
        switch (reason)
        {
            case StopReason.OutOfMoves:
                modalWindowManager.descriptionText = "Bạn đã hết lượt di chuyển!";
                break;
            case StopReason.Trap:
                modalWindowManager.descriptionText = "Bạn đã chạm vào ô bẫy!";
                break;
            default:
                modalWindowManager.descriptionText = "Bạn đã thất bại!";
                break;
        }
        modalWindowManager.confirmButton.SetText("Retry");
        modalWindowManager.Open(true);
    }

    public void HideAllPanels()
    {
        if(winPanel) winPanel.SetActive(false);
        if(losePanel) losePanel.SetActive(false);
    }

    public void OnRetryButton()
    {
        HideAllPanels();
        if (mapCursor != null) mapCursor.ResetLevel();
    }

    public void OnNextLevelButton()
    {
        DOVirtual.DelayedCall(0.5f, () =>
        {
            rubikMap.LoadNextLevelByTriggerButton();
        });
    }
}