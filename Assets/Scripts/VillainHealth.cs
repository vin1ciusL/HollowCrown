using UnityEngine;

public class VillainHealth : MonoBehaviour
{
    [Header("Vida")]
    public float maxHealth = 10f;
    public float currentHealth = 10f;

    [Header("Combate")]
    public float attackDamage = 5f;
    public float attackRange = 1f;
    public float attackCooldown = 1f;

    private float attackTimer = 0f;

    void Start()
    {
        currentHealth = maxHealth;
        attackTimer = Random.Range(0f, attackCooldown); // evita todos atacarem ao mesmo tempo
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
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, attackRange);

        foreach (Collider2D hit in hits)
        {
            HeroHealth hero = hit.GetComponent<HeroHealth>();
            if (hero != null)
            {
                hero.TakeDamage(attackDamage);
                attackTimer = 0f;
                break;
            }
        }
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        Debug.Log($"Vilão levou {damage} de dano! Vida restante: {currentHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log("Vilão morreu!");
        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
