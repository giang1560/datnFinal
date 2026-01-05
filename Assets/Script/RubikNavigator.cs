using UnityEngine;

/// <summary>
/// Điều hướng di chuyển giữa các mặt Rubik
/// (dựa trên bảng topology chuẩn).
/// </summary>
public class RubikNavigator : MonoBehaviour
{
    public int size;
    private int max;

    // bảng topology sinh từ builder
    private EdgeTransition[,] topology;

    void Awake()
    {
        max = size - 1;
        topology = RubikTopologyBuilder.BuildStandardCrossTopology();
    }

    // ---------------------------------------------------------
    //  STEP 1 — trượt 1 bước trong cùng mặt
    // ---------------------------------------------------------
    public TileCoord StepForward(TileCoord tile)
    {
        Vector2Int delta = tile.localDelta;

        if (delta == Vector2Int.zero)
            delta = new Vector2Int(0, 1);

        return new TileCoord(
            tile.face,
            tile.x + delta.x,
            tile.y + delta.y,
            delta
        );
    }

    // ---------------------------------------------------------
    //  STEP 2 — vượt mép sang mặt khác (TOPOLOGY) — ✅ FIXED
    // ---------------------------------------------------------
    public TileCoord TransitionAcrossEdge(TileCoord from, TileCoord outOfBounds)
    {
        // 1️⃣ xác định hướng thoát
        Direction exitDir = DetectDirection(from, outOfBounds);

        // 2️⃣ lấy rule từ bảng topology
        EdgeTransition t = topology[(int)from.face, (int)exitDir];

        // 3️⃣ ✅ FIX: Tính transversal value (giá trị chạy dọc cạnh)
        // - Nếu thoát qua UP/DOWN → transversal = x (chạy ngang)
        // - Nếu thoát qua LEFT/RIGHT → transversal = y (chạy dọc)
        int transversal = (exitDir == Direction.Up || exitDir == Direction.Down) 
                          ? from.x 
                          : from.y;

        // 4️⃣ Flip transversal nếu cần (mirror effect)
        if (t.flipCoordinate)
        {
            transversal = max - transversal;
        }

        // 5️⃣ ✅ FIX: Tính tọa độ mới dựa trên entrySide
        int nx = 0, ny = 0;

        switch (t.entrySide)
        {
            case EdgeSide.Top:    // Vào từ cạnh TRÊN → y = max
                nx = transversal;
                ny = max;
                break;

            case EdgeSide.Bottom: // Vào từ cạnh DƯỚI → y = 0
                nx = transversal;
                ny = 0;
                break;

            case EdgeSide.Left:   // Vào từ cạnh TRÁI → x = 0
                nx = 0;
                ny = transversal;
                break;

            case EdgeSide.Right:  // Vào từ cạnh PHẢI → x = max
                nx = max;
                ny = transversal;
                break;
        }

        // 6️⃣ Quán tính mới
        Vector2Int delta = DirectionToDelta(t.newMoveDirection);

        return new TileCoord(t.nextFace, nx, ny, delta);
    }

    // ---------------------------------------------------------
    //  Detect hướng thoát khỏi mặt
    // ---------------------------------------------------------
    private Direction DetectDirection(TileCoord from, TileCoord exit)
    {
        if      (exit.y > max) return Direction.Up;
        else if (exit.y < 0)   return Direction.Down;
        else if (exit.x > max) return Direction.Right;
        else                   return Direction.Left;
    }

    // ---------------------------------------------------------
    //  Convert Direction → delta vector
    // ---------------------------------------------------------
    private Vector2Int DirectionToDelta(Direction d)
    {
        switch (d)
        {
            case Direction.Up:    return new Vector2Int(0, 1);
            case Direction.Down:  return new Vector2Int(0, -1);
            case Direction.Left:  return new Vector2Int(-1, 0);
            case Direction.Right: return new Vector2Int(1, 0);
        }
        return Vector2Int.zero;
    }

    // ---------------------------------------------------------
    public bool IsInBounds(TileCoord t)
    {
        return t.x >= 0 && t.x <= max &&
               t.y >= 0 && t.y <= max;
    }
}