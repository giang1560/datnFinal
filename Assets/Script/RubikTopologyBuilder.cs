using UnityEngine;

public static class RubikTopologyBuilder
{
    public static EdgeTransition[,] BuildStandardCrossTopology()
    {
        EdgeTransition[,] table = new EdgeTransition[6, 4];

        // Helper function để định nghĩa transition
        void Link(FaceID from, Direction dir, FaceID to, EdgeSide entry, Direction move, bool flip)
        {
            table[(int)from, (int)dir] = new EdgeTransition 
            { 
                nextFace = to, 
                entrySide = entry, 
                newMoveDirection = move, 
                flipCoordinate = flip 
            };
        }

        // ============================================================
        // TOPOLOGY CỦA RUBIK CUBE CHUẨN (Matrix System)
        // Convention: Y tăng = lên trên, Y giảm = xuống dưới
        // ============================================================

        // --- 1. FRONT FACE ---
        // Up: Ra khỏi Front(y=max) → Vào Top từ Bottom(y=0) → Tiếp tục Up
        Link(FaceID.Front, Direction.Up,    FaceID.Top,    EdgeSide.Bottom, Direction.Up,    false);
        
        // Down: Ra khỏi Front(y=0) → Vào Bottom từ Top(y=max) → Tiếp tục Down
        Link(FaceID.Front, Direction.Down,  FaceID.Bottom, EdgeSide.Top,    Direction.Down,  false);
        
        // Left: Ra khỏi Front(x=0) → Vào Left từ Right(x=max) → Tiếp tục Left
        Link(FaceID.Front, Direction.Left,  FaceID.Left,   EdgeSide.Right,  Direction.Left,  false);
        
        // Right: Ra khỏi Front(x=max) → Vào Right từ Left(x=0) → Tiếp tục Right
        Link(FaceID.Front, Direction.Right, FaceID.Right,  EdgeSide.Left,   Direction.Right, false);

        // --- 2. TOP FACE ---
        // Up: Ra khỏi Top(y=max) → Vào Back từ Top(y=max) → ĐẢO HƯỚNG thành Down (vì Back lật 180°)
        Link(FaceID.Top, Direction.Up,    FaceID.Back,  EdgeSide.Top,    Direction.Down,  true);
        
        // Down: Ra khỏi Top(y=0) → Vào Front từ Top(y=max) → Tiếp tục Down
        Link(FaceID.Top, Direction.Down,  FaceID.Front, EdgeSide.Top,    Direction.Down, false);
        
        // Left: Ra khỏi Top(x=0) → Vào Left từ Top(y=max) → Chuyển sang Down (xoay 90°)
        Link(FaceID.Top, Direction.Left,  FaceID.Left,  EdgeSide.Top,    Direction.Down, true);
        
        // Right: Ra khỏi Top(x=max) → Vào Right từ Top(y=max) → Chuyển sang Down (xoay 90°)
        Link(FaceID.Top, Direction.Right, FaceID.Right, EdgeSide.Top,    Direction.Down, false);

        // --- 3. BOTTOM FACE ---
        // Up: Ra khỏi Bottom(y=max) → Vào Front từ Bottom(y=0) → Tiếp tục Up
        Link(FaceID.Bottom, Direction.Up,    FaceID.Front, EdgeSide.Bottom, Direction.Up,   false);
        
        // Down: Ra khỏi Bottom(y=0) → Vào Back từ Bottom(y=0) → ĐẢO HƯỚNG thành Up (vì Back lật 180°)
        Link(FaceID.Bottom, Direction.Down,  FaceID.Back,  EdgeSide.Bottom, Direction.Up,   true);
        
        // Left: Ra khỏi Bottom(x=0) → Vào Left từ Bottom(y=0) → Chuyển sang Up (xoay 90°)
        Link(FaceID.Bottom, Direction.Left,  FaceID.Left,  EdgeSide.Bottom, Direction.Up,   false);
        
        // Right: Ra khỏi Bottom(x=max) → Vào Right từ Bottom(y=0) → Chuyển sang Up (xoay 90°)
        Link(FaceID.Bottom, Direction.Right, FaceID.Right, EdgeSide.Bottom, Direction.Up,   true);

        // --- 4. LEFT FACE ---
        // Up: Ra khỏi Left(y=max) → Vào Top từ Left(x=0) → Chuyển sang Right (xoay 90°)
        Link(FaceID.Left, Direction.Up,    FaceID.Top,    EdgeSide.Left,   Direction.Right, true);
        
        // Down: Ra khỏi Left(y=0) → Vào Bottom từ Left(x=0) → Chuyển sang Right (xoay 90°)
        Link(FaceID.Left, Direction.Down,  FaceID.Bottom, EdgeSide.Left,   Direction.Right, false);
        
        // Left: Ra khỏi Left(x=0) → Vào Back từ Right(x=max) → Tiếp tục Left
        Link(FaceID.Left, Direction.Left,  FaceID.Back,   EdgeSide.Right,  Direction.Left,  false);
        
        // Right: Ra khỏi Left(x=max) → Vào Front từ Left(x=0) → Tiếp tục Right
        Link(FaceID.Left, Direction.Right, FaceID.Front,  EdgeSide.Left,   Direction.Right, false);

        // --- 5. RIGHT FACE ---
        // Up: Ra khỏi Right(y=max) → Vào Top từ Right(x=max) → Chuyển sang Left (xoay 90°)
        Link(FaceID.Right, Direction.Up,    FaceID.Top,    EdgeSide.Right,  Direction.Left,  false);
        
        // Down: Ra khỏi Right(y=0) → Vào Bottom từ Right(x=max) → Chuyển sang Left (xoay 90°)
        Link(FaceID.Right, Direction.Down,  FaceID.Bottom, EdgeSide.Right,  Direction.Left,  true);
        
        // Left: Ra khỏi Right(x=0) → Vào Front từ Right(x=max) → Tiếp tục Left
        Link(FaceID.Right, Direction.Left,  FaceID.Front,  EdgeSide.Right,  Direction.Left,  false);
        
        // Right: Ra khỏi Right(x=max) → Vào Back từ Left(x=0) → Tiếp tục Right
        Link(FaceID.Right, Direction.Right, FaceID.Back,   EdgeSide.Left,   Direction.Right, false);

        // --- 6. BACK FACE ---
        // QUAN TRỌNG: Back được lật 180° trong hierarchy
        // Cạnh trên Back(y=max) nối với cạnh trên Top(y=max)
        // Cạnh dưới Back(y=0) nối với cạnh dưới Bottom(y=0)
        
        // Up: Ra khỏi Back(y=max) → Vào Top từ Top(y=max) → ĐẢO HƯỚNG thành Down
        Link(FaceID.Back, Direction.Up,    FaceID.Top,    EdgeSide.Top,    Direction.Down,  true);
        
        // Down: Ra khỏi Back(y=0) → Vào Bottom từ Bottom(y=0) → ĐẢO HƯỚNG thành Up
        Link(FaceID.Back, Direction.Down,  FaceID.Bottom, EdgeSide.Bottom, Direction.Up,    true);
        
        // Left: Ra khỏi Back(x=0) → Vào Right từ Right(x=max) → Tiếp tục Left
        Link(FaceID.Back, Direction.Left,  FaceID.Right,  EdgeSide.Right,  Direction.Left,  false);
        
        // Right: Ra khỏi Back(x=max) → Vào Left từ Left(x=0) → Tiếp tục Right
        Link(FaceID.Back, Direction.Right, FaceID.Left,   EdgeSide.Left,   Direction.Right, false);

        return table;
    }
}

/*
 * GIẢI THÍCH TOPOLOGY:
 * 
 * Khối Rubik được "mở ra" theo dạng chữ thập (Cross Layout):
 * 
 *           [TOP]
 *            (1)
 *    [LEFT] [FRONT] [RIGHT] [BACK - Lật 180°]
 *     (4)     (0)      (5)      (2)
 *          [BOTTOM]
 *            (3)
 * 
 * ⚠️ QUAN TRỌNG: Mặt BACK được lật 180° trong Unity hierarchy!
 * - Cạnh TRÊN của BACK (y=max) nối với cạnh TRÊN của TOP (y=max)
 * - Cạnh DƯỚI của BACK (y=0) nối với cạnh DƯỚI của BOTTOM (y=0)
 * - Khi từ TOP/BOTTOM chuyển sang BACK → Hướng di chuyển bị ĐẢO NGƯỢC
 * 
 * - Matrix System: Y=0 là dưới, Y=Max là trên
 * - Khi nhân vật trượt ra khỏi cạnh một mặt, nó vào mặt kế bên
 * - entrySide xác định cạnh nào của mặt mới đón nhân vật
 * - newMoveDirection xác định hướng quán tính sau khi chuyển mặt
 * - flipCoordinate = true khi cần đảo ngược trục (mirror effect)
 */