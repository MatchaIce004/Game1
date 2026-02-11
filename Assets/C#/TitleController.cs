using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleController : MonoBehaviour
{
    [Header("Optional: if you add Continue button later")]
    public Button continueButton;

    void Start()
    {
        // 続きからボタンがある場合だけ、押せる/押せないを切り替え
        if (continueButton != null && ItemManager.Instance != null)
        {
            continueButton.interactable = ItemManager.Instance.CanContinue();
        }
    }

    // いまの「スタートボタン」はこれにつなぐ（=はじめから）
    public void OnStartNewGame()
    {
        // 仕様：図鑑は残すが、進行は新規開始
        // ※ ItemManager側にNewGame/ContinueGame/CanContinueを実装する前提
        if (ItemManager.Instance != null)
        {
            ItemManager.Instance.NewGame(); // LoadoutSceneへ飛ぶ想定
        }
        else
        {
            // まだItemManager改修前なら暫定でLoadoutへ
            SceneManager.LoadScene("LoadoutScene");
        }
    }

    // 「続きから」ボタンを追加したらこれにつなぐ
    public void OnContinue()
    {
        if (ItemManager.Instance != null)
        {
            ItemManager.Instance.ContinueGame(); // LoadoutSceneへ飛ぶ想定
        }
        else
        {
            Debug.LogWarning("ItemManagerがいないので続きからできません");
        }
    }

    public void OnQuit()
    {
        Application.Quit();
    }
}
