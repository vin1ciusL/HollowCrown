using UnityEngine;

public class MageAttack : MonoBehaviour
{
    public GameObject fireballPrefab;
    public Transform firePoint;

    [Header("Ataque")]
    public float attackRange = 8f;
    public float fireRate = 0.5f;
    public float damage = 0f;

    [Header("Movimento")]
    public float moveSpeed = 1.5f;
    public float minDistance = 4f;

    [Header("Limites do Mapa (deixe zerado para usar a câmera)")]
    public Vector2 mapMin = Vector2.zero;
    public Vector2 mapMax = Vector2.zero;

    private float fireTimer = 0f;
    private Transform target;
    private MageAnimator mageAnimator;
    private Rigidbody2D rb;

    void Start()
    {
        mageAnimator = GetComponent<MageAnimator>();
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
        }
        Debug.Log($"[MageAttack] Spawn em {transform.position} | fireball={(fireballPrefab!=null)} firePoint={(firePoint!=null)}");
    }

    void Update()
    {
        target = EncontrarAlvoMaisProximo();
        if (target == null) return;

        float dist = Vector2.Distance(transform.position, target.position);
        if (dist > attackRange) return;

        fireTimer += Time.deltaTime;
        if (fireTimer >= 1f / fireRate)
        {
            fireTimer = 0f;
            CastFireball();
        }
    }

    Transform EncontrarAlvoMaisProximo()
    {
        Transform alvo = null;
        float menorDist = float.MaxValue;

        GameObject heroObj = GameObject.FindWithTag("Player");
        if (heroObj != null)
        {
            float d = Vector2.Distance(transform.position, heroObj.transform.position);
            if (d < menorDist) { menorDist = d; alvo = heroObj.transform; }
        }

        GolemHealth golem = Object.FindAnyObjectByType<GolemHealth>();
        if (golem != null)
        {
            float d = Vector2.Distance(transform.position, golem.transform.position);
            if (d < menorDist) { menorDist = d; alvo = golem.transform; }
        }

        LichHealth lich = LichHealth.Instance;
        if (lich != null)
        {
            float d = Vector2.Distance(transform.position, lich.transform.position);
            if (d < menorDist) { menorDist = d; alvo = lich.transform; }
        }

        return alvo;
    }

    void FixedUpdate()
    {
        if (target == null || rb == null) return;

        float dist = Vector2.Distance(rb.position, (Vector2)target.position);
        if (dist > minDistance)
        {
            Vector2 newPos = Vector2.MoveTowards(
                rb.position, target.position, moveSpeed * Time.fixedDeltaTime);
            rb.MovePosition(newPos);
        }
    }

    public void CastFireball()
    {
        if (fireballPrefab == null) { Debug.LogError("[MageAttack] fireballPrefab nao atribuido!"); return; }
        if (firePoint == null)      { Debug.LogError("[MageAttack] firePoint nao atribuido!");     return; }
        if (target == null)         return;

        if (mageAnimator != null)
            mageAnimator.TriggerCast();

        Vector2 direction = (target.position - firePoint.position).normalized;
        GameObject fireball = Instantiate(fireballPrefab, firePoint.position, Quaternion.identity);
        Fireball fb = fireball.GetComponent<Fireball>();
        if (fb != null)
        {
            fb.Initialize(direction, damage);
            Debug.Log($"[MageAttack] Disparou fireball -> {target.name} (dist={Vector2.Distance(transform.position, target.position):F1})");
        }
        else
            Debug.LogError("[MageAttack] Script Fireball.cs NAO encontrado no prefab!");
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, minDistance);
    }
}