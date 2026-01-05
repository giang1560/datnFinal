using UnityEngine;
using System.Collections.Generic;

public class GhostManager : MonoBehaviour
{
    [Header("Settings")]
    public GameObject ghostPrefab;
    public RubikMap map;
    public RubikNavigator navigator;
    public MapCursor playerCursor;

    private GameObject ghostInstance;
    private RubikSimulator simulator;

    private bool isGhostEnabled = true;

    void Start()
    {
        if (map != null && navigator != null)
            simulator = new RubikSimulator(map, navigator);

        if (ghostPrefab != null)
        {
            ghostInstance = Instantiate(ghostPrefab);
            ghostInstance.SetActive(false);

            Collider col = ghostInstance.GetComponent<Collider>();
            if (col) col.enabled = false;
        }
    }
    
    public void SetGhostEnabled(bool isEnabled)
    {
        isGhostEnabled = isEnabled;
        if (!isEnabled) HideGhost();
    }

    /// <summary>
    /// Preview dựa trên ô MŨI TÊN (TileCoord), không còn Direction.
    /// </summary>
    public void ShowPreview(TileCoord startTile)
    {
        if (!isGhostEnabled || ghostInstance == null || simulator == null)
            return;

        ghostInstance.SetActive(true);

        SimulationResult result = simulator.SimulateSlide(startTile);

        // nếu simulator có path → lấy ô cuối
        if (result.steps != null && result.steps.Count > 0)
        {
            SimulationStep last = result.steps[result.steps.Count - 1];
            SnapGhostToCoord(last.coord);
        }
        else
        {
            // fallback: đứng ngay vị trí player
            SnapGhostToCoord(playerCursor.CurrentTile);
        }
    }

    public void HideGhost()
    {
        if (ghostInstance)
            ghostInstance.SetActive(false);
    }

    private void SnapGhostToCoord(TileCoord c)
    {
        TileCell cell = map.GetTileCell(c);
        if (cell == null) return;

        ghostInstance.transform.position = cell.transform.position;
        ghostInstance.transform.rotation = cell.transform.rotation;
    }
}
