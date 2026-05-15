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
    private Transform golem;
    private Transform mage;
    private float retargetTimer = 0f;
    private const float retargetInterval = 0.5f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
    }

    void Start()
    {
        BuscarAlvos();
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
            // Ignora qualquer entidade viva (não bloqueia path)
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
