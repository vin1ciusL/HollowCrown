using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class VillainController : MonoBehaviour
{
    [Header("Movimento")]
    public float moveSpeed = 3f;

    [Header("Detecção")]
    [Tooltip("Distância máxima para começar a seguir o herói")]
    public float detectionRange = 8f;
    [Tooltip("Distância mínima para parar perto do herói")]
    public float stopDistance = 0.8f;

    [Header("Anti-Softlock")]
    [Tooltip("Tempo parado antes de considerar como preso")]
    public float stuckTimeThreshold = 1.5f;
    [Tooltip("Velocidade mínima para não ser considerado parado")]
    public float stuckSpeedThreshold = 0.15f;
    [Tooltip("Força do impulso de escape")]
    public float unstuckForce = 4f;

    [Header("Layers de Obstáculo")]
    [Tooltip("Quais layers são consideradas obstáculo para desvio (ex: Obstaculos)")]
    public LayerMask obstacleLayer = ~0;

    [Header("Debug")]
    public bool showGizmos = true;

    private Rigidbody2D rb;
    private Transform hero;
    private bool heroFound = false;

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
    }

    void Start()
    {
        BuscarHeroi();
        lastPosition = rb.position;
    }

    void BuscarHeroi()
    {
        GameObject heroObj = GameObject.FindWithTag("Player");
        if (heroObj != null)
        {
            hero = heroObj.transform;
            heroFound = true;
        }
        else
        {
            heroFound = false;
        }
    }

    Transform AlvoMaisProximo()
    {
        Transform alvo = null;
        float menorDist = float.MaxValue;

        if (hero != null)
        {
            menorDist = Vector2.Distance(rb.position, hero.position);
            alvo = hero;
        }

        LichHealth lich = LichHealth.Instance;
        if (lich != null)
        {
            float d = Vector2.Distance(rb.position, lich.transform.position);
            if (d < menorDist)
                alvo = lich.transform;
        }

        return alvo;
    }

    void Update()
    {
        if (!heroFound)
            BuscarHeroi();

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

        Transform alvo = AlvoMaisProximo();

        if (alvo == null)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        float distancia = Vector2.Distance(rb.position, alvo.position);

        if (distancia > detectionRange)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (distancia <= stopDistance)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 direcaoIdeal = ((Vector2)alvo.position - rb.position).normalized;
        Vector2 direcaoFinal = GetDirecaoDesviando(direcaoIdeal);
        rb.linearVelocity = direcaoFinal * moveSpeed;
    }

    // ─── Sistema Anti-Softlock ──────────────────────────────────

    void DetectarSoftlock()
    {
        if (unstuckCooldown > 0f) return; // já está escapando

        Transform alvo = AlvoMaisProximo();
        if (alvo == null) { stuckTimer = 0f; return; }

        float distancia = Vector2.Distance(rb.position, alvo.position);

        // Só detecta softlock quando deveria estar se movendo
        if (distancia <= stopDistance || distancia > detectionRange)
        {
            stuckTimer = 0f;
            return;
        }

        // Verifica se está efetivamente parado
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
        // Tenta direções perpendiculares ao alvo, com aleatoriedade
        Transform alvo = AlvoMaisProximo();
        if (alvo == null) return;

        Vector2 dirAlvo = ((Vector2)alvo.position - rb.position).normalized;

        // Tenta perpendicular (esquerda/direita aleatória) + componente para trás
        float lado = Random.value > 0.5f ? 90f : -90f;
        escapeDirection = Rotacionar(dirAlvo, lado + Random.Range(-30f, 30f));

        // Verifica se a direção de escape está obstruída
        if (EstaObstruido(escapeDirection, 2f))
        {
            escapeDirection = Rotacionar(dirAlvo, -lado + Random.Range(-30f, 30f));
            if (EstaObstruido(escapeDirection, 2f))
                escapeDirection = -dirAlvo; // Fallback: recuar
        }

        unstuckCooldown = 0.5f;
        Debug.Log($"[VillainController] Anti-softlock ativado em {gameObject.name}");
    }

    // ─── Pathfinding com desvio ─────────────────────────────────

    Vector2 GetDirecaoDesviando(Vector2 direcaoIdeal)
    {
        // Caminho direto está livre?
        if (!EstaObstruido(direcaoIdeal, 1.2f))
            return direcaoIdeal;

        // Tenta ângulos crescentes para esquerda e direita
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

    // Chamado quando o herói é destruído ou desativado
    void OnHeroLost()
    {
        heroFound = false;
        hero = null;
    }

    void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;

        // Círculo de detecção (amarelo)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Círculo de parada (vermelho)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, stopDistance);
    }
}
