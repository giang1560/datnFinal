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
    
    // ✅ LƯU LOẠI Ô BAN ĐẦU để reset
    private TileType originalType;

    public bool IsHighlighted
    {
        get => _isHighlighted;
        set { if(_isHighlighted != value) { _isHighlighted = value; RefreshVisual(); } }
    }

    public TileType Type
    {
        get => _type;
        set { if(_type != value) { _type = value; RefreshVisual(); } }
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
        if (_rend == null) _rend = GetComponent<Renderer>();
        if (_rend == null) return;
        
        transform.localScale = new Vector3(0.9f, 0.1f, 0.9f);
        
        switch (_type)
        {
            case TileType.Floor:
                _rend.material.color = Color.white;
                break;
                
            case TileType.Wall:
                _rend.material.color = Color.black;
                transform.localScale = new Vector3(0.9f, 1f, 0.9f);
                break;
                
            case TileType.Trap:
                _rend.material.color = Color.red;
                break;
                
            case TileType.Sticky:
                _rend.material.color = Color.blue;
                break;
                
            case TileType.Goal:
                _rend.material.color = Color.green;
                break;
                
            case TileType.OneWay:
                _rend.material.color = new Color(1f, 0.5f, 0f); // Cam
                break;
                
            case TileType.Teleport:
                _rend.material.color = new Color(0.5f, 0f, 1f); // Tím
                break;
                
            case TileType.Cracked:
                if (specialData.isBroken)
                {
                    _rend.material.color = Color.gray;
                    transform.localScale = new Vector3(0.9f, 1f, 0.9f);
                }
                else
                {
                    float alpha = Mathf.Clamp01(specialData.durability / (float)originalDurability);
                    _rend.material.color = new Color(1f, 1f, 0f, 0.5f + alpha * 0.5f);
                }
                break;
        }

        if (_isHighlighted) 
        {
            _rend.material.color = Color.yellow;
        }
    }
}