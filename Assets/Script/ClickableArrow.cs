using UnityEngine;
using UnityEngine.EventSystems;

public class ClickableArrow : MonoBehaviour, IPointerClickHandler
{
    [HideInInspector]
    public TileCoord startTile;

    private PlayerInputController input;
    private GhostManager ghost;

    void Awake()
    {
        input = FindFirstObjectByType<PlayerInputController>();
        ghost = FindFirstObjectByType<GhostManager>();
    }

    public void Setup(TileCoord tile)
    {
        startTile = tile;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // ghost preview (optional)
        ghost?.ShowPreview(startTile);

        // gửi lệnh di chuyển
        input?.OnArrowClicked(startTile);
    }
}
