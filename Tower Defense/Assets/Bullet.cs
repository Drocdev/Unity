using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 20f;
    private Transform target;

    [Header("Damage")]
    public int damage = 1;

    [Header("Splash Damage")]
    public float explosionRadius = 0f; // 0 = no splash damage
    public GameObject impactEffect;    // optional visual effect

    [Header("Effects")]
    public bool applyPoison = false;
    public float poisonDamagePerSecond = 2f;
    public float poisonDuration = 3f;

    public bool applySlow = false;
    [Range(0f, 1f)]
    public float slowAmount = 0.5f; // 50% speed
    public float slowDuration = 2f;

    // --- Targeting ---
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

        // Smooth rotation toward target
        Quaternion lookRotation = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Lerp(transform.rotation, lookRotation, Time.deltaTime * 20f);

        // Move toward target
        transform.position += dir.normalized * distanceThisFrame;
    }

    void HitTarget()
    {
        // Spawn impact effect
        if (impactEffect != null)
        {
            Instantiate(impactEffect, transform.position, transform.rotation);
        }

        // Damage logic
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
        foreach (Collider col in colliders)
        {
            Enemy e = col.GetComponent<Enemy>();
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
        // Base damage
        e.TakeDamage(damage);

        // Poison effect
        if (applyPoison)
        {
            e.ApplyPoison(poisonDamagePerSecond, poisonDuration);
        }

        // Slow effect
        if (applySlow)
        {
            e.ApplySlow(slowAmount, slowDuration);
        }
    }

    // Visualize splash radius in editor
    void OnDrawGizmosSelected()
    {
        if (explosionRadius > 0f)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }
    }
}
