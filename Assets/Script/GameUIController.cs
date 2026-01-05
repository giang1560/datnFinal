using UnityEngine;
using UnityEngine.UI;

public class GameUIController : MonoBehaviour
{
    [Header("References")]
    public MapCursor mapCursor;
    // ĐÃ XÓA: public CommandQueueManager queueManager; <--- Dòng này thừa
    
    [Header("Buttons")]
    public Button resetButton;

    void Start()
    {
        if (resetButton != null)
        {
            resetButton.onClick.AddListener(HandleResetClick);
        }
    }

    void Update()
    {
        if (resetButton != null) resetButton.interactable = true; 
    }

    void HandleResetClick()
    {
        if(mapCursor != null) mapCursor.ResetLevel();
    }
}