using UnityEngine;

public class PlayerInputController : MonoBehaviour
{
    public MapCursor cursor;

    public void OnArrowClicked(TileCoord startTile)
    {
        if (cursor != null && cursor.CanReceiveInput)
            cursor.MoveFromArrow(startTile);
    }
}
