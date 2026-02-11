using UnityEngine;
using UnityEngine.SceneManagement;

public class StairTrigger : MonoBehaviour
{
    [Header("Hold Time (seconds)")]
    public float holdTime = 2.0f;
    public float escapeLockSeconds = 5f;

    [Header("Grid Reference")]
    public Grid grid; // シーンのGridをアサイン推奨（未設定なら自動検索）

    float stayTimer = 0f;
    Vector3Int currentCell3;
    bool onStair = false;

    void Awake()
    {
        if (grid == null) grid = FindObjectOfType<Grid>();
    }

    void Update()
    {
        if (ItemManager.Instance == null) return;
        if (grid == null) return;

        Vector3Int cell3 = grid.WorldToCell(transform.position);
        Vector2Int cell = new Vector2Int(cell3.x, cell3.y);

        var kind = ItemManager.Instance.GetStairKindAtCell(cell);

        if (kind == ItemManager.StairKind.None)
        {
            stayTimer = 0f;
            onStair = false;
            return;
        }

        if (onStair && cell3 == currentCell3)
        {
            stayTimer += Time.deltaTime;

            if (stayTimer >= holdTime)
            {
                TriggerStair(kind, cell);
                stayTimer = 0f;
                onStair = false;
            }
            return;
        }

        onStair = true;
        currentCell3 = cell3;
        stayTimer = 0f;
    }

    void TriggerStair(ItemManager.StairKind kind, Vector2Int cell)
    {
        int floor = ItemManager.Instance.currentFloor;

        // Escape（1Fのみ）
        if (kind == ItemManager.StairKind.Escape)
        {
            if (floor != 1) return;

            SaveCell(cell, kind);

            if (Time.timeSinceLevelLoad < escapeLockSeconds)
            {
                Debug.Log("まだ脱出できません");
                return;
            }

            Escape();
            return;
        }

        int delta = 0;

        // Entrance: 1F→2F, 2F→1F
        if (kind == ItemManager.StairKind.Entrance)
        {
            if (floor == 1) delta = +1;
            else if (floor == 2) delta = -1;
            else return;
        }

        // Mid: 2F→3F, 3F→2F
        if (kind == ItemManager.StairKind.Mid)
        {
            if (floor == 2) delta = +1;
            else if (floor == 3) delta = -1;
            else return;
        }

        if (delta == 0) return;

        SaveCell(cell, kind);
        ItemManager.Instance.ChangeFloor(delta);
    }

    void SaveCell(Vector2Int cell, ItemManager.StairKind kind)
    {
        ItemManager.Instance.hasLastStairCell = true;
        ItemManager.Instance.lastStairCell = cell;
        ItemManager.Instance.lastStairKind = kind;
        ItemManager.Instance.SaveRun();
    }

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