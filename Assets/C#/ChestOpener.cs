using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class ChestOpener : MonoBehaviour
{
    public float openDistance = 1f; // 使わないなら消してOK

    CaveGenerator gen;

    void Start()
    {
        gen = FindObjectOfType<CaveGenerator>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            TryOpenChest();
        }
    }

    void TryOpenChest()
    {
        if (gen == null || gen.chestTilemap == null) return;
        if (ItemManager.Instance == null) return;

        Vector3 playerPos = transform.position;

        foreach (Vector2Int pos in GetNearbyCells(playerPos))
        {
            if (gen.chestTilemap.GetTile((Vector3Int)pos) != null)
            {
                OpenChest(pos);
                return;
            }
        }
    }

    void OpenChest(Vector2Int pos)
    {
        if (ItemManager.Instance == null) return;
        if (gen == null || gen.chestTilemap == null) return;

        // すでに開けた箱なら何もしない（戻り階層でも復活しないための保険）
        if (ItemManager.Instance.IsChestOpened(pos)) return;

        // 箱レアを取得（消す前に取るのが安全）
        ItemRarity rarity = gen != null ? gen.GetChestRarity(pos) : ItemRarity.Common;

        // 宝箱を消す
        gen.chestTilemap.SetTile((Vector3Int)pos, null);

        // ★開封済みとして floor別に記録（Bの核心）
        ItemManager.Instance.MarkChestOpened(pos);

        var item = ItemManager.Instance.DrawFromChest(rarity);
        if (item == null)
        {
            Debug.Log($"空箱 ({rarity})");
            // 空箱演出
        }
        else
        {
            Debug.Log($"宝箱 ({rarity}) → {item.itemName}");
            ItemManager.Instance.AddPendingLoot(item);
        }

        // レア辞書から消す（任意：整合性）
        if (gen != null && gen.chestRarityByCell != null)
            gen.chestRarityByCell.Remove(pos);
    }

    List<Vector2Int> GetNearbyCells(Vector3 pos)
    {
        List<Vector2Int> cells = new List<Vector2Int>();

        Vector2Int center = new Vector2Int(
            Mathf.RoundToInt(pos.x),
            Mathf.RoundToInt(pos.y)
        );

        cells.Add(center);
        cells.Add(center + Vector2Int.right);
        cells.Add(center + Vector2Int.left);
        cells.Add(center + Vector2Int.up);
        cells.Add(center + Vector2Int.down);

        return cells;
    }
}
