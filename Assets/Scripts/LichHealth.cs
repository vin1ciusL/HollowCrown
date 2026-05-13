using UnityEngine;

public class LichHealth : MonoBehaviour
{
    public float maxHealth = 80f;
    public float currentHealth;

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        if (currentHealth <= 0)
            Destroy(gameObject);
    }
}
