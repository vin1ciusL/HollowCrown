using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class HeroController : MonoBehaviour
{
    [Header("Movimento")]
    public float moveSpeed = 5f;

    [Header("Detecção de Inimigos")]
    public float attackRange = 6f;
    public float stopDistance = 0.8f;
    public bool showGizmos = true;

    private Rigidbody2D rb;
    private Transform targetEnemy;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        gameObject.tag = "Player";
    }

    void Update()
    {
        targetEnemy = FindClosestEnemyInRange();
    }

    void FixedUpdate()
    {
        if (targetEnemy == null)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        float dist = Vector2.Distance(rb.position, targetEnemy.position);

        if (dist <= stopDistance)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 direcao = ((Vector2)targetEnemy.position - rb.position).normalized;
        rb.linearVelocity = direcao * moveSpeed;
    }

    Transform FindClosestEnemyInRange()
    {
        VillainController[] enemies = FindObjectsByType<VillainController>(FindObjectsSortMode.None);
        Transform closest = null;
        float closestDist = Mathf.Infinity;

        foreach (var enemy in enemies)
        {
            float dist = Vector2.Distance(rb.position, enemy.transform.position);
            if (dist <= attackRange && dist < closestDist)
            {
                closestDist = dist;
                closest = enemy.transform;
            }
        }

        return closest;
    }

    void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}