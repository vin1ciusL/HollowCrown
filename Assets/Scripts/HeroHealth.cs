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

    private float attackTimer = 0f;
    private HeroAnimator heroAnimator;

    void Start()
    {
        currentHealth = maxHealth;
        heroAnimator = GetComponent<HeroAnimator>();
    }

    void Update()
    {
        attackTimer += Time.deltaTime;
        if (attackTimer >= attackCooldown)
            TryAttack();
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

        MageHealth mage = FindAnyObjectByType<MageHealth>();
        if (mage != null && mage.gameObject.activeSelf &&
            Vector2.Distance(transform.position, mage.transform.position) <= attackRange)
        {
            mage.TakeDamage(attackDamage);
            heroAnimator?.TriggerAttack();
        }
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        if (currentHealth <= 0)
            gameObject.SetActive(false);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}