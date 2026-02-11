using UnityEngine;

public class GameTimer : MonoBehaviour
{
    public static GameTimer Instance;

    private float elapsedTime = 0f;
    private bool isRunning = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        if (isRunning)
            elapsedTime += Time.deltaTime;
    }

    public void ResetTimer()
    {
        elapsedTime = 0f;
    }

    public void StartTimer()
    {
        isRunning = true;
    }

    public void StopTimer()
    {
        isRunning = false;
    }

    public float GetTime()
    {
        return elapsedTime;
    }

    // Continue用（ItemManagerが呼ぶ）
    public void SetTime(float t)
    {
        elapsedTime = Mathf.Max(0f, t);
    }
}
