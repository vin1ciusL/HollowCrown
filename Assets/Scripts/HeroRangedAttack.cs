using UnityEngine;

public class HeroRangedAttack : MonoBehaviour
{
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float fireRate = 2f;
    public float range = 5f;

    private float nextFireTime;
    private Transform villain;

    void Start()
    {
        GameObject target = GameObject.FindGameObjectWithTag("Player");

        if (target != null)
        {
            villain = target.transform;
        }
    }

    void Update()
    {
        if (villain == null) return;

        float distance = Vector2.Distance(transform.position, villain.position);

        if (distance <= range && Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + fireRate;
        }
    }

    void Shoot()
    {
        Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
    }
}