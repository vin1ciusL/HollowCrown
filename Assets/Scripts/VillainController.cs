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

    [Header("Elite")]
    [Tooltip("Multiplicador de knockback recebido (0 = imune, 1 = normal)")]
    [Range(0f, 1f)]
    public float resistenciaKnockback = 1f;

    [Header("Debug")]
    public bool showGizmos = true;

    private Rigidbody2D rb;
    private Transform hero;
    private Transform golem;
    private Transform mage;
    private float retargetTimer = 0f;
    private const float retargetInterval = 0.5f;

    // Anti-softlock
    private float stuckTimer = 0f;
    private Vector2 lastPosition;
    private float unstuckCooldown = 0f;
    private Vector2 escapeDirection;

    // Knockback
    private float knockbackTimer = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
    }

    void Start()
    {
        BuscarAlvos();
        lastPosition = rb.position;
    }

    void BuscarAlvos()
    {
        if (hero == null)
        {
            GameObject heroObj = GameObject.FindWithTag("Player");
            hero = heroObj != null ? heroObj.transform : null;
        }

        if (golem == null)
        {
            GolemHealth g = Object.FindAnyObjectByType<GolemHealth>();
            golem = (g != null && g.gameObject.activeInHierarchy) ? g.transform : null;
        }

        if (mage == null)
        {
            MageHealth m = Object.FindAnyObjectByType<MageHealth>();
            mage = (m != null && m.gameObject.activeInHierarchy) ? m.transform : null;
        }
    }

    Transform AlvoMaisProximo()
    {
        Transform alvo = null;
        float menorDist = float.MaxValue;

        ConsiderarAlvo(hero, ref alvo, ref menorDist);
        ConsiderarAlvo(golem, ref alvo, ref menorDist);
        ConsiderarAlvo(mage, ref alvo, ref menorDist);

        LichHealth lich = LichHealth.Instance;
        if (lich != null)
            ConsiderarAlvo(lich.transform, ref alvo, ref menorDist);

        return alvo;
    }

    void ConsiderarAlvo(Transform t, ref Transform alvo, ref float menorDist)
    {
        if (t == null || !t.gameObject.activeInHierarchy) return;
        float d = Vector2.Distance(rb.position, t.position);
        if (d < menorDist)
        {
            menorDist = d;
            alvo = t;
        }
    }

    void Update()
    {
        retargetTimer += Time.deltaTime;
        if (retargetTimer >= retargetInterval)
        {
            retargetTimer = 0f;
            BuscarAlvos();
        }

        DetectarSoftlock();
    }

    void FixedUpdate()
    {
        // Knockback recente: deixa a velocidade aplicada decair naturalmente,
        // sem sobrescrever com movimento. Drag do Rigidbody2D ou colisões
        // amortecem o impulso.
        if (knockbackTimer > 0f)
        {
            knockbackTimer -= Time.fixedDeltaTime;
            return;
        }

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
        RaycastHit2D[] hits = Physics2D.RaycastAll(rb.position, direcao, distancia, obstacleLayer);
        foreach (var hit in hits)
        {
            if (hit.collider.gameObject == gameObject) continue;
            if (hit.collider.isTrigger) continue;
            GameObject go = hit.collider.gameObject;
            if (go.GetComponentInParent<HeroHealth>() != null) continue;
            if (go.GetComponentInParent<GolemHealth>() != null) continue;
            if (go.GetComponentInParent<MageHealth>() != null) continue;
            if (go.GetComponentInParent<LichHealth>() != null) continue;
            if (go.GetComponentInParent<VillainHealth>() != null) continue;
            return true;
        }
        return false;
    }

    Vector2 Rotacionar(Vector2 v, float graus)
    {
        float rad = graus * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(cos * v.x - sin * v.y, sin * v.x + cos * v.y);
    }

    void OnHeroLost()
    {
        hero = null;
    }

    /// <summary>
    /// Aplica knockback no vilão. Sobrescreve a velocidade atual e bloqueia
    /// o movimento da IA por <paramref name="duracao"/> segundos, deixando o
    /// impulso decair naturalmente.
    /// </summary>
    public void AplicarKnockback(Vector2 direcao, float forca, float duracao = 0.2f)
    {
        if (forca <= 0f) return;
        if (rb == null) return;

        forca *= resistenciaKnockback;
        if (forca < 0.05f) return;

        rb.linearVelocity = direcao.normalized * forca;
        knockbackTimer = duracao * resistenciaKnockback;
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
