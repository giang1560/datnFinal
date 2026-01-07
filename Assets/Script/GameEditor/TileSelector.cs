using Michsky.MUIP;
using UnityEngine;
using UnityEngine.UI;

public class TileSelector : MonoBehaviour
{
    [Header("Layer Mask for Tile Cells")]
    [SerializeField]
    private LayerMask tileCellLayerMask;

    [Header("Selected Dimmed Color")]
    [SerializeField] private Color32 selectedDimmedColor = new Color32(128, 128, 128, 255);
    private TileCell selectedTileCell;

    [Header("References")]
    [SerializeField] private HorizontalSelector horizontalSelector;
    [SerializeField] private ButtonManager exportButton;
    [SerializeField] private RubikMap rubikMap;
    [SerializeField] private GameObject gameUI;
    private LevelConfig currentLevelConfig;

    private void Awake()
    {
        horizontalSelector.onButtonClick = SelectTileType;
        exportButton.onClick.AddListener(ExportMapData);
    }

    void OnEnable()
    {
        gameUI.SetActive(false);
    }

    void OnDisable()
    {
        gameUI.SetActive(true);
    }

    public TileCell GetTileCellUnderCursor(Camera cam)
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hitInfo, Mathf.Infinity, tileCellLayerMask))
        {
            return hitInfo.collider.GetComponent<TileCell>();
        }
        return null;
    }

    private void Update()
    {
        if(!GameController.Instance.IsEditorMode)
            return;
        if (Input.GetMouseButton(0))
        {
            selectedTileCell = GetTileCellUnderCursor(Camera.main) ?? selectedTileCell;
            if (selectedTileCell != null)
            {
                Debug.Log($"Selected Tile Cell at Face: {selectedTileCell.face}, X: {selectedTileCell.x}, Y: {selectedTileCell.y}, Type: {selectedTileCell.Type}");
                UpdateUISelection();
            }
        }

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            if (selectedTileCell != null)
            {
                selectedTileCell.Type = selectedTileCell.Type.Next();
                Debug.Log($"Changed Tile Cell Type to: {selectedTileCell.Type}");
                UpdateUISelection();
            }
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            if (selectedTileCell != null)
            {
                selectedTileCell.Type = selectedTileCell.Type.Previous();
                Debug.Log($"Changed Tile Cell Type to: {selectedTileCell.Type}");
                UpdateUISelection();
            }
        }
    }

    private void UpdateUISelection()
    {
        if (selectedTileCell != null && horizontalSelector != null)
        {
            int typeIndex = (int)selectedTileCell.Type;
            Debug.Log($"Updating UI Selector to TileType Index: {typeIndex}");
            Debug.Log($"Current UI Selector Index: {horizontalSelector.index}");
            bool dir = typeIndex > horizontalSelector.index;
            while (horizontalSelector.index != typeIndex)
            {
                if (dir)
                    horizontalSelector.NextItem();
                else
                    horizontalSelector.PreviousItem();

                Debug.Log($"Updating UI Selector to TileType Index: {typeIndex}");
                Debug.Log($"Current UI Selector Index: {horizontalSelector.index}");
            }
        }
    }

    public void SelectTileType(int typeIndex)
    {
        if (selectedTileCell != null)
        {
            TileType newType = (TileType)typeIndex;
            Debug.Log($"Changing Tile Cell Type from {selectedTileCell.Type} to {newType}");
            selectedTileCell.Type = newType;
            Debug.Log($"Changed Tile Cell Type to: {selectedTileCell.Type}");
        }
    }

    /// <summary>
    /// Xuất dữ liệu bản đồ hiện tại ra LevelConfig và in ra console
    /// </summary>
    [ContextMenu("Export Map Data")]
    public void ExportMapData()
    {
        if (rubikMap != null)
        {
            LevelConfig config = rubikMap.ExportLevelConfig();
            currentLevelConfig = config;

            // As json
            // Debug.Log("Exported Level Config:");
            // Debug.Log(JsonUtility.ToJson(config, true));

            // Save to clipboard as csv row to facilitate pasting into spreadsheets
            string csvRow = config.ToCSVRow();
            GUIUtility.systemCopyBuffer = csvRow;
            Debug.Log("Copied CSV row to clipboard: " + csvRow);
        }
    }

    private void OnDrawGizmos()
    {
        if (selectedTileCell != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(selectedTileCell.transform.position, Vector3.one * 1.1f);
        }
    }
}

public static class EnumExtensions
{
    public static T Next<T>(this T src) where T : System.Enum
    {
        System.Array arr = System.Enum.GetValues(src.GetType());
        int j = System.Array.IndexOf(arr, src) + 1;
        return (T)(arr.GetValue(j % arr.Length));
    }

    public static T Previous<T>(this T src) where T : System.Enum
    {
        System.Array arr = System.Enum.GetValues(src.GetType());
        int j = System.Array.IndexOf(arr, src) - 1;
        if (j < 0) j = arr.Length - 1;
        return (T)(arr.GetValue(j));
    }
}
