using UnityEngine;

public class HeroHealth : MonoBehaviour
{
    [Header("Vida")]
    public float maxHealth = 100f;
    public float currentHealth = 100f;

    [Header("Combate")]
    public float attackDamage = 10f;
    public float attackRange = 1f;
    public float attackCooldown = 1f;

    private float attackTimer = 0f;

    void Start()
    {
        currentHealth = maxHealth;
    }

    void Update()
    {
        attackTimer += Time.deltaTime;

        if (attackTimer >= attackCooldown)
        {
            TryAttack();
        }
    }

    void TryAttack()
    {
        // Procura vilões no raio de ataque
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, attackRange);

        foreach (Collider2D hit in hits)
        {
            VillainHealth villain = hit.GetComponent<VillainHealth>();
            if (villain != null)
            {
                villain.TakeDamage(attackDamage);
                attackTimer = 0f;
                break; // ataca um por vez
            }
        }
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        Debug.Log($"Herói levou {damage} de dano! Vida restante: {currentHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log("Herói morreu!");
        // Aqui você pode adicionar game over depois
        gameObject.SetActive(false);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
