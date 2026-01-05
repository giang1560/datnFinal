using UnityEngine;
using System.Collections.Generic;

// --- ENUMS ---
public enum FaceID { Front = 0, Top = 1, Back = 2, Bottom = 3, Left = 4, Right = 5 }
public enum Direction { Up, Down, Left, Right }

// ✅ CẬP NHẬT: Thêm các loại ô đặc biệt
public enum TileType 
{ 
    Floor,      // Ô thường
    Wall,       // Tường (dừng nhân vật)
    Trap,       // Bẫy (game over ngay lập tức)
    Sticky,     // Ô dính (dừng nhân vật ngay)
    Goal,       // Đích đến
    OneWay,     // Ô một chiều (bắt buộc đi theo hướng chỉ định)
    Teleport,   // Cổng dịch chuyển
    Cracked     // Ô nứt (đi qua 1 lần thì vỡ thành tường)
}

public enum StopReason 
{ 
    None,           // Chưa dừng
    Wall,           // Va tường
    Goal,           // Đến đích
    Trap,           // Rơi bẫy
    Sticky,         // Dính vào ô sticky
    OneWayForced,   // Bị ô one-way điều hướng
    Teleported,     // Đã dịch chuyển
    OutOfMoves      // Hết lượt
}

public enum EdgeSide { Top, Bottom, Left, Right }

// --- STRUCTS ---
[System.Serializable]
public struct TileCoord
{
    public FaceID face;
    public int x;
    public int y;

    public Vector2Int localDelta;

    public TileCoord(FaceID face, int x, int y)
    {
        this.face = face;
        this.x = x;
        this.y = y;
        this.localDelta = Vector2Int.zero;
    }

    public TileCoord(FaceID face, int x, int y, Vector2Int delta)
    {
        this.face = face;
        this.x = x;
        this.y = y;
        this.localDelta = delta;
    }

    public override string ToString()
    {
        return $"{face}({x},{y})";
    }
}


[System.Serializable]
public struct EdgeTransition
{
    public FaceID nextFace;
    public EdgeSide entrySide;
    public Direction newMoveDirection;
    public bool flipCoordinate;
}

[System.Serializable]
public struct SimulationStep
{
    public TileCoord coord;
    public bool isFaceChange;
    public StopReason stepResult;
    public Direction newDirection;
}

[System.Serializable]
public class SimulationResult
{
    public List<SimulationStep> steps = new List<SimulationStep>();
    public StopReason stopReason = StopReason.None;
}

// ✅ MỚI: Data cho ô đặc biệt
[System.Serializable]
public class TileSpecialData
{
    // Cho OneWay
    public Direction oneWayDirection = Direction.Up;
    
    // Cho Teleport (ID của ô teleport đích)
    public int teleportPairID = -1;
    
    // Cho Cracked Tile
    public int durability = 1; // Số lần đi qua trước khi vỡ
    public bool isBroken = false;
}