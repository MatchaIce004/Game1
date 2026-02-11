using System.Collections;
using UnityEngine;
using TMPro;

public class PlayerHealth : MonoBehaviour
{
    [Header("HP")]
    public int maxHP = 3;
    public int currentHP = 3;

    [Header("Damage")]
    public float invincibleSeconds = 0.6f;
    private bool invincible = false;

    [Header("UI (optional)")]
    public TextMeshProUGUI hpText; // 数字表示したい場合だけセット

    void Start()
    {
        currentHP = Mathf.Clamp(currentHP, 1, maxHP);
        RefreshUI();
    }

    public void SetFullHP()
    {
        currentHP = maxHP;
        RefreshUI();
    }

    public bool CanTakeDamage() => !invincible;

    public void TakeDamage(int amount)
    {
        if (amount <= 0) return;
        if (invincible) return;

        currentHP -= amount;
        currentHP = Mathf.Max(0, currentHP);
        RefreshUI();

        if (currentHP <= 0)
        {
            Die();
            return;
        }

        StartCoroutine(InvincibleCoroutine());
    }

    IEnumerator InvincibleCoroutine()
    {
        invincible = true;
        yield return new WaitForSeconds(invincibleSeconds);
        invincible = false;
    }

    void Die()
    {
        // ここで死亡イベント
        if (ItemManager.Instance != null)
        {
            ItemManager.Instance.OnPlayerDied();
        }
        else
        {
            Debug.LogError("ItemManagerが見つかりません");
        }
    }

    void RefreshUI()
    {
        if (hpText != null)
        {
            hpText.text = $"HP: {currentHP}/{maxHP}";
        }
    }
}
