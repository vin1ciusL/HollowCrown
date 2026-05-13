using UnityEngine;

public class Fireball : MonoBehaviour
{
    public float speed = 8f;
    public float lifetime = 4f;

    private Vector2 direction;
    private float damage = 15f;

    public void Initialize(Vector2 dir, float dmg = 15f)
    {
        direction = dir.normalized;
        damage = dmg;
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        transform.Translate(direction * speed * Time.deltaTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        HeroHealth hero = other.GetComponentInParent<HeroHealth>();
        if (hero != null)
        {
            hero.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}