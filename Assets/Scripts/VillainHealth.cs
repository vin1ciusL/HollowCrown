using UnityEngine;

public class VillainHealth : MonoBehaviour
{
    [Header("Vida")]
    public float maxHealth = 10f;
    public float currentHealth = 10f;

    [Header("Combate")]
    public float attackDamage = 5f;
    public float attackRange = 1.5f;
    public float attackCooldown = 1f;

    private float attackTimer = 0f;
    private VillainAnimator villainAnimator;
    private Rigidbody2D rb;

    void Start()
    {
        currentHealth = maxHealth;
        attackTimer = Random.Range(0f, attackCooldown);
        villainAnimator = GetComponent<VillainAnimator>();
        rb = GetComponent<Rigidbody2D>();
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
        Vector2 attackDir = GetAttackDirection();
        Vector2 attackPos = (Vector2)transform.position + attackDir * attackRange * 0.5f;

        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPos, attackRange * 0.6f);

        foreach (Collider2D hit in hits)
        {
            HeroHealth hero = hit.GetComponent<HeroHealth>();
            if (hero != null)
            {
                hero.TakeDamage(attackDamage);
                attackTimer = 0f;

                if (villainAnimator != null)
                    villainAnimator.TriggerAttack();

                break;
            }
        }
    }

    Vector2 GetAttackDirection()
    {
        // Vilão ataca na direção do herói
        GameObject hero = GameObject.FindWithTag("Player");
        if (hero != null)
        {
            Vector2 dir = ((Vector2)hero.transform.position - (Vector2)transform.position).normalized;
            return dir;
        }

        // Fallback: direção do movimento
        if (rb != null && rb.linearVelocity.magnitude > 0.1f)
            return rb.linearVelocity.normalized;

        return Vector2.down;
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        if (currentHealth <= 0)
            Die();
    }

    void Die()
    {
        Destroy(gameObject);
    }
}
