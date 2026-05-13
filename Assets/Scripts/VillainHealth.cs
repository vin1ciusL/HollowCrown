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

        attackTimer += Time.deltaTime;
        if (attackTimer >= attackCooldown)
            TryAttack();
    }

    void TryAttack()
    {
        if (heroHealth == null) return;

        float dist = Vector2.Distance(transform.position, heroHealth.transform.position);
        if (dist > attackRange) return;

        attackTimer = 0f;

        if (villainAnimator != null)
        {
            pendingDamage = true;
            villainAnimator.TriggerAttack();
        }
        else
        {
            heroHealth.TakeDamage(attackDamage);
        }
    }

    void ApplyPendingDamage()
    {
        if (!pendingDamage || heroHealth == null) return;
        pendingDamage = false;

        float dist = Vector2.Distance(transform.position, heroHealth.transform.position);
        if (dist <= attackRange * 1.2f)
            heroHealth.TakeDamage(attackDamage);
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            OnMorte?.Invoke(); // <- NOVO
            Destroy(gameObject);
        }
    }
}