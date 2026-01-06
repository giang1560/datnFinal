using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Chạy simulation trượt (Ice Sliding) với ring-based topology.
/// Xử lý tất cả các loại ô đặc biệt (Trap, Goal, OneWay, Teleport, Cracked, Sticky).
/// </summary>
public class RubikSimulator
{
    private readonly RubikMap map;

    public RubikSimulator(RubikMap map)
    {
        this.map = map;
    }

    // ─────────────────────────────────────────────
    //  MAIN SIMULATION
    // ─────────────────────────────────────────────
    public SimulationResult SimulateSlide(TileCoord startTile)
    {
        SimulationResult result = new SimulationResult
        {
            steps = new List<SimulationStep>(),
            stopReason = StopReason.None
        };

        TileCoord current = startTile;

        // ✅ Kiểm tra spawn trên ô đặc biệt (Trap/Goal)
        if (TryProcessInstantTile(result, current))
            return result;

        int safety = 0;
        const int MAX_STEPS = 150;

        while (safety++ < MAX_STEPS)
        {
            // 1. Tính ô tiếp theo (trượt thêm 1 bước)
            TileCoord next = RubikNavigator.StepForward(current);

            // 2. Nếu ra ngoài mặt → Chuyển sang mặt kế
            bool transitioned = false;
            if (!map.IsInsideFace(next))
            {
                next = RubikNavigator.TransitionAcrossEdge(current, next);
                transitioned = true;
            }

            // 3. Kiểm tra tường sau khi biết vị trí chính xác
            if (map.IsWall(next))
            {
                result.stopReason = StopReason.Wall;
                break;
            }

            // 4. ✅ FIX: Cập nhật current TRƯỚC KHI ghi step
            // Điều này đảm bảo delta được carry over đúng
            TileCoord previousTile = current;
            current = next;

            // 5. Ghi nhận bước đi
            SimulationStep step = new SimulationStep
            {
                coord = current,
                isFaceChange = transitioned,
                stepResult = StopReason.None
            };

            result.steps.Add(step);

            // 6. ✅ XỬ LÝ Ô ĐẶC BIỆT
            TileCell cell = map.GetTileCell(current);
            if (cell == null)
            {
                result.stopReason = StopReason.Wall;
                break;
            }

            switch (cell.Type)
            {
                // ──────────────────────────────────
                // TRAP: Game Over ngay lập tức
                // ──────────────────────────────────
                case TileType.Trap:
                    step.stepResult = StopReason.Trap;
                    result.stopReason = StopReason.Trap;
                    return result;

                // ──────────────────────────────────
                // GOAL: Thắng ngay lập tức
                // ──────────────────────────────────
                case TileType.Goal:
                    step.stepResult = StopReason.Goal;
                    result.stopReason = StopReason.Goal;
                    return result;

                // ──────────────────────────────────
                // STICKY: Dừng lại ngay
                // ──────────────────────────────────
                case TileType.Sticky:
                    step.stepResult = StopReason.Sticky;
                    result.stopReason = StopReason.Sticky;
                    return result;

                // ──────────────────────────────────
                // ONEWAY: Đổi hướng theo ô chỉ định
                // ──────────────────────────────────
                case TileType.OneWay:
                    // ✅ Đổi delta theo hướng OneWay
                    current.localDelta = DirectionToDelta(cell.specialData.oneWayDirection);
                    step.stepResult = StopReason.OneWayForced;
                    continue;

                // ──────────────────────────────────
                // TELEPORT: Dịch chuyển sang ô khác
                // ──────────────────────────────────
                case TileType.Teleport:
                {
                    TileCoord destination = map.GetTeleportDestination(current);

                    // Nếu không tìm thấy đích (hoặc đích = chính nó) → Dừng
                    if (destination.face == current.face &&
                        destination.x == current.x &&
                        destination.y == current.y)
                    {
                        result.stopReason = StopReason.Sticky;
                        return result;
                    }

                    // Ghi nhận bước teleport
                    SimulationStep teleStep = new SimulationStep
                    {
                        coord = destination,
                        isFaceChange = (destination.face != current.face),
                        stepResult = StopReason.Teleported
                    };

                    result.steps.Add(teleStep);

                    // ✅ Vô hiệu hóa cặp teleport (xóa khỏi dictionary)
                    map.DisableTeleportPair(current);

                    // ✅ Chuyển cả 2 ô thành Floor
                    TileCell sourceCell = map.GetTileCell(current);
                    TileCell destCell = map.GetTileCell(destination);
                    
                    if (sourceCell != null) sourceCell.Type = TileType.Floor;
                    if (destCell != null) destCell.Type = TileType.Floor;

                    // Cập nhật vị trí và giữ nguyên delta
                    current = new TileCoord(
                        destination.face,
                        destination.x,
                        destination.y,
                        current.localDelta // Giữ nguyên hướng trượt
                    );

                    continue;
                }

                // ──────────────────────────────────
                // CRACKED: Giảm độ bền, tiếp tục trượt
                // ──────────────────────────────────
                case TileType.Cracked:
                    // ✅ Xử lý Cracked Tile
                    step.stepResult = StopReason.None;
                    // Tiếp tục trượt (không dừng)
                    continue;

                // ──────────────────────────────────
                // FLOOR: Tiếp tục trượt
                // ──────────────────────────────────
                case TileType.Floor:
                    continue;
            }
        }

        // Nếu vòng lặp kết thúc mà chưa có stopReason → Coi như va tường
        if (result.stopReason == StopReason.None)
        {
            result.stopReason = StopReason.Wall;
        }

        return result;
    }

    // ─────────────────────────────────────────────
    //  HELPER: Xử lý ô đặc biệt ngay tại vị trí spawn
    // ─────────────────────────────────────────────
    bool TryProcessInstantTile(SimulationResult result, TileCoord tile)
    {
        TileCell cell = map.GetTileCell(tile);
        if (cell == null) return false;

        // Nếu spawn trên Trap → Thua ngay
        if (cell.Type == TileType.Trap)
        {
            result.steps.Add(new SimulationStep
            {
                coord = tile,
                stepResult = StopReason.Trap
            });
            result.stopReason = StopReason.Trap;
            return true;
        }

        // Nếu spawn trên Goal → Thắng ngay
        if (cell.Type == TileType.Goal)
        {
            result.steps.Add(new SimulationStep
            {
                coord = tile,
                stepResult = StopReason.Goal
            });
            result.stopReason = StopReason.Goal;
            return true;
        }

        return false;
    }

    // ─────────────────────────────────────────────
    //  HELPER: Chuyển Direction → Vector2Int
    // ─────────────────────────────────────────────
    private Vector2Int DirectionToDelta(Direction direction)
    {
        switch (direction)
        {
            case Direction.Up:    return new Vector2Int(0, 1);
            case Direction.Down:  return new Vector2Int(0, -1);
            case Direction.Left:  return new Vector2Int(-1, 0);
            case Direction.Right: return new Vector2Int(1, 0);
        }
        return Vector2Int.zero;
    }
}