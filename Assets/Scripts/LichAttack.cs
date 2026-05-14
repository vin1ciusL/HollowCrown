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

    private float fireTimer = 0f;
    private Transform player;
    private Rigidbody2D rb;
    private LichAnimator lichAnimator;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        lichAnimator = GetComponent<LichAnimator>();

        GameObject hero = GameObject.FindWithTag("Player");
        if (hero != null) player = hero.transform;
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
    }

    void FixedUpdate()
    {
        if (player == null || rb == null) return;

        float dist = Vector2.Distance(rb.position, player.position);
        if (dist > followDistance)
        {
            Vector2 newPos = Vector2.MoveTowards(rb.position, player.position, moveSpeed * Time.fixedDeltaTime);
            rb.MovePosition(newPos);
        }
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
