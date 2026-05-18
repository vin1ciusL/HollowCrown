using System.Collections.Generic;
using UnityEngine;

public class CavaleiroNivel6Health : MonoBehaviour
{
    public static readonly List<CavaleiroNivel6Health> All = new();

    public event System.Action OnMorte;

    [Header("Vida")]
    public float maxHealth = 30f;
    public float currentHealth = 30f;

    [Header("Combate")]
    public float attackDamage = 12f;
    public float attackRange = 1.8f;
    public float attackCooldown = 0.8f;

    [Header("Recompensa")]
    public int almasRecompensa = 20;

    private float attackTimer = 0f;
    private bool pendingDamage = false;
    private CavaleiroNivel6Animator cavaleiroAnimator;
    private HeroHealth heroHealth;
    private LichHealth lichAlvo;

    void OnEnable()  => All.Add(this);
    void OnDisable() => All.Remove(this);

    void Start()
    {
        currentHealth = maxHealth;
        attackTimer = Random.Range(0f, attackCooldown);
        cavaleiroAnimator = GetComponent<CavaleiroNivel6Animator>();

        GameObject heroObj = GameObject.FindWithTag("Player");
        if (heroObj != null)
            heroHealth = heroObj.GetComponent<HeroHealth>();

        if (cavaleiroAnimator != null)
            cavaleiroAnimator.OnImpactFrame += ApplyPendingDamage;
    }

    void OnDestroy()
    {
        if (cavaleiroAnimator != null)
            cavaleiroAnimator.OnImpactFrame -= ApplyPendingDamage;
    }

    void Update()
    {
        if (heroHealth == null)
        {
            GameObject heroObj = GameObject.FindWithTag("Player");
            if (heroObj != null)
                heroHealth = heroObj.GetComponent<HeroHealth>();
        }

        lichAlvo = LichHealth.Instance;

        attackTimer += Time.deltaTime;
        if (attackTimer >= attackCooldown)
            TryAttack();
    }

    MonoBehaviour AlvoMaisProximo()
    {
        float distHero = heroHealth != null
            ? Vector2.Distance(transform.position, heroHealth.transform.position)
            : float.MaxValue;
        float distLich = lichAlvo != null
            ? Vector2.Distance(transform.position, lichAlvo.transform.position)
            : float.MaxValue;

        if (distHero <= attackRange && distHero <= distLich) return heroHealth;
        if (distLich <= attackRange) return lichAlvo;
        return null;
    }

    void TryAttack()
    {
        MonoBehaviour alvo = AlvoMaisProximo();
        if (alvo == null) return;

        attackTimer = 0f;

        if (cavaleiroAnimator != null)
        {
            pendingDamage = true;
            cavaleiroAnimator.TriggerAttack();
        }
        else
        {
            AplicarDano(alvo);
        }
    }

    void AplicarDano(MonoBehaviour alvo)
    {
        if (alvo is HeroHealth h) h.TakeDamage(attackDamage);
        else if (alvo is LichHealth l) l.TakeDamage(attackDamage);
    }

    void ApplyPendingDamage()
    {
        if (!pendingDamage) return;
        pendingDamage = false;

        MonoBehaviour alvo = AlvoMaisProximo();
        if (alvo != null) AplicarDano(alvo);
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            if (SoulManager.Instance != null)
                SoulManager.Instance.AdicionarAlmas(almasRecompensa);

            OnMorte?.Invoke();
            Destroy(gameObject);
        }
    }
}
