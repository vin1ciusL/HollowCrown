using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class LichAttack : MonoBehaviour
{
    [Header("Projetil")]
    public GameObject projectilePrefab;
    public Transform firePoint;

    [Header("Combate")]
    public float attackRange = 10f;
    public float fireRate = 1f;
    public float damage = 20f;

    [Header("Movimento")]
    public float moveSpeed = 1.5f;
    public float followDistance = 3f;

    [Header("Anti-Softlock")]
    public float stuckTimeThreshold = 1.5f;
    public float stuckSpeedThreshold = 0.15f;
    public float unstuckForce = 3f;

    [Header("Layers de Obstáculo")]
    [Tooltip("Quais layers são consideradas obstáculo para desvio (ex: Obstaculos)")]
    public LayerMask obstacleLayer = ~0;

    private float fireTimer = 0f;
    private Transform player;
    private Rigidbody2D rb;
    private LichAnimator lichAnimator;

    // Anti-softlock
    private float stuckTimer = 0f;
    private Vector2 lastPosition;
    private float unstuckCooldown = 0f;
    private Vector2 escapeDirection;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        lichAnimator = GetComponent<LichAnimator>();

        GameObject hero = GameObject.FindWithTag("Player");
        if (hero != null) player = hero.transform;

        lastPosition = rb.position;
    }

    void Update()
    {
        if (player == null)
        {
            GameObject hero = GameObject.FindWithTag("Player");
            if (hero != null) player = hero.transform;
        }

        VillainHealth alvo = EncontrarVilaoMaisProximo();
        if (alvo == null) return;

        float dist = Vector2.Distance(transform.position, alvo.transform.position);
        if (dist > attackRange) return;

        fireTimer += Time.deltaTime;
        if (fireTimer >= 1f / fireRate)
        {
            fireTimer = 0f;
            Atirar(alvo.transform);
        }

        DetectarSoftlock();
    }

    void FixedUpdate()
    {
        if (player == null || rb == null) return;

        // Se está em modo de escape (anti-softlock), usa o impulso
        if (unstuckCooldown > 0f)
        {
            unstuckCooldown -= Time.fixedDeltaTime;
            rb.MovePosition(rb.position + escapeDirection * unstuckForce * Time.fixedDeltaTime);
            return;
        }

        float dist = Vector2.Distance(rb.position, player.position);
        if (dist > followDistance)
        {
            Vector2 newPos = Vector2.MoveTowards(rb.position, player.position, moveSpeed * Time.fixedDeltaTime);
            rb.MovePosition(newPos);
        }
    }

    // ─── Anti-Softlock ──────────────────────────────────────────

    void DetectarSoftlock()
    {
        if (unstuckCooldown > 0f) return;
        if (player == null) { stuckTimer = 0f; return; }

        float distToPlayer = Vector2.Distance(rb.position, player.position);
        if (distToPlayer <= followDistance)
        {
            stuckTimer = 0f;
            lastPosition = rb.position;
            return;
        }

        float deslocamento = Vector2.Distance(rb.position, lastPosition);
        if (deslocamento < stuckSpeedThreshold * Time.deltaTime)
        {
            stuckTimer += Time.deltaTime;
            if (stuckTimer >= stuckTimeThreshold)
            {
                // Tenta escapar perpendicular
                Vector2 dirAlvo = ((Vector2)player.position - rb.position).normalized;
                float lado = Random.value > 0.5f ? 90f : -90f;
                float rad = (lado + Random.Range(-30f, 30f)) * Mathf.Deg2Rad;
                escapeDirection = new Vector2(
                    Mathf.Cos(rad) * dirAlvo.x - Mathf.Sin(rad) * dirAlvo.y,
                    Mathf.Sin(rad) * dirAlvo.x + Mathf.Cos(rad) * dirAlvo.y
                );

                // Verifica se a direção está obstruída
                RaycastHit2D hitCheck = Physics2D.Raycast(rb.position, escapeDirection, 2f, obstacleLayer);
                if (hitCheck.collider != null && hitCheck.collider.gameObject != gameObject)
                {
                    // Tenta o outro lado
                    rad = (-lado + Random.Range(-30f, 30f)) * Mathf.Deg2Rad;
                    escapeDirection = new Vector2(
                        Mathf.Cos(rad) * dirAlvo.x - Mathf.Sin(rad) * dirAlvo.y,
                        Mathf.Sin(rad) * dirAlvo.x + Mathf.Cos(rad) * dirAlvo.y
                    );
                }

                unstuckCooldown = 0.4f;
                stuckTimer = 0f;
                Debug.Log($"[LichAttack] Anti-softlock ativado");
            }
        }
        else
        {
            stuckTimer = 0f;
        }

        lastPosition = rb.position;
    }

    void Atirar(Transform alvo)
    {
        if (lichAnimator != null) lichAnimator.TriggerAttack();

        // Com projétil
        if (projectilePrefab != null && firePoint != null)
        {
            Vector2 dir = ((Vector2)alvo.position - (Vector2)firePoint.position).normalized;
            GameObject proj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
            LichProjectile lp = proj.GetComponent<LichProjectile>();
            if (lp != null) lp.Initialize(dir, damage);
            return;
        }

        // Sem projétil: dano direto se estiver perto
        VillainHealth vh = alvo.GetComponent<VillainHealth>();
        if (vh != null)
            vh.TakeDamage(damage);
    }

    VillainHealth EncontrarVilaoMaisProximo()
    {
        VillainHealth maisProximo = null;
        float menorDist = float.MaxValue;

        foreach (VillainHealth v in VillainHealth.All)
        {
            if (v == null) continue;
            float d = Vector2.Distance(transform.position, v.transform.position);
            if (d < menorDist)
            {
                menorDist = d;
                maisProximo = v;
            }
        }
        return maisProximo;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, followDistance);
    }
}
