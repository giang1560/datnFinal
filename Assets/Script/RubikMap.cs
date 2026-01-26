using UnityEngine;
using System.Collections.Generic;
using System;
using Michsky.MUIP;
using DG.Tweening;
using System.Collections;

public class RubikMap : MonoBehaviour
{
    [Header("Map Settings")]
    public int mapSize = 3;
    public float tileSize = 1.0f;
    public ArrowVisibilityManager arrowManager;
    public GameObject tilePrefab;

    [Header("Level Data")]
    [Tooltip("Mỗi string = 1 face, ký tự = loại ô. VD: '0' = Floor, '1' = Wall, 'T' = Trap, 'S' = Sticky, 'X' = Goal, 'O' = OneWay, 'P' = Teleport, 'C' = Cracked")]
    public string[] levelRawData;

    [Header("Special Tiles Config")]
    [Tooltip("Format: 'FaceID,X,Y,Direction' VD: '0,1,2,Up' = OneWay tại Front(1,2) hướng Up")]
    public string[] oneWayConfig;

    // [Tooltip("Format: 'FaceID1,X1,Y1,FaceID2,X2,Y2' VD: '0,1,1,2,1,1' = Teleport từ Front(1,1) đến Back(1,1)")]
    // public string[] teleportPairs;

    [Header("UI")]
    [SerializeField] private Canvas loadingCanvas;
    [SerializeField] private HorizontalSelector horizontalSelector;

    [Header("Camera Settings")]
    [SerializeField] private Camera mainCamera;

    [Header("Map Cursor")]
    [SerializeField] private MapCursor mapCursor;
    [Header("GameMode")]
    [SerializeField] private GameMode gameMode = GameMode.Play;

    private Dictionary<FaceID, TileCell[,]> mapData = new Dictionary<FaceID, TileCell[,]>();
    //private Dictionary<string, TileCoord> teleportLinks = new Dictionary<string, TileCoord>();
    private List<TileCoord> teleportLinks = new List<TileCoord>();
    private List<GameObject> faceObjects = new List<GameObject>();
    private TileCoord playerSpawn;
    private bool hasSpawn = false;
    public event System.Action<TileCell> OnTileChanged;
    private int currentLevelIndex = 0;
    private List<LevelConfig> levelConfigs;

    void Awake()
    {
        horizontalSelector.onValueChanged.AddListener((index) =>
        {
            LoadLevel(index);
        });
    }

    void Start()
    {
        switch (gameMode)
        {
            case GameMode.Play:
                InitPlayMode();
                break;
            case GameMode.Edit:
                InitEditorMode();
                break;
            case GameMode.Online:
                // Handle online mode if needed
                break;
        }
    }

    /// <summary>
    /// Khởi tạo chế độ chơi
    /// </summary>
    private void InitPlayMode()
    {
        DOVirtual.DelayedCall(3f, () =>
        {
            this.levelConfigs = LevelRemoteConfig.Instance.LevelConfigs;

            for (int i = 1; i < levelConfigs.Count; i++)
            {
                horizontalSelector.CreateNewItem("Level " + (i + 1));
            }

            LoadLevel(0);
        });
    }

    /// <summary>
    /// Khởi tạo chế độ chỉnh sửa
    /// </summary>
    private void InitEditorMode()
    {
        StartCoroutine(GenerateMap(true));
    }

    public IEnumerator GenerateMap(bool isLeft = true)
    {
        ValidateLevelRawDataBySize();

        arrowManager.gameObject.SetActive(false);
        mapCursor.gameObject.SetActive(false);
        transform.parent.rotation = Quaternion.Euler(0f, 0f, 0f);
        yield return CleanUpMapWithAnimation(isLeft).WaitForCompletion();


        float offset = (mapSize * tileSize) / 2.0f;
        float startPos = -offset + (tileSize / 2.0f);

        // 1. Tạo các ô
        for (int i = 0; i < 6; i++)
        {
            FaceID fId = (FaceID)i;
            GameObject faceRoot = new GameObject("Face_" + fId);
            faceRoot.transform.parent = transform;
            faceRoot.transform.localPosition = Vector3.zero;
            faceRoot.transform.localRotation = Quaternion.identity;
            faceObjects.Add(faceRoot);

            TileCell[,] cells = new TileCell[mapSize, mapSize];

            for (int y = 0; y < mapSize; y++)
            {
                for (int x = 0; x < mapSize; x++)
                {
                    GetFaceTransform(i, startPos + x * tileSize, startPos + y * tileSize, offset,
                                     out Vector3 pos, out Quaternion rot);

                    GameObject obj = Instantiate(tilePrefab, pos, rot, faceRoot.transform);
                    obj.name = $"Tile_{x}_{y}";

                    TileCell cell = obj.GetComponent<TileCell>();
                    TileType type = ParseType(fId, x, y);

                    if(type == TileType.Teleport)
                    {
                        teleportLinks.Add(new TileCoord(fId, x, y));
                    }

                    cell.Initialize(fId, x, y, type);
                    cells[x, y] = cell;
                }
            }
            mapData.Add(fId, cells);
        }

        // 2. Cấu hình OneWay
        ParseOneWayConfig();

        // 3. Cấu hình Teleport
        //ParseTeleportConfig();

        Debug.Log($"[RubikMap] Animation start. isLeft={isLeft}");

        transform.position += isLeft ? Vector3.right * 20f : Vector3.left * 20f;
        transform.DOMoveX(0f, 0.5f)
        .OnComplete(() =>
        {
            Debug.Log("[RubikMap] Map generation complete.");
            RubikNavigator.Init(mapSize);
            if(gameMode == GameMode.Play)
            mapCursor.Init(this, levelConfigs[currentLevelIndex].maxMoves);
            else
            mapCursor.Init(this, 10);

            arrowManager.Init();
            arrowManager.gameObject.SetActive(true);
            mapCursor.gameObject.SetActive(true);
            horizontalSelector.isActive = true;
        });

        //change camera solid color
        if (levelConfigs != null && currentLevelIndex >= 0 && currentLevelIndex < levelConfigs.Count)
        {
            string bgColorHex = levelConfigs[currentLevelIndex].backgroundColorHex;
            if (!string.IsNullOrEmpty(bgColorHex))
            {
                if (ColorUtility.TryParseHtmlString(bgColorHex, out Color bgColor))
                {
                    GetCameraBackgroundColorTween(bgColor, 0.5f).Play();
                }
            }
        }
    }

    public Tween GetCameraBackgroundColorTween(Color targetColor, float duration)
    {
        if (mainCamera == null)
            return null;

        Color initialColor = mainCamera.backgroundColor;
        return DOTween.To(() => initialColor, x =>
        {
            initialColor = x;
            mainCamera.backgroundColor = initialColor;
        }, targetColor, duration);
    }

    /// <summary>
    /// Tải level từ danh sách levelConfigs
    /// </summary>
    /// <param name="levelIndex"></param>
    public void LoadLevel(int levelIndex)
    {
        bool isLeft = levelIndex <= currentLevelIndex || (currentLevelIndex == 0 && levelIndex == levelConfigs.Count - 1);
        if (currentLevelIndex == levelConfigs.Count - 1 && levelIndex == 0)
            isLeft = false;
        currentLevelIndex = levelIndex;
        if (levelConfigs != null && levelIndex >= 0 && levelIndex < levelConfigs.Count)
        {
            horizontalSelector.isActive = false;
            levelRawData = levelConfigs[levelIndex].levelRawData;
            mapSize = levelConfigs[levelIndex].mapSize;
            StartCoroutine(GenerateMap(isLeft));
        }
    }

    /// <summary>
    /// Tải level tiếp theo từ danh sách levelConfigs
    /// </summary>
    public void LoadNextLevel()
    {
        int nextLevelIndex = currentLevelIndex + 1;
        if (levelConfigs != null && nextLevelIndex < levelConfigs.Count)
        {
            LoadLevel(nextLevelIndex);
        }
        else
        {
            Debug.Log("No more levels to load.");
        }
    }

    public void LoadNextLevelByTriggerButton()
    {
        horizontalSelector.ForwardClick();
    }

    /// <summary>
    /// Tải level trước đó từ danh sách levelConfigs
    /// </summary>
    public void LoadPreviousLevel()
    {
        int prevLevelIndex = currentLevelIndex - 1;
        if (levelConfigs != null && prevLevelIndex >= 0)
        {
            LoadLevel(prevLevelIndex);
        }
        else
        {
            Debug.Log("No previous level to load.");
        }
    }

    /// <summary>
    /// Xóa sạch bản đồ hiện tại
    /// </summary>
    public Tween CleanUpMapWithAnimation(bool isLeft)
    {
        GameObject emptyParent = new GameObject("TempParent");

        // Move it to the left with animation
        return emptyParent.transform.DOMoveX(isLeft ? -20f : 20f, 0.5f)
        .OnStart(() =>
        {
            // Create a empty parent in this position and attach all tiles to it
            emptyParent.transform.position = transform.position;
            emptyParent.transform.rotation = transform.rotation;

            foreach (GameObject child in faceObjects)
            {
                child.transform.SetParent(emptyParent.transform);
            }
        })
        .OnComplete(() =>
        {
            // Destroy the empty parent and all its children
            Destroy(emptyParent);
            mapData.Clear();
            teleportLinks.Clear();
            faceObjects.Clear();
        });
    }

    /// <summary>
    /// Xuất dữ liệu level hiện tại thành LevelConfig
    /// </summary>
    /// <returns></returns>
    public LevelConfig ExportLevelConfig()
    {
        LevelConfig config = new LevelConfig();
        config.mapSize = mapSize;
        int rowLength = mapSize * mapSize;
        config.levelRawData = new string[6];

        foreach (FaceID f in Enum.GetValues(typeof(FaceID)))
        {
            char[] rowChars = new char[rowLength];
            TileCell[,] cells = mapData[f];

            for (int y = 0; y < mapSize; y++)
            {
                for (int x = 0; x < mapSize; x++)
                {
                    TileCell cell = cells[x, y];
                    char c = '0'; // Default Floor

                    switch (cell.Type)
                    {
                        case TileType.Wall: c = '1'; break;
                        case TileType.Trap: c = 'T'; break;
                        case TileType.Sticky: c = 'S'; break;
                        case TileType.Goal: c = 'X'; break;
                        case TileType.OneWay: c = 'O'; break;
                        case TileType.Teleport: c = 'P'; break;
                        case TileType.Cracked: c = 'C'; break;
                        case TileType.Spawn: c = 'M'; break;
                    }

                    // Đánh dấu spawn
                    // if (playerSpawn.face == f && playerSpawn.x == x && playerSpawn.y == y)
                    // {
                    //     c = 'M';
                    // }

                    rowChars[y * mapSize + x] = c;
                }
            }

            config.levelRawData[(int)f] = new string(rowChars);
        }
        this.levelRawData = config.levelRawData;
        return config;
    }

    /// <summary>
    /// Đảm bảo dữ liệu levelRawData đúng với kích thước mapSize và hợp lệ
    /// </summary>
    void ValidateLevelRawDataBySize()
    {
        int rowLength = mapSize * mapSize;
        if (levelRawData == null || levelRawData.Length < 6)
        {
            while (levelRawData.Length < 6)
            {
                System.Array.Resize(ref levelRawData, levelRawData.Length + 1);
                levelRawData[levelRawData.Length - 1] = new string('0', rowLength);
            }
        }

        foreach (FaceID f in System.Enum.GetValues(typeof(FaceID)))
        {
            int idx = (int)f;
            if (levelRawData[idx] == null || levelRawData[idx].Length < rowLength)
            {
                levelRawData[idx] = levelRawData[idx] != null ? levelRawData[idx] : "";
                while (levelRawData[idx].Length < rowLength)
                {
                    levelRawData[idx] += "0";
                }
            }
        }
    }

    // --- PARSE LEVEL DATA ---
    TileType ParseType(FaceID f, int x, int y)
    {
        if (levelRawData == null || (int)f >= levelRawData.Length) return TileType.Floor;
        string row = levelRawData[(int)f];
        int idx = y * mapSize + x;
        if (idx >= row.Length) return TileType.Floor;

        char c = row[idx];
        switch (c)
        {
            case '1': return TileType.Wall;
            case 'T': return TileType.Trap;
            case 'S': return TileType.Sticky;
            case 'X': return TileType.Goal;
            case 'O': return TileType.OneWay;
            case 'P': return TileType.Teleport;
            case 'C': return TileType.Cracked;

            case 'M':
                // đánh dấu spawn
                playerSpawn = new TileCoord(f, x, y);
                hasSpawn = true;
                return TileType.Spawn;

            default: return TileType.Floor;
        }
    }

    // --- ONEWAY CONFIG ---
    void ParseOneWayConfig()
    {
        if (oneWayConfig == null) return;

        foreach (string config in oneWayConfig)
        {
            string[] parts = config.Split(',');
            if (parts.Length < 4) continue;

            FaceID face = (FaceID)int.Parse(parts[0].Trim());
            int x = int.Parse(parts[1].Trim());
            int y = int.Parse(parts[2].Trim());
            Direction dir = (Direction)System.Enum.Parse(typeof(Direction), parts[3].Trim());

            TileCell cell = GetTileCell(new TileCoord(face, x, y));
            if (cell != null && cell.Type == TileType.OneWay)
            {
                cell.SetOneWayDirection(dir);
            }
        }
    }

    // --- TELEPORT CONFIG ---
    // void ParseTeleportConfig()
    // {
    //     if (teleportPairs == null) return;

    //     foreach (string pair in teleportPairs)
    //     {
    //         string[] parts = pair.Split(',');
    //         if (parts.Length < 6) continue;

    //         FaceID face1 = (FaceID)int.Parse(parts[0].Trim());
    //         int x1 = int.Parse(parts[1].Trim());
    //         int y1 = int.Parse(parts[2].Trim());

    //         FaceID face2 = (FaceID)int.Parse(parts[3].Trim());
    //         int x2 = int.Parse(parts[4].Trim());
    //         int y2 = int.Parse(parts[5].Trim());

    //         TileCoord coord1 = new TileCoord(face1, x1, y1);
    //         TileCoord coord2 = new TileCoord(face2, x2, y2);

    //         string key1 = $"{(int)face1}_{x1}_{y1}";
    //         string key2 = $"{(int)face2}_{x2}_{y2}";

    //         teleportLinks[key1] = coord2;
    //         teleportLinks[key2] = coord1; // Hai chiều

    //         TileCell cell1 = GetTileCell(coord1);
    //         TileCell cell2 = GetTileCell(coord2);

    //         if (cell1 != null) cell1.SetTeleportPair(1);
    //         if (cell2 != null) cell2.SetTeleportPair(1);
    //     }
    // }

    // --- PUBLIC API ---
    public TileCell GetTileCell(TileCoord c)
    {
        if (mapData.ContainsKey(c.face) && IsInside(c.x, c.y))
            return mapData[c.face][c.x, c.y];
        return null;
    }

    public bool IsWalkable(TileCoord c)
    {
        TileCell cell = GetTileCell(c);
        if (cell == null) return false;

        // Cracked đã vỡ = Wall
        if (cell.Type == TileType.Cracked && cell.specialData.isBroken)
            return false;

        return cell.Type != TileType.Wall;
    }

    public TileCoord GetTeleportDestination(TileCoord from)
    {
        // string key = $"{(int)from.face}_{from.x}_{from.y}";
        // if (teleportLinks.ContainsKey(key))
        // {
        //     return teleportLinks[key];
        // }
        // return from; // Không tìm thấy → Trả về chính nó
        int idx = -1;
        for(int i = 0; i < teleportLinks.Count; i++)
        {
            TileCoord t = teleportLinks[i];
            if(t.face == from.face && t.x == from.x && t.y == from.y)
            {
                idx = i;
                break;
            }
        }

        int nextIdx = (idx + 1) % teleportLinks.Count;
        return teleportLinks[nextIdx];
    }

    // ✅ MỚI: Vô hiệu hóa cặp teleport (xóa khỏi dictionary)
    public void DisableTeleportPair(TileCoord coord)
    {
        // string key1 = $"{(int)coord.face}_{coord.x}_{coord.y}";

        // // Tìm ô đích
        // if (teleportLinks.ContainsKey(key1))
        // {
        //     TileCoord dest = teleportLinks[key1];
        //     string key2 = $"{(int)dest.face}_{dest.x}_{dest.y}";

        //     // Xóa cả 2 chiều
        //     teleportLinks.Remove(key1);
        //     teleportLinks.Remove(key2);
        // }
    }

    public void ConvertTeleportPairToFloor(TileCoord coord)
    {
        // string key1 = $"{(int)coord.face}_{coord.x}_{coord.y}";

        // if (!teleportLinks.ContainsKey(key1))
        //     return;

        // TileCoord dest = teleportLinks[key1];
        // string key2 = $"{(int)dest.face}_{dest.x}_{dest.y}";

        // // Lấy cell thật
        // TileCell a = GetTileCell(coord);
        // TileCell b = GetTileCell(dest);

        // if (a != null)
        //     a.ChangeType(TileType.Floor);

        // if (b != null)
        //     b.ChangeType(TileType.Floor);

        // // Xóa link
        // teleportLinks.Remove(key1);
        // teleportLinks.Remove(key2);
    }


    public void ResetAllTiles()
    {
        // Reset visual và state của tiles
        // foreach (var facePair in mapData)
        // {
        //     foreach (TileCell cell in facePair.Value)
        //     {
        //         if (cell != null) cell.ResetTile();
        //     }
        // }

        // ✅ Khôi phục lại tất cả teleport links
        teleportLinks.Clear();
        //ParseTeleportConfig();

        // Debug: Kiểm tra xem có parse lại đúng không
        Debug.Log($"[RubikMap] Reset: Teleport links count = {teleportLinks.Count}");

        if(gameMode == GameMode.Edit)
        {
            StartCoroutine(GenerateMap(true));
        }
        else
        {
            LoadLevel(currentLevelIndex);
        }
    }

    // --- HELPERS ---
    public bool IsInsideFace(TileCoord c)
    {
        return c.x >= 0 && c.x < mapSize &&
            c.y >= 0 && c.y < mapSize;
    }

    public bool IsWall(TileCoord t)
    {
        var cell = GetTileCell(t);
        if (cell == null) return true;
        return cell.Type == TileType.Wall;
    }

    public Vector3 GetEdgeAnchor(TileCoord player, Vector2Int delta, float offset)
    {
        // vị trí world của ô player
        Vector3 basePos = GetWorldPosition(player);

        // hướng dịch chuyển sang ngoài mép
        Vector3 dir = new Vector3(delta.x, 0f, delta.y);

        // đẩy mũi tên ra ngoài một chút
        return basePos + dir.normalized * offset;
    }



    bool IsInside(int x, int y) => x >= 0 && x < mapSize && y >= 0 && y < mapSize;

    void GetFaceTransform(int faceIndex, float u, float v, float offset, out Vector3 pos, out Quaternion rot)
    {
        pos = Vector3.zero; rot = Quaternion.identity;
        switch (faceIndex)
        {
            case 0: pos = new Vector3(u, v, -offset); rot = Quaternion.Euler(-90, 0, 0); break;
            case 1: pos = new Vector3(u, offset, v); rot = Quaternion.Euler(0, 0, 0); break;
            case 2: pos = new Vector3(-u, v, offset); rot = Quaternion.Euler(-90, 0, 180); break;
            case 3: pos = new Vector3(u, -offset, -v); rot = Quaternion.Euler(180, 0, 0); break;
            case 4: pos = new Vector3(-offset, v, -u); rot = Quaternion.Euler(0, 0, 90); break;
            case 5: pos = new Vector3(offset, v, u); rot = Quaternion.Euler(0, 0, -90); break;
        }
    }

    public TileCoord GetPlayerSpawn()
    {
        if (hasSpawn)
            return playerSpawn;

        // fallback cũ: tìm Floor đầu tiên
        foreach (var kv in mapData)
        {
            FaceID face = kv.Key;
            TileCell[,] cells = kv.Value;

            for (int x = 0; x < mapSize; x++)
            {
                for (int y = 0; y < mapSize; y++)
                {
                    TileCell c = cells[x, y];
                    if (c != null && c.Type == TileType.Spawn)
                        return new TileCoord(face, x, y);
                }
            }
        }

        return new TileCoord(FaceID.Front, 0, 0);
    }

    public Vector3 GetWorldPosition(TileCoord c)
    {
        TileCell cell = GetTileCell(c);
        return cell ? cell.transform.position : Vector3.zero;
    }
}

public enum GameMode
{
    Play,
    Edit,
    Online
}