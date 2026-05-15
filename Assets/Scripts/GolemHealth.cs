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

    private float attackTimer = 0f;
    private GolemAnimator golemAnimator;

    void Start()
    {
        currentHealth = maxHealth;
        golemAnimator = GetComponent<GolemAnimator>();
    }

    void Update()
    {
        attackTimer += Time.deltaTime;
        if (attackTimer >= attackCooldown)
            TryAttack();
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

        MageHealth mage = FindAnyObjectByType<MageHealth>();
        if (mage != null && mage.gameObject.activeSelf &&
            Vector2.Distance(transform.position, mage.transform.position) <= aoeRadius)
        {
            attackTimer = 0f;
            AoeAttack();
        }
    }

    void AoeAttack()
    {
        golemAnimator?.TriggerAttack();

        foreach (VillainHealth villain in new List<VillainHealth>(VillainHealth.All))
        {
            if (Vector2.Distance(transform.position, villain.transform.position) <= aoeRadius)
                villain.TakeDamage(attackDamage);
        }

        MageHealth mage = FindAnyObjectByType<MageHealth>();
        if (mage != null && mage.gameObject.activeSelf &&
            Vector2.Distance(transform.position, mage.transform.position) <= aoeRadius)
            mage.TakeDamage(attackDamage);
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        if (currentHealth <= 0)
            gameObject.SetActive(false);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, aoeRadius);
    }
}