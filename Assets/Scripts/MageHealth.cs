using UnityEngine;

public class MageHealth : MonoBehaviour
{
    [Header("Vida")]
    public float maxHealth = 50f;
    public float currentHealth;

    [Header("Invulnerabilidade")]
    public float invulnerabilityDuration = 0.4f;

    private float invulnTimer = 0f;

    void Start()
    {
        currentHealth = maxHealth;
    }

    void Update()
    {
        if (invulnTimer > 0f)
            invulnTimer -= Time.deltaTime;
    }

    public void TakeDamage(float damage)
    {
        if (invulnTimer > 0f) return;

        currentHealth -= damage;
        invulnTimer = invulnerabilityDuration;
        Debug.Log($"Mago levou {damage} de dano! Vida: {currentHealth}");

        if (currentHealth <= 0)
            Die();
    }

    void Die()
    {
        Debug.Log("Mago morreu!");
        gameObject.SetActive(false);
    }
}
