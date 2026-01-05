using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameUIManager : MonoBehaviour
{
    [Header("UI References")]
    public MapCursor mapCursor;
    public GameObject winPanel;
    public GameObject losePanel;
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
        winPanel.SetActive(true);
        losePanel.SetActive(false);
    }

    public void ShowLose(StopReason reason)
    {
        winPanel.SetActive(false);
        losePanel.SetActive(true);
        if (loseReasonText != null)
        {
            switch (reason)
            {
                case StopReason.Trap: loseReasonText.text = "THẤT BẠI!\nBạn đã rơi vào bẫy."; break;
                case StopReason.OutOfMoves: loseReasonText.text = "THẤT BẠI!\nHết lượt đi."; break;
                default: loseReasonText.text = "GAME OVER"; break;
            }
        }
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
        HideAllPanels();
        if (mapCursor != null) mapCursor.ResetLevel();
    }
}