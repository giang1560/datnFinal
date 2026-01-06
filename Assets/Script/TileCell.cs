using UnityEngine;

public class TileCell : MonoBehaviour
{
    [Header("Base Info")]
    public FaceID face;
    public int x;
    public int y;

    [SerializeField] private TileType _type;
    [SerializeField] private bool _isHighlighted;

    [Header("Special Tile Data")]
    public TileSpecialData specialData = new TileSpecialData();

    private Renderer _rend;
    private int originalDurability;
    private TileType originalType;

    public bool IsHighlighted
    {
        get => _isHighlighted;
        set { if (_isHighlighted != value) { _isHighlighted = value; RefreshVisual(); } }
    }

    public TileType Type
    {
        get => _type;
        set { if (_type != value) { _type = value; RefreshVisual(); } }
    }

    public void Initialize(FaceID f, int x, int y, TileType t)
    {
        this.face = f;
        this.x = x;
        this.y = y;
        this.Type = t;

        // ✅ LƯU LOẠI Ô BAN ĐẦU
        this.originalType = t;

        if (t == TileType.Cracked)
        {
            originalDurability = specialData.durability;
        }

        RefreshVisual();
    }

    public void SetOneWayDirection(Direction dir)
    {
        specialData.oneWayDirection = dir;
        RefreshVisual();
    }

    public void SetTeleportPair(int pairID)
    {
        specialData.teleportPairID = pairID;
        RefreshVisual();
    }

    public void OnPlayerPassThrough()
    {
        if (_type == TileType.Cracked && !specialData.isBroken)
        {
            specialData.durability--;

            if (specialData.durability <= 0)
            {
                specialData.isBroken = true;
                _type = TileType.Wall;

                // 🔔 BÁO TILE ĐÃ THAY ĐỔI
                //OnChanged?.Invoke(this);
            }

            RefreshVisual();
        }
    }


    public void ResetTile()
    {
        // ✅ KHÔI PHỤC VỀ LOẠI Ô BAN ĐẦU
        _type = originalType;

        // Reset Cracked về trạng thái ban đầu
        if (originalType == TileType.Cracked)
        {
            specialData.durability = originalDurability;
            specialData.isBroken = false;
        }

        RefreshVisual();
    }

    public void RefreshVisual()
    {
        if (_rend != null)
            Destroy(_rend.gameObject);

        GameObject tilePrefab = ScriptableObjectController.Instance.tileDefineSO.GetTilePrefab(_type);
        if (tilePrefab == null) return;
        _rend = Instantiate(tilePrefab, transform).GetComponent<Renderer>();

        if (_isHighlighted)
        {
            _rend.material.color = Color.yellow;
        }
    }

    /// <summary>
    /// Thay đổi loại ô
    /// </summary>
    /// <param name="newType"></param>
    public void ChangeType(TileType newType)
    {
        if (_type != newType)
        {
            _type = newType;
            RefreshVisual();
        }
    }
}