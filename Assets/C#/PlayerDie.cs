using UnityEngine;

public class PlayerDie : MonoBehaviour
{
    private void OnCollisionEnter2D(Collision2D other)
    {
        if (!other.gameObject.CompareTag("Enemy")) return;

        Debug.Log("プレイヤー死亡");

        if (ItemManager.Instance != null)
        {
            ItemManager.Instance.OnPlayerDied();
        }
    }
}
