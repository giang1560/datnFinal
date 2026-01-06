using UnityEngine;

[CreateAssetMenu(fileName = "TileDefine", menuName = "ScriptableObjects/TileDefineSO", order = 1)]
public class TileDefine : ScriptableObject
{
    private GameObject[] tilePrefabs = new GameObject[System.Enum.GetValues(typeof(TileType)).Length];
}
