using UnityEngine;

public class DamageOnTouch : MonoBehaviour
{
    public int damage = 1;

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (!other.collider.CompareTag("Player")) return;

        var hp = other.collider.GetComponent<PlayerHealth>();
        if (hp == null) return;

        hp.TakeDamage(damage);
    }
}

