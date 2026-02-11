using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class ResultUI : MonoBehaviour
{
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI itemListText;

    void Start()
    {
        float time = GameTimer.Instance != null ? GameTimer.Instance.GetTime() : 0f;
        if (timeText != null) timeText.text = $"Time : {time:F2} sec";

        ShowItemList();
    }

    void ShowItemList()
    {
        if (itemListText == null) return;

        if (ItemManager.Instance == null)
        {
            itemListText.text = "ItemManagerが見つかりません";
            return;
        }

        var loot = ItemManager.Instance.runPendingLoot;

        if (loot == null || loot.Count == 0)
        {
            itemListText.text = "（今回持ち帰ったアイテムはありません）";
            return;
        }

        itemListText.text = "";
        foreach (var item in loot)
        {
            if (item == null) continue;
            itemListText.text += $"・{item.itemName}\n";
        }
    }

    // 「持ち帰る（確定）」ボタン
    public void OnConfirmReturnToLoadout()
    {
        if (GameTimer.Instance != null) GameTimer.Instance.StopTimer();

        if (ItemManager.Instance != null)
        {
            // ★ここで確定！
            ItemManager.Instance.OnEscapeSuccess();
        }

        SceneManager.LoadScene("LoadoutScene");
    }

    // 「タイトルへ」ボタン（確定しない）
    public void OnGoToTitle()
    {
        if (GameTimer.Instance != null) GameTimer.Instance.StopTimer();

        // runPendingLootは確定しないので、戻りたいなら消しても良い
        // ※仕様次第。いったん消しておくと混乱しにくい
        if (ItemManager.Instance != null)
        {
            ItemManager.Instance.runPendingLoot.Clear();
            ItemManager.Instance.runLoadout.Clear();
            ItemManager.Instance.ClearRunSave();
        }

        SceneManager.LoadScene("TitleScene");
    }
}
