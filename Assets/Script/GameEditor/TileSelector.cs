using UnityEngine;

public class TileSelector : MonoBehaviour
{
    [Header("Layer Mask for Tile Cells")]
    [SerializeField]
    private LayerMask tileCellLayerMask;
    private TileCell selectedTileCell;

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
        if (Input.GetMouseButton(0))
        {
            selectedTileCell = GetTileCellUnderCursor(Camera.main);
            if (selectedTileCell != null)
            {
                Debug.Log($"Selected Tile Cell at Face: {selectedTileCell.face}, X: {selectedTileCell.x}, Y: {selectedTileCell.y}, Type: {selectedTileCell.Type}");
            }
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
