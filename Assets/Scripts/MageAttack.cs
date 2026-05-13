using UnityEngine;

public class MageAttack : MonoBehaviour
{
    public GameObject fireballPrefab;
    public Transform firePoint;

    [Header("Ataque")]
    public float attackRange = 8f;
    public float fireRate = 0.5f;
    public float damage = 15f;

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
    private Vector2 boundsMin;
    private Vector2 boundsMax;

    void Start()
    {
        mageAnimator = GetComponent<MageAnimator>();
        rb = GetComponent<Rigidbody2D>();

        // Se os limites não foram definidos, usa os bounds da câmera
        if (mapMin == Vector2.zero && mapMax == Vector2.zero)
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                boundsMin = cam.ViewportToWorldPoint(new Vector3(0, 0, 0));
                boundsMax = cam.ViewportToWorldPoint(new Vector3(1, 1, 0));
            }
        }
        else
        {
            boundsMin = mapMin;
            boundsMax = mapMax;
        }
    }

    void Update()
    {
        if (target == null)
        {
            GameObject hero = GameObject.FindWithTag("Player");
            if (hero != null) target = hero.transform;
            else return;
        }

        float dist = Vector2.Distance(transform.position, target.position);

        if (dist > attackRange) return;

        fireTimer += Time.deltaTime;
        if (fireTimer >= 1f / fireRate)
        {
            fireTimer = 0f;
            CastFireball();
        }
    }

    void FixedUpdate()
    {
        if (target == null || rb == null) return;

        // Só segue até os limites do mapa — ignora se o alvo saiu
        Vector2 clampedTarget = new Vector2(
            Mathf.Clamp(target.position.x, boundsMin.x, boundsMax.x),
            Mathf.Clamp(target.position.y, boundsMin.y, boundsMax.y));

        float dist = Vector2.Distance(rb.position, clampedTarget);
        if (dist > minDistance)
        {
            Vector2 newPos = Vector2.MoveTowards(
                rb.position, clampedTarget, moveSpeed * Time.fixedDeltaTime);

            newPos.x = Mathf.Clamp(newPos.x, boundsMin.x, boundsMax.x);
            newPos.y = Mathf.Clamp(newPos.y, boundsMin.y, boundsMax.y);

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
            fb.Initialize(direction, damage);
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