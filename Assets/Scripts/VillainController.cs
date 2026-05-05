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

    void Update()
    {
        // Tenta encontrar o herói se ainda não achou (ex: herói spawnado depois)
        if (!heroFound)
        {
            BuscarHeroi();
        }
    }

    void FixedUpdate()
    {
        if (!heroFound || hero == null)
        {
            // Herói não existe: fica parado
            rb.linearVelocity = Vector2.zero;
            return;
        }

        float distancia = Vector2.Distance(rb.position, hero.position);

        if (distancia > detectionRange)
        {
            // Fora do alcance: fica parado
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (distancia <= stopDistance)
        {
            // Perto demais: para (aqui você pode adicionar ataque depois)
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // Segue o herói
        Vector2 direcao = ((Vector2)hero.position - rb.position).normalized;
        rb.linearVelocity = direcao * moveSpeed;
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
