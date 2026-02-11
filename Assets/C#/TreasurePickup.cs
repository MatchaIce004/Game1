using UnityEngine;
using UnityEngine.Tilemaps;

public class TreasurePickup : MonoBehaviour
{
    [Header("Treasure Tilemap / Tile")]
    public Tilemap treasureTilemap;   // Inspectorで「お宝(Tilemap)」を入れる
    public TileBase treasureTile;     // Inspectorで「お宝タイル」を入れる

    [Header("Hold Time (seconds)")]
    public float holdTime = 1.0f;     // ★何秒乗ったら取得するか

    float stayTimer = 0f;
    Vector3Int currentCell;
    bool onTreasure = false;

    void Update()
    {
        if (ItemManager.Instance == null) return;
        if (ItemManager.Instance.runHasTreasure) return; // 既に持ってたら何もしない

        if (treasureTilemap == null || treasureTile == null) return;

        Vector3Int cell = treasureTilemap.WorldToCell(transform.position);
        TileBase tile = treasureTilemap.GetTile(cell);

        // お宝タイルじゃない → リセット
        if (tile == null || tile != treasureTile)
        {
            stayTimer = 0f;
            onTreasure = false;
            return;
        }

        // 同じセルに乗り続けている
        if (onTreasure && cell == currentCell)
        {
            stayTimer += Time.deltaTime;

            if (stayTimer >= holdTime)
            {
                Pickup(cell);
                stayTimer = 0f;
                onTreasure = false;
            }
            return;
        }

        // 新しくお宝タイルに乗った
        onTreasure = true;
        currentCell = cell;
        stayTimer = 0f;
    }

    void Pickup(Vector3Int cell)
    {
        // タイル削除
        treasureTilemap.SetTile(cell, null);
        treasureTilemap.RefreshTile(cell);

        // フラグ
        ItemManager.Instance.runHasTreasure = true;
        ItemManager.Instance.treasurePicked = true;
        ItemManager.Instance.SaveRun();

        Debug.Log("お宝を入手！（Tilemap）");
    }
}
