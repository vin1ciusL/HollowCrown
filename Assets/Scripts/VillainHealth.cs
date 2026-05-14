using System.Collections.Generic;
using UnityEngine;

public class VillainHealth : MonoBehaviour
{
    public static readonly List<VillainHealth> All = new();

    public event System.Action OnMorte; // <- NOVO

    [Header("Vida")]
    public float maxHealth = 10f;
    public float currentHealth = 10f;

    [Header("Combate")]
    public float attackDamage = 5f;
    public float attackRange = 1.5f;
    public float attackCooldown = 1f;

    private float attackTimer = 0f;
    private bool pendingDamage = false;
    private VillainAnimator villainAnimator;
    private HeroHealth heroHealth;
    private LichHealth lichAlvo;

    void OnEnable()  => All.Add(this);
    void OnDisable() => All.Remove(this);

    void Start()
    {
        currentHealth = maxHealth;
        attackTimer = Random.Range(0f, attackCooldown);
        villainAnimator = GetComponent<VillainAnimator>();

        GameObject heroObj = GameObject.FindWithTag("Player");
        if (heroObj != null)
            heroHealth = heroObj.GetComponent<HeroHealth>();

        if (villainAnimator != null)
            villainAnimator.OnImpactFrame += ApplyPendingDamage;
    }

    void OnDestroy()
    {
        if (villainAnimator != null)
            villainAnimator.OnImpactFrame -= ApplyPendingDamage;
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

    // Retorna o alvo mais próximo dentro do attackRange
    MonoBehaviour AlvoMaisProximo()
    {
        float distHero = heroHealth != null ? Vector2.Distance(transform.position, heroHealth.transform.position) : float.MaxValue;
        float distLich = lichAlvo != null ? Vector2.Distance(transform.position, lichAlvo.transform.position) : float.MaxValue;

        if (distHero <= attackRange && distHero <= distLich) return heroHealth;
        if (distLich <= attackRange) return lichAlvo;
        return null;
    }

    void TryAttack()
    {
        MonoBehaviour alvo = AlvoMaisProximo();
        if (alvo == null) return;

        attackTimer = 0f;

        if (villainAnimator != null)
        {
            pendingDamage = true;
            villainAnimator.TriggerAttack();
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

    [Header("Recompensa")]
    [Tooltip("Almas concedidas ao jogador ao matar este inimigo")]
    public int almasRecompensa = 10;

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            // Concede almas ao jogador
            if (SoulManager.Instance != null)
                SoulManager.Instance.AdicionarAlmas(almasRecompensa);

            OnMorte?.Invoke();
            Destroy(gameObject);
        }
    }
}