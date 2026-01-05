using UnityEngine;
using System.Collections.Generic;

public class ArrowVisibilityManager : MonoBehaviour
{
    [Header("References")]
    public GameObject arrowPrefab;
    public MapCursor cursor;
    public RubikMap rubikMap;
    public RubikNavigator navigator;
    
    [Header("Settings")]
    public float arrowHeightOffset = 0.2f;
    public float edgeArrowDistance = 1f;

    private readonly List<GameObject> activeArrows = new();

    void OnEnable()
    {
        SpawnArrows();
    }

    public void ClearArrows()
    {
        foreach (var a in activeArrows)
            Destroy(a);

        activeArrows.Clear();
    }

    public void SpawnArrows()
    {
        ClearArrows();

        TileCoord player = cursor.CurrentTile;

        Vector2Int[] deltas =
        {
            new Vector2Int(0, 1),   // up
            new Vector2Int(1, 0),   // right
            new Vector2Int(0,-1),   // down
            new Vector2Int(-1,0)    // left
        };

        foreach (var d in deltas)
            TrySpawnArrow(player, d);
    }

    void TrySpawnArrow(TileCoord player, Vector2Int delta)
    {
        // ✅ BƯỚC 1: Mô phỏng 1 bước trượt để tìm vị trí THỰC SỰ player sẽ đến
        TileCoord simulatedDest = SimulateOneSlideStep(player, delta);
        
        // Nếu simulation trả về vị trí không hợp lệ → Không spawn arrow
        if (simulatedDest.face == (FaceID)(-1))
            return;

        // ✅ BƯỚC 2: Kiểm tra xem có chuyển mặt không
        bool isTransition = (simulatedDest.face != player.face);

        // ✅ BƯỚC 3: Spawn arrow tại vị trí phù hợp
        if (isTransition)
        {
            // Arrow ở cạnh (khi chuyển mặt)
            CreateArrowAtEdge(player, simulatedDest, delta);
        }
        else
        {
            // Arrow trên tile đích (cùng mặt)
            TileCell destCell = rubikMap.GetTileCell(simulatedDest);
            if (destCell != null)
            {
                CreateArrowOnTile(player, delta, destCell.transform.position, destCell.transform.rotation);
            }
        }
    }

    // ─────────────────────────────────────────────
    // ✅ MÔ PHỎNG 1 BƯỚC TRƯỢT (Tìm ô đầu tiên player đến)
    // ─────────────────────────────────────────────
    TileCoord SimulateOneSlideStep(TileCoord start, Vector2Int delta)
    {
        TileCoord current = new TileCoord(start.face, start.x, start.y, delta);
        TileCoord next = navigator.StepForward(current);
        
        if (!rubikMap.IsInsideFace(next))
        {
            next = navigator.TransitionAcrossEdge(current, next);
        }
        
        if (rubikMap.IsWall(next))
        {
            return new TileCoord((FaceID)(-1), -1, -1);
        }
        
        return next;
    }

    // ─────────────────────────────────────────────
    // ✅ TẠO ARROW TRÊN TILE (Trường hợp bình thường)
    // ─────────────────────────────────────────────
    void CreateArrowOnTile(TileCoord playerPos, Vector2Int originalDelta, Vector3 tileWorldPos, Quaternion tileRotation)
    {
        Vector3 arrowPos = tileWorldPos + tileRotation * Vector3.up * arrowHeightOffset;

        GameObject arrow = Instantiate(
            arrowPrefab,
            arrowPos,
            tileRotation,
            transform
        );

        var clickable = arrow.GetComponent<ClickableArrow>();
        if (clickable != null)
        {
            TileCoord startWithDelta = new TileCoord(
                playerPos.face,
                playerPos.x,
                playerPos.y,
                originalDelta
            );
            clickable.Setup(startWithDelta);
        }

        activeArrows.Add(arrow);
    }

    // ─────────────────────────────────────────────
    // ✅ TẠO ARROW Ở CẠNH TILE (Khi chuyển mặt) - FIXED V2
    // ─────────────────────────────────────────────
    void CreateArrowAtEdge(TileCoord playerTile, TileCoord targetTile, Vector2Int originalDelta)
    {
        TileCell playerCell = rubikMap.GetTileCell(playerTile);
        if (playerCell == null) return;

        Vector3 playerWorldPos = playerCell.transform.position;

        // ✅ FIX: Tìm tile lân cận trong cùng face để tính direction
        Vector3 edgeDirection = GetDirectionFromNeighbor(playerTile, originalDelta);

        // Arrow spawn ở cạnh theo hướng này
        float tileSize = rubikMap.tileSize;
        Vector3 edgePosition = playerWorldPos + edgeDirection * (tileSize * edgeArrowDistance);

        // Arrow nổi lên trên bề mặt
        Vector3 arrowPos = edgePosition + playerCell.transform.up * arrowHeightOffset;

        GameObject arrow = Instantiate(
            arrowPrefab,
            arrowPos,
            playerCell.transform.rotation,
            transform
        );

        var clickable = arrow.GetComponent<ClickableArrow>();
        if (clickable != null)
        {
            TileCoord startWithDelta = new TileCoord(
                playerTile.face,
                playerTile.x,
                playerTile.y,
                originalDelta
            );
            clickable.Setup(startWithDelta);
        }

        activeArrows.Add(arrow);
    }

    // ─────────────────────────────────────────────
    // ✅ HÀM MỚI: Tính direction dựa trên tile lân cận
    // ─────────────────────────────────────────────
    Vector3 GetDirectionFromNeighbor(TileCoord baseTile, Vector2Int delta)
    {
        TileCell baseCell = rubikMap.GetTileCell(baseTile);
        if (baseCell == null) return Vector3.forward;

        Vector3 basePos = baseCell.transform.position;

        // Thử tìm tile lân cận trong cùng face
        TileCoord neighborCoord = new TileCoord(
            baseTile.face,
            baseTile.x + delta.x,
            baseTile.y + delta.y
        );

        // Nếu lân cận vẫn trong bounds → Dùng vị trí thực
        if (rubikMap.IsInsideFace(neighborCoord))
        {
            TileCell neighborCell = rubikMap.GetTileCell(neighborCoord);
            if (neighborCell != null)
            {
                return (neighborCell.transform.position - basePos).normalized;
            }
        }

        // Nếu không có lân cận (ra khỏi mặt) → Dùng tile đối diện để suy luận
        // Ví dụ: Nếu delta = (1,0), tìm tile (-1,0) để biết hướng X
        // Nếu delta = (0,1), tìm tile (0,-1) để biết hướng Y

        Vector2Int oppositeDelta = -delta;
        TileCoord oppositeCoord = new TileCoord(
            baseTile.face,
            baseTile.x + oppositeDelta.x,
            baseTile.y + oppositeDelta.y
        );

        if (rubikMap.IsInsideFace(oppositeCoord))
        {
            TileCell oppositeCell = rubikMap.GetTileCell(oppositeCoord);
            if (oppositeCell != null)
            {
                // Direction ngược lại
                return (basePos - oppositeCell.transform.position).normalized;
            }
        }

        // Fallback: Dùng local space (có thể sai với một số face)
        Vector3 localDir = new Vector3(delta.x, 0, delta.y);
        return baseCell.transform.TransformDirection(localDir).normalized;
    }
}