using UnityEngine;

public class LichHealth : MonoBehaviour
{
    public static LichHealth Instance { get; private set; }

    public float maxHealth = 80f;
    public float currentHealth;

    void OnEnable()
    {
        Instance = this;
        currentHealth = maxHealth;
    }

    void OnDisable()
    {
        if (Instance == this) Instance = null;
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        if (currentHealth <= 0)
            Destroy(gameObject);
    }
}
