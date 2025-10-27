using UnityEngine;

public class SlowBullet : MonoBehaviour
{
    [Header("Bullet Settings")]
    public float speed = 20f;
    public int damage = 1;
    public float explosionRadius = 0f; // 0 means no splash damage

    [Header("Poison & Slow Settings")]
    public bool applyPoison = false;
    public float poisonDamagePerSecond = 2f;
    public float poisonDuration = 3f;

    public bool applySlow = false;
    public float slowAmount = 0.5f;     // 0.5 = 50% speed
    public float slowDuration = 2f;

    [Header("Visuals")]
    public GameObject impactEffect; // Optional explosion or hit VFX

    private Transform target;

    public void Seek(Transform _target)
    {
        target = _target;
    }

    void Update()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 dir = target.position - transform.position;
        float distanceThisFrame = speed * Time.deltaTime;

        if (dir.magnitude <= distanceThisFrame)
        {
            HitTarget();
            return;
        }

        // Smooth rotation
        Quaternion lookRotation = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Lerp(transform.rotation, lookRotation, Time.deltaTime * 20f);

        // Move toward target
        transform.position += dir.normalized * distanceThisFrame;
    }

    void HitTarget()
    {
        if (impactEffect != null)
        {
            Instantiate(impactEffect, transform.position, transform.rotation);
        }

        if (explosionRadius > 0f)
        {
            Explode();
        }
        else
        {
            Damage(target);
        }

        Destroy(gameObject);
    }

    void Explode()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (Collider collider in colliders)
        {
            Enemy e = collider.GetComponent<Enemy>();
            if (e != null)
            {
                ApplyEffects(e);
            }
        }
    }

    void Damage(Transform enemy)
    {
        Enemy e = enemy.GetComponent<Enemy>();
        if (e != null)
        {
            ApplyEffects(e);
        }
    }

    void ApplyEffects(Enemy e)
    {
        // Normal damage
        e.TakeDamage(damage);

        // Apply poison
        if (applyPoison)
        {
            e.ApplyPoison(poisonDamagePerSecond, poisonDuration);
        }

        // Apply slow
        if (applySlow)
        {
            e.ApplySlow(slowAmount, slowDuration);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
