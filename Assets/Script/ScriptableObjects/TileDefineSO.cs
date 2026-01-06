using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TileDefine", menuName = "ScriptableObjects/TileDefineSO", order = 1)]
public class TileDefineSO : ScriptableObject
{
    [SerializeField,EnumBind(typeof(TileType))]
    private List<GameObject> tilePrefabs = new List<GameObject>(new GameObject[System.Enum.GetValues(typeof(TileType)).Length]);

    /// <summary>
    /// Lấy prefab tương ứng với loại ô
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns> <summary>
    public GameObject GetTilePrefab(TileType type)
    {
        int index = (int)type;
        if (index < 0 || index >= tilePrefabs.Count)
        {
            Debug.LogError($"TileDefine: Invalid TileType index {index}");
            return null;
        }
        return tilePrefabs[index];
    }
}
