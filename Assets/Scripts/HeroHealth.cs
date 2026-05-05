using UnityEngine;

public class HeroHealth : MonoBehaviour
{
    [Header("Vida")]
    public float maxHealth = 100f;
    public float currentHealth = 100f;

    [Header("Combate")]
    public float attackDamage = 10f;
    public float attackRange = 1.5f;
    public float attackCooldown = 1f;

    private float attackTimer = 0f;
    private HeroAnimator heroAnimator;

    void Start()
    {
        currentHealth = maxHealth;
        heroAnimator = GetComponent<HeroAnimator>();
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
        // Pega a direção que o herói está olhando
        Vector2 attackDir = GetAttackDirection();

        // Posição do ataque na frente do herói
        Vector2 attackPos = (Vector2)transform.position + attackDir * attackRange * 0.5f;

        // Detecta inimigos numa área na frente
        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPos, attackRange * 0.6f);

        foreach (Collider2D hit in hits)
        {
            VillainHealth villain = hit.GetComponent<VillainHealth>();
            if (villain != null)
            {
                villain.TakeDamage(attackDamage);
                attackTimer = 0f;

                if (heroAnimator != null)
                    heroAnimator.TriggerAttack();

                break;
            }
        }
    }

    Vector2 GetAttackDirection()
    {
        if (heroAnimator == null) return Vector2.down;
        return heroAnimator.GetFacingDirection();
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        Debug.Log($"Herói levou {damage} de dano! Vida: {currentHealth}");

        if (currentHealth <= 0)
            Die();
    }

    void Die()
    {
        Debug.Log("Herói morreu!");
        gameObject.SetActive(false);
    }

    void OnDrawGizmosSelected()
    {
        if (heroAnimator == null) return;
        Vector2 dir = heroAnimator.GetFacingDirection();
        Vector2 pos = (Vector2)transform.position + dir * attackRange * 0.5f;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(pos, attackRange * 0.6f);
    }
}
