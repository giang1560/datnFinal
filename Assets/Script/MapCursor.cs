using System.Collections;
using UnityEngine;

public class MapCursor : MonoBehaviour
{
    [Header("References")]
    private RubikMap map;
    public GhostManager ghost;
    public ArrowVisibilityManager arrowManager;
    public VectorBasedRotator vectorRotator;

    [Header("Game")]
    public int maxMoves = 10;

    public TileCoord CurrentTile => currentTile;
    public bool CanReceiveInput => !isAnimating && movesLeft > 0;

    private RubikSimulator simulator;
    private TileCoord currentTile;
    private bool isAnimating;
    private int movesLeft;

    [Header("Movement")]
    public float slideSpeed = 2f;
    public bool enableRotation = true;

    public void Init(RubikMap map, int maxMoves)
    {
        this.map = map;
        this.maxMoves = maxMoves;
        simulator = new RubikSimulator(map);

        currentTile = map.GetPlayerSpawn();
        transform.position = map.GetWorldPosition(currentTile);

        movesLeft = maxMoves;
        arrowManager?.SpawnArrows();
    }

    public void MoveFromArrow(TileCoord startTile)
    {
        if (!CanReceiveInput)
            return;

        StartCoroutine(SlideRoutine(startTile));
    }

    IEnumerator SlideRoutine(TileCoord startTile)
    {
        isAnimating = true;
        arrowManager.ClearArrows();
        ghost.HideGhost();

        movesLeft--;

        SimulationResult sim = simulator.SimulateSlide(startTile);

        Vector3 previousWorldPos = transform.position;

        foreach (SimulationStep step in sim.steps)
        {
            TileCoord previousTile = currentTile;
            currentTile = step.coord;

            TileCell cell = map.GetTileCell(step.coord);
            if (cell)
            {
                Vector3 currentWorldPos = cell.transform.position;
                yield return MoveTo(currentWorldPos);

                // ✅ Xử lý Cracked tile NGAY KHI RỜI đi
                TileCell previousCell = map.GetTileCell(previousTile);
                if (previousCell != null && previousCell.Type == TileType.Cracked)
                {
                    previousCell.OnPlayerPassThrough();
                }

                // ✅ Xử lý Teleport tile KHI ĐẾN
                if (step.stepResult == StopReason.Teleported)
                {
                    // Đổi cả 2 ô teleport thành Floor
                    TileCell sourceCell = map.GetTileCell(previousTile);
                    TileCell destCell = map.GetTileCell(currentTile);
                    
                    if (sourceCell != null && sourceCell.Type == TileType.Teleport)
                        sourceCell.Type = TileType.Floor;
                    if (destCell != null && destCell.Type == TileType.Teleport)
                        destCell.Type = TileType.Floor;
                        
                    Debug.Log($"[MapCursor] Teleport used: {previousTile} ↔ {currentTile}");
                }

                // ─────────────────────────────────────────────────
                // ✅ ROTATION LOGIC: Phân biệt Teleport và Face Change
                // ─────────────────────────────────────────────────
                if (enableRotation && vectorRotator != null)
                {
                    // 🔷 Case 1: TELEPORT → Dùng RotateForTeleport()
                    if (step.stepResult == StopReason.Teleported)
                    {
                        bool rotationComplete = false;

                        Debug.Log($"[MapCursor] Teleport rotation: {previousWorldPos} → {currentWorldPos}");
                        
                        vectorRotator.RotateForTeleport(
                            previousWorldPos,
                            currentWorldPos,
                            () => rotationComplete = true
                        );

                        yield return new WaitUntil(() => rotationComplete);
                    }
                    // 🔷 Case 2: FACE CHANGE (Normal) → Dùng RotateBasedOnMovement()
                    else if (step.isFaceChange)
                    {
                        bool rotationComplete = false;

                        Debug.Log($"[MapCursor] Face change rotation: {previousWorldPos} → {currentWorldPos}");
                        
                        vectorRotator.RotateBasedOnMovement(
                            previousWorldPos,
                            currentWorldPos,
                            () => rotationComplete = true
                        );

                        yield return new WaitUntil(() => rotationComplete);
                    }
                }

                previousWorldPos = currentWorldPos;
            }
        }

        HandleStop(sim.stopReason);

        isAnimating = false;

        if (movesLeft > 0)
            arrowManager.SpawnArrows();
    }

    IEnumerator MoveTo(Vector3 target)
    {
        float t = 0f;
        Vector3 start = transform.position;

        while (t < 1f)
        {
            t += Time.deltaTime * slideSpeed;
            transform.position = Vector3.Lerp(start, target, t);
            yield return null;
        }

        transform.position = target;
    }

    void HandleStop(StopReason reason)
    {
        switch (reason)
        {
            case StopReason.Goal:
                Debug.Log("WIN!");
                break;

            case StopReason.Trap:
                Debug.Log("LOSE (trap)");
                break;

            case StopReason.Wall:
            case StopReason.Sticky:
                Debug.Log("Stop.");
                break;
        }
    }

    public void ResetLevel()
    {
        StopAllCoroutines();
        isAnimating = false;

        if (vectorRotator != null)
            vectorRotator.ResetRotation();

        movesLeft = maxMoves;

        map.ResetAllTiles();

        currentTile = map.GetPlayerSpawn();
        transform.position = map.GetWorldPosition(currentTile);

        ghost?.HideGhost();

        if (arrowManager != null)
        {
            arrowManager.ClearArrows();
            arrowManager.SpawnArrows();
        }

        Debug.Log("[RESET] Level reset");
    }
}