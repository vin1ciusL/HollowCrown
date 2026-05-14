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

    [Header("Anti-Softlock")]
    [Tooltip("Tempo parado antes de considerar como preso")]
    public float stuckTimeThreshold = 1.2f;
    [Tooltip("Velocidade mínima para não ser considerado parado")]
    public float stuckSpeedThreshold = 0.15f;
    [Tooltip("Força do impulso de escape")]
    public float unstuckForce = 5f;

    [Header("Layers de Obstáculo")]
    [Tooltip("Quais layers são consideradas obstáculo para desvio (ex: Obstaculos)")]
    public LayerMask obstacleLayer = ~0; // padrão: todas as layers

    private Rigidbody2D rb;
    private Transform targetEnemy;

    // Anti-softlock
    private float stuckTimer = 0f;
    private Vector2 lastPosition;
    private float unstuckCooldown = 0f;
    private Vector2 escapeDirection;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        gameObject.tag = "Player";
        lastPosition = transform.position;
    }

    void Update()
    {
        targetEnemy = FindClosestEnemyInRange();
        DetectarSoftlock();
    }

    void FixedUpdate()
    {
        // Se está em modo de escape (anti-softlock), usa o impulso
        if (unstuckCooldown > 0f)
        {
            unstuckCooldown -= Time.fixedDeltaTime;
            rb.linearVelocity = escapeDirection * unstuckForce;
            return;
        }

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

        // Tenta desviar de obstáculos sólidos
        Vector2 direcaoFinal = GetDirecaoDesviando(direcao);
        rb.linearVelocity = direcaoFinal * moveSpeed;
    }

    // ─── Anti-Softlock ──────────────────────────────────────────

    void DetectarSoftlock()
    {
        if (unstuckCooldown > 0f) return;
        if (targetEnemy == null) { stuckTimer = 0f; return; }

        float dist = Vector2.Distance(rb.position, targetEnemy.position);
        if (dist <= stopDistance)
        {
            stuckTimer = 0f;
            return;
        }

        float deslocamento = Vector2.Distance(rb.position, lastPosition);
        if (deslocamento < stuckSpeedThreshold * Time.deltaTime)
        {
            stuckTimer += Time.deltaTime;
            if (stuckTimer >= stuckTimeThreshold)
            {
                AplicarEscape();
                stuckTimer = 0f;
            }
        }
        else
        {
            stuckTimer = 0f;
        }

        lastPosition = rb.position;
    }

    void AplicarEscape()
    {
        if (targetEnemy == null) return;

        Vector2 dirAlvo = ((Vector2)targetEnemy.position - rb.position).normalized;
        float lado = Random.value > 0.5f ? 90f : -90f;
        escapeDirection = Rotacionar(dirAlvo, lado + Random.Range(-30f, 30f));

        if (EstaObstruido(escapeDirection, 2f))
        {
            escapeDirection = Rotacionar(dirAlvo, -lado + Random.Range(-30f, 30f));
            if (EstaObstruido(escapeDirection, 2f))
                escapeDirection = -dirAlvo;
        }

        unstuckCooldown = 0.5f;
        Debug.Log($"[HeroController] Anti-softlock ativado em {gameObject.name}");
    }

    // ─── Desvio de obstáculos ───────────────────────────────────

    Vector2 GetDirecaoDesviando(Vector2 direcaoIdeal)
    {
        if (!EstaObstruido(direcaoIdeal, 1.2f))
            return direcaoIdeal;

        for (int angulo = 20; angulo <= 160; angulo += 20)
        {
            Vector2 direita = Rotacionar(direcaoIdeal, angulo);
            if (!EstaObstruido(direita, 1.2f))
                return direita;

            Vector2 esquerda = Rotacionar(direcaoIdeal, -angulo);
            if (!EstaObstruido(esquerda, 1.2f))
                return esquerda;
        }

        return direcaoIdeal;
    }

    bool EstaObstruido(Vector2 direcao, float distancia)
    {
        RaycastHit2D hit = Physics2D.Raycast(rb.position, direcao, distancia, obstacleLayer);
        // Ignora a si mesmo (caso esteja na mesma layer)
        if (hit.collider != null && hit.collider.gameObject != gameObject && !hit.collider.isTrigger)
            return true;
        return false;
    }

    Vector2 Rotacionar(Vector2 v, float graus)
    {
        float rad = graus * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(cos * v.x - sin * v.y, sin * v.x + cos * v.y);
    }

    // ─── Busca de inimigos ──────────────────────────────────────

    Transform FindClosestEnemyInRange()
    {
        Transform closest = null;
        float closestDist = Mathf.Infinity;

        foreach (var villain in VillainHealth.All)
        {
            float dist = Vector2.Distance(rb.position, villain.transform.position);
            if (dist <= attackRange && dist < closestDist)
            {
                closestDist = dist;
                closest = villain.transform;
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