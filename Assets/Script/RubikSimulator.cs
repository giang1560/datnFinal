using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Chạy simulation trượt (Ice Sliding) với ring-based topology.
/// ✅ FIXED: Cracked tile xử lý đúng - không cho đi xuyên qua ô đã vỡ
/// ✅ FIXED: Teleport không đổi type trong simulation - chỉ đổi khi player thực sự đến
/// </summary>
public class RubikSimulator
{
    private readonly RubikMap map;
    private bool needsRebuild = false;

    public RubikSimulator(RubikMap map)
    {
        this.map = map;
        map.OnTileChanged += HandleTileChanged;
    }

    public SimulationResult SimulateSlide(TileCoord startTile)
    {
        if (needsRebuild)
        {
            needsRebuild = false;
        }

        SimulationResult result = new SimulationResult
        {
            steps = new List<SimulationStep>(),
            stopReason = StopReason.None
        };

        TileCoord current = startTile;

        // ✅ TRACK DURABILITY CỦA CÁC Ô CRACKED TRONG SIMULATION
        Dictionary<string, int> crackedDurability = new Dictionary<string, int>();

        // ✅ Kiểm tra spawn trên ô đặc biệt (Trap/Goal)
        if (TryProcessInstantTile(result, current))
            return result;

        int safety = 0;
        const int MAX_STEPS = 150;

        while (safety++ < MAX_STEPS)
        {
            // ─────────────────────────────────────────────────────────
            // ✅ FIX 1: XỬ LÝ CRACKED TILE TRƯỚC KHI RỜI ĐI
            // ─────────────────────────────────────────────────────────
            TileCell currentCell = map.GetTileCell(current);
            if (currentCell != null && currentCell.Type == TileType.Cracked && !currentCell.specialData.isBroken)
            {
                string key = GetTileKey(current);
                
                // Lần đầu gặp → lấy durability gốc từ map
                if (!crackedDurability.ContainsKey(key))
                {
                    crackedDurability[key] = currentCell.specialData.durability;
                }
                
                // Giảm durability (vì player đang rời khỏi ô này)
                crackedDurability[key]--;
                
                Debug.Log($"[Simulation] Left Cracked {key}, durability now: {crackedDurability[key]}");
            }

            // ─────────────────────────────────────────────────────────
            // 1. Tính ô tiếp theo (trượt thêm 1 bước)
            // ─────────────────────────────────────────────────────────
            TileCoord next = RubikNavigator.StepForward(current);

            // 2. Nếu ra ngoài mặt → Chuyển sang mặt kế
            bool transitioned = false;
            if (!map.IsInsideFace(next))
            {
                next = RubikNavigator.TransitionAcrossEdge(current, next);
                transitioned = true;
            }

            // ─────────────────────────────────────────────────────────
            // ✅ FIX 2: KIỂM TRA BLOCK (bao gồm cả Cracked đã vỡ)
            // ─────────────────────────────────────────────────────────
            if (IsBlocked(next, crackedDurability))
            {
                result.stopReason = StopReason.Wall;
                break;
            }

            // 4. Cập nhật current
            current = next;

            // 5. Ghi nhận bước đi
            SimulationStep step = new SimulationStep
            {
                coord = current,
                isFaceChange = transitioned,
                stepResult = StopReason.None
            };

            result.steps.Add(step);

            // ─────────────────────────────────────────────────────────
            // 6. ✅ XỬ LÝ Ô ĐẶC BIỆT (trừ Cracked - đã xử lý ở đầu loop)
            // ─────────────────────────────────────────────────────────
            TileCell cell = map.GetTileCell(current);
            if (cell == null)
            {
                result.stopReason = StopReason.Wall;
                break;
            }

            switch (cell.Type)
            {
                case TileType.Trap:
                    step.stepResult = StopReason.Trap;
                    result.stopReason = StopReason.Trap;
                    return result;

                case TileType.Goal:
                    step.stepResult = StopReason.Goal;
                    result.stopReason = StopReason.Goal;
                    return result;

                case TileType.Sticky:
                    step.stepResult = StopReason.Sticky;
                    result.stopReason = StopReason.Sticky;
                    return result;

                case TileType.OneWay:
                    current.localDelta = DirectionToDelta(cell.specialData.oneWayDirection);
                    step.stepResult = StopReason.OneWayForced;
                    continue;

                case TileType.Teleport:
                {
                    TileCoord destination = map.GetTeleportDestination(current);

                    // Nếu tele tới chính nó → coi như Sticky
                    if (destination.face == current.face &&
                        destination.x == current.x &&
                        destination.y == current.y)
                    {
                        step.stepResult = StopReason.Sticky;
                        result.stopReason = StopReason.Sticky;
                        return result;
                    }

                    // Ghi lại bước Tele
                    SimulationStep teleStep = new SimulationStep
                    {
                        coord = destination,
                        isFaceChange = (destination.face != current.face),
                        stepResult = StopReason.Teleported
                    };
                    result.steps.Add(teleStep);

                    // ⭐⭐⭐ QUAN TRỌNG ⭐⭐⭐
                    // Biến cả 2 ô teleport thành FLOOR trên MAP THẬT
                    map.ConvertTeleportPairToFloor(current);

                    // Set vị trí mới cho player
                    current = new TileCoord(
                        destination.face,
                        destination.x,
                        destination.y,
                        current.localDelta   // giữ hướng trượt
                    );

                    // Sau teleport: DỪNG SLIDE → chờ input mới
                    result.stopReason = StopReason.Teleported;
                    return result;
                }


                // ─────────────────────────────────────────────────────────
                // ✅ CRACKED: KHÔNG CẦN XỬ LÝ Ở ĐÂY NỮA (đã xử lý ở đầu loop)
                // ─────────────────────────────────────────────────────────
                case TileType.Cracked:
                    // Skip - durability đã được giảm ở đầu loop khi RỜI ô này
                    continue;

                case TileType.Floor:
                    continue;
            }
        }

        if (result.stopReason == StopReason.None)
        {
            result.stopReason = StopReason.Wall;
        }

        return result;
    }

    // ─────────────────────────────────────────────────────
    //  ✅ HELPER: Kiểm tra ô có bị block không
    // ─────────────────────────────────────────────────────
    private bool IsBlocked(TileCoord coord, Dictionary<string, int> crackedDurability)
    {
        TileCell cell = map.GetTileCell(coord);
        if (cell == null) return true;
        
        // Nếu là Wall thật → block
        if (cell.Type == TileType.Wall) return true;
        
        // Nếu là Cracked đã vỡ thật (từ map) → block
        if (cell.Type == TileType.Cracked && cell.specialData.isBroken) return true;
        
        // ✅ Nếu là Cracked và đã hết durability trong simulation → block
        if (cell.Type == TileType.Cracked && !cell.specialData.isBroken)
        {
            string key = GetTileKey(coord);
            
            if (crackedDurability.ContainsKey(key))
            {
                // ✅ Nếu durability đã về 0 → coi như đã vỡ, BLOCK
                if (crackedDurability[key] <= 0)
                {
                    Debug.Log($"[Simulation] Blocked by broken Cracked at {key}");
                    return true;
                }
            }
        }
        
        return false;
    }

    private string GetTileKey(TileCoord coord)
    {
        return $"{(int)coord.face}_{coord.x}_{coord.y}";
    }

    bool TryProcessInstantTile(SimulationResult result, TileCoord tile)
    {
        TileCell cell = map.GetTileCell(tile);
        if (cell == null) return false;

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

    private void HandleTileChanged(TileCell cell)
    {
        needsRebuild = true;
    }
}