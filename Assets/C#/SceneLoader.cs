using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public void LoadTitle()
    {
        SceneManager.LoadScene("TitleScene");
    }

    public void LoadGame()
    {
        SceneManager.LoadScene("GameScene");
    }

    public void LoadResult()
    {
        SceneManager.LoadScene("ResultScene");
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}

