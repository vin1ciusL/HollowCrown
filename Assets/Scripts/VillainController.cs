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

    [Header("Debug")]
    public bool showGizmos = true;

    private Rigidbody2D rb;
    private Transform hero;
    private bool heroFound = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
    }

    void Start()
    {
        BuscarHeroi();
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
    }

    void FixedUpdate()
    {
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
        RaycastHit2D[] hits = Physics2D.RaycastAll(rb.position, direcao, distancia);
        foreach (var hit in hits)
        {
            if (hit.collider.gameObject == gameObject) continue;
            if (hit.collider.isTrigger) continue;
            if (hit.collider.CompareTag("Player")) continue;
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
