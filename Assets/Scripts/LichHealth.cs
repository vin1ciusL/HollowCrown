using UnityEngine;

public class LichHealth : MonoBehaviour
{
    public static LichHealth Instance { get; private set; }

    public float maxHealth = 80f;
    public float currentHealth;

    [Header("Invulnerabilidade")]
    public float invulnerabilityDuration = 0.5f;

    private float invulnTimer = 0f;
    private SpriteRenderer sr;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    void Start()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
    }

    void OnEnable()
    {
        Instance = this;
    }

    void OnDisable()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (invulnTimer > 0f)
        {
            invulnTimer -= Time.deltaTime;
            if (sr != null)
            {
                Color c = sr.color;
                c.a = Mathf.PingPong(Time.time * 10f, 1f) * 0.5f + 0.5f;
                sr.color = c;
            }
            if (invulnTimer <= 0f && sr != null)
            {
                Color c = sr.color; c.a = 1f; sr.color = c;
            }
        }
    }

    public void TakeDamage(float damage)
    {
        if (invulnTimer > 0f) return;

        currentHealth -= damage;
        invulnTimer = invulnerabilityDuration;

        if (currentHealth <= 0)
            Destroy(gameObject);
    }
}
