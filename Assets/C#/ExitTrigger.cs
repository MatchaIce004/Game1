using UnityEngine;
using UnityEngine.SceneManagement;

public class ExitTrigger : MonoBehaviour
{
    public float requireTime = 10f; // 10秒経たないと脱出不可

    private bool escaped = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (escaped) return;
        if (!other.CompareTag("Player")) return;

        if (ItemManager.Instance != null && ItemManager.Instance.currentFloor != 1)
        {
            Debug.Log("1層以外では脱出できません");
            return;
        }

        float timeNow = GameTimer.Instance != null ? GameTimer.Instance.GetTime() : 0f;

        if (timeNow < requireTime)
        {
            Debug.Log($"脱出まであと {requireTime - timeNow:F1} 秒必要");
            return;
        }

        escaped = true;

        Debug.Log("脱出成功！");

        // 時間保存（後で表示したいなら使える）
        PlayerPrefs.SetFloat("PlayTime", timeNow);
        PlayerPrefs.Save();

        // ランの確定処理（倉庫へ、run消す等）
        if (ItemManager.Instance != null)
        {
            // ★ お宝ありならクリアへ
            bool hasTreasure = ItemManager.Instance.runHasTreasure;

            ItemManager.Instance.OnEscapeSuccess(); // ここで runHasTreasure は false に戻る設計

            if (hasTreasure)
            {
                SceneManager.LoadScene("ClearScene");
                return;
            }
        }

        // お宝なし → 通常リザルト
        SceneManager.LoadScene("ResultScene");
    }
}
