using UnityEngine;

public class Fireball : MonoBehaviour
{
    public float speed = 8f;
    public float lifetime = 4f;

    private Vector2 direction;
    private float damage = 15f;
    private bool isAlly = false;

    public void Initialize(Vector2 dir, float dmg = 15f, bool ally = false)
    {
        direction = dir.normalized;
        damage = dmg;
        isAlly = ally;
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        transform.Translate(direction * speed * Time.deltaTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isAlly)
        {
            VillainHealth villain = other.GetComponentInParent<VillainHealth>();
            if (villain != null)
            {
                villain.TakeDamage(damage);
                Destroy(gameObject);
                return;
            }

            MageHealth mage = other.GetComponentInParent<MageHealth>();
            if (mage != null)
            {
                mage.TakeDamage(damage);
                Destroy(gameObject);
            }
        }
        else
        {
            HeroHealth hero = other.GetComponentInParent<HeroHealth>();
            if (hero != null)
            {
                hero.TakeDamage(damage);
                Destroy(gameObject);
                return;
            }

            GolemHealth golem = other.GetComponentInParent<GolemHealth>();
            if (golem != null)
            {
                golem.TakeDamage(damage);
                Destroy(gameObject);
                return;
            }

            LichHealth lich = other.GetComponentInParent<LichHealth>();
            if (lich != null)
            {
                lich.TakeDamage(damage);
                Destroy(gameObject);
            }
        }
    }
}
