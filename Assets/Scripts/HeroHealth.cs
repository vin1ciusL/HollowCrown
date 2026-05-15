using UnityEngine;
using System.Collections.Generic;

public class HeroHealth : MonoBehaviour
{
    [Header("Vida")]
    public float maxHealth = 100f;
    public float currentHealth = 100f;

    [Header("Combate")]
    public float attackDamage = 10f;
    public float attackRange = 1.5f;
    public float attackCooldown = 1f;

    [Header("Invulnerabilidade")]
    [Tooltip("Tempo (s) imune a dano logo após levar um hit")]
    public float invulnerabilityDuration = 0.5f;

    private float attackTimer = 0f;
    private float invulnTimer = 0f;
    private HeroAnimator heroAnimator;
    private SpriteRenderer sr;

    void Start()
    {
        currentHealth = maxHealth;
        heroAnimator = GetComponent<HeroAnimator>();
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
        attackTimer = 0f;

        foreach (VillainHealth villain in new List<VillainHealth>(VillainHealth.All))
        {
            if (Vector2.Distance(transform.position, villain.transform.position) <= attackRange)
            {
                villain.TakeDamage(attackDamage);
                heroAnimator?.TriggerAttack();
                return;
            }
        }
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
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}