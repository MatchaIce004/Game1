using UnityEngine;

[CreateAssetMenu(fileName = "ChestLootTable", menuName = "Game/ChestLootTable")]
public class ChestLootTable : ScriptableObject
{
    public ItemData[] possibleItems;
}
