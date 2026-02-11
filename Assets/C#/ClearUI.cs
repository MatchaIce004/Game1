using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class ClearUI : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI messageText;

    void Start()
    {
        // いまは最小：クリアメッセージだけ
        if (messageText != null)
        {
            messageText.text = "CLEAR!";
        }

        // 後で表示したい値の例（今はログだけ）
        float time = GameTimer.Instance != null ? GameTimer.Instance.GetTime() : 0f;
        Debug.Log($"[CLEAR] Time={time:F2}s");
    }

    public void OnGoTitle()
    {
        // タイマー止めたいなら
        if (GameTimer.Instance != null) GameTimer.Instance.StopTimer();

        SceneManager.LoadScene("TitleScene");
    }
}
