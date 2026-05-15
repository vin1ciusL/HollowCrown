using UnityEngine;
using System.Collections.Generic;

public class GolemHealth : MonoBehaviour
{
    [Header("Vida")]
    public float maxHealth = 250f;
    public float currentHealth = 250f;

    [Header("Combate")]
    public float attackDamage = 15f;
    public float aoeRadius = 2.5f;
    public float attackCooldown = 1.5f;

    [Header("Knockback")]
    [Tooltip("Força do empurrão aplicado a cada vilão na AOE (MÉDIO)")]
    public float knockbackForce = 7f;
    [Tooltip("Duração em segundos do bloqueio de movimento do vilão")]
    public float knockbackDuration = 0.25f;

    [Header("Invulnerabilidade")]
    public float invulnerabilityDuration = 0.5f;

    private float attackTimer = 0f;
    private float invulnTimer = 0f;
    private GolemAnimator golemAnimator;
    private SpriteRenderer sr;

    void Start()
    {
        currentHealth = maxHealth;
        golemAnimator = GetComponent<GolemAnimator>();
        sr = GetComponentInChildren<SpriteRenderer>();
    }

    void Update()
    {
        attackTimer += Time.deltaTime;
        if (attackTimer >= attackCooldown)
            TryAttack();

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

    void TryAttack()
    {
        foreach (VillainHealth villain in new List<VillainHealth>(VillainHealth.All))
        {
            if (Vector2.Distance(transform.position, villain.transform.position) <= aoeRadius)
            {
                attackTimer = 0f;
                AoeAttack();
                return;
            }
        }
    }

    void AoeAttack()
    {
        golemAnimator?.TriggerAttack();

        foreach (VillainHealth villain in new List<VillainHealth>(VillainHealth.All))
        {
            if (Vector2.Distance(transform.position, villain.transform.position) <= aoeRadius)
            {
                villain.TakeDamage(attackDamage);
                AplicarKnockback(villain);
            }
        }
    }

    void AplicarKnockback(VillainHealth villain)
    {
        if (villain == null || knockbackForce <= 0f) return;
        VillainController vc = villain.GetComponent<VillainController>();
        if (vc == null) return;
        Vector2 dir = (Vector2)(villain.transform.position - transform.position);
        vc.AplicarKnockback(dir, knockbackForce, knockbackDuration);
    }

    public void TakeDamage(float damage)
    {
        if (invulnTimer > 0f) return;

        currentHealth -= damage;
        invulnTimer = invulnerabilityDuration;

        if (currentHealth <= 0)
        {
            if (PlayerLives.Instance != null) PlayerLives.Instance.PerderVida();
            gameObject.SetActive(false);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, aoeRadius);
    }
}