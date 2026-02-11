using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class TitleUI : MonoBehaviour
{
    public Button continueButton;

    void Start()
    {
        if (continueButton != null && ItemManager.Instance != null)
        {
            continueButton.interactable = ItemManager.Instance.CanContinue();
        }

        // クリア済み表示はUI後回しならログでOK
        var p = GameProgress.Load();
        if (p.hasClearedOnce)
        {
            Debug.Log("クリア済みデータあり（UIは後で）");
        }
    }

    public void OnNewGame()
    {
        if (ItemManager.Instance != null)
        {
            // 進行完全リセット（図鑑は残る）
            ItemManager.Instance.NewGame();
            GameProgress.Clear(); // クリアフラグも消すなら
        }
    }

    public void OnContinue()
    {
        if (ItemManager.Instance != null)
        {
            // 続きから＝Loadoutから
            ItemManager.Instance.ContinueGame();
        }
    }

    public void OnQuit()
    {
        Application.Quit();
    }
}
