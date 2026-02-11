using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement;

public class StairTrigger : MonoBehaviour
{
    CaveGenerator gen;

    [Header("Hold Time (seconds)")]
    public float holdTime = 2.0f;          // ★階段に何秒乗れば発動するか
    public float escapeLockSeconds = 5f;   // ★1F脱出ロック時間

    float stayTimer = 0f;
    Vector3Int currentCell;
    bool onStair = false;

    void Start()
    {
        gen = FindObjectOfType<CaveGenerator>();
    }

    void Update()
    {
        if (gen == null || gen.stairTilemap == null) return;
        if (ItemManager.Instance == null) return;

        Vector3Int cell = gen.stairTilemap.WorldToCell(transform.position);
        TileBase tile = gen.stairTilemap.GetTile(cell);

        // ===== 階段に乗っていない =====
        if (tile == null)
        {
            stayTimer = 0f;
            onStair = false;
            return;
        }

        // ===== 同じ階段に乗り続けている =====
        if (onStair && cell == currentCell)
        {
            stayTimer += Time.deltaTime;

            // デバッグ表示
            // Debug.Log($"移動まで {holdTime - stayTimer:F1}");

            if (stayTimer >= holdTime)
            {
                TriggerStair(tile, cell);
                stayTimer = 0f;
                onStair = false;
            }

            return;
        }

        // ===== 新しく階段に乗った =====
        onStair = true;
        currentCell = cell;
        stayTimer = 0f;
    }

    // =========================
    // 階段処理本体
    // =========================
    void TriggerStair(TileBase tile, Vector3Int cell)
    {
        int floor = ItemManager.Instance.currentFloor;

        // 下り階段（入口/中間）
        if (gen.stairDownTile != null && tile == gen.stairDownTile)
        {
            // 1Fの下り＝Entrance（1F→2F）
            // 2Fの下り＝Mid（2F→3F）
            string kind = (floor == 1) ? "Entrance" : "Mid";

            SaveCell(cell, kind);
            ItemManager.Instance.ChangeFloor(+1);
            return;
        }

        // 上り階段（入口/中間/脱出）
        if (gen.stairUpTile != null && tile == gen.stairUpTile)
        {
            // 1Fの上り＝Escape（脱出）
            // 2F/3Fの上り＝入口側 or 中間側（どっちかは floor で判断）
            if (floor == 1)
            {
                SaveCell(cell, "Escape");

                if (Time.timeSinceLevelLoad < escapeLockSeconds)
                {
                    Debug.Log("まだ脱出できません");
                    return;
                }

                Escape();
                return;
            }

            // 2Fの上り＝Entrance（2F→1F）
            // 3Fの上り＝Mid（3F→2F）
            string kind = (floor == 2) ? "Entrance" : "Mid";

            SaveCell(cell, kind);
            ItemManager.Instance.ChangeFloor(-1);
        }
    }

    void SaveCell(Vector3Int cell, string kind)
    {
        ItemManager.Instance.hasLastStairCell = true;
        ItemManager.Instance.lastStairCell = new Vector2Int(cell.x, cell.y);
        ItemManager.Instance.lastStairKind = kind;   // ★追加
        ItemManager.Instance.SaveRun();
    }


    // =========================
    // 脱出処理
    // =========================
    void Escape()
    {
        float timeNow = GameTimer.Instance != null ? GameTimer.Instance.GetTime() : 0f;

        PlayerPrefs.SetFloat("PlayTime", timeNow);
        PlayerPrefs.Save();

        bool hasTreasure = ItemManager.Instance.runHasTreasure;

        ItemManager.Instance.OnEscapeSuccess();

        if (hasTreasure)
            SceneManager.LoadScene("ClearScene");
        else
            SceneManager.LoadScene("ResultScene");
    }
}
