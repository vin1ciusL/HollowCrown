using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class MageAlly : MonoBehaviour
{
    [Header("Projetil")]
    public GameObject fireballPrefab;
    public Transform firePoint;

    [Header("Combate")]
    public float attackRange = 9f;
    public float fireRate = 0.7f;
    public float damage = 12f;

    [Header("Movimento")]
    public float moveSpeed = 1.8f;
    public float followDistance = 3.5f;

    private float fireTimer = 0f;
    private Transform player;
    private Rigidbody2D rb;
    private MageAnimator mageAnimator;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        mageAnimator = GetComponent<MageAnimator>();

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
            Vector2 newPos = Vector2.MoveTowards(
                rb.position, player.position, moveSpeed * Time.fixedDeltaTime);
            rb.MovePosition(newPos);
        }
    }

    void Atirar(Transform alvo)
    {
        if (mageAnimator != null) mageAnimator.TriggerCast();

        if (fireballPrefab != null && firePoint != null)
        {
            Vector2 dir = ((Vector2)alvo.position - (Vector2)firePoint.position).normalized;
            GameObject proj = Instantiate(fireballPrefab, firePoint.position, Quaternion.identity);
            Fireball fb = proj.GetComponent<Fireball>();
            if (fb != null) fb.Initialize(dir, damage, ally: true);
            return;
        }

        VillainHealth vh = alvo.GetComponent<VillainHealth>();
        if (vh != null) vh.TakeDamage(damage);
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
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, followDistance);
    }
}
