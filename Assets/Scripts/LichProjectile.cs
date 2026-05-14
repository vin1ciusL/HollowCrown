using UnityEngine;

public class LichProjectile : MonoBehaviour
{
    public float speed = 8f;
    public float lifetime = 4f;
    public float damage = 20f;

    private Vector2 direction;

    public void Initialize(Vector2 dir, float dmg)
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
        VillainHealth villain = other.GetComponentInParent<VillainHealth>();
        if (villain != null)
        {
            villain.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}
