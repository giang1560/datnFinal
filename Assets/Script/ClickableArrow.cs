using UnityEngine;
using UnityEngine.EventSystems;

public class ClickableArrow : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler
{
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

    public void OnPointerEnter(PointerEventData eventData)
    {
        ghost?.ShowPreview(startTile);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ghost?.HideGhost();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        ghost?.HideGhost();
        input?.OnArrowClicked(startTile);
    }
}
