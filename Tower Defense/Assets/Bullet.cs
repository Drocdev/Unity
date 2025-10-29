using UnityEngine;
using System.Collections.Generic;

public class Bullet : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 20f;
    private Transform target;

    [Header("Damage")]
    public int damage = 1;

    [Header("Splash Damage")]
    public float explosionRadius = 0f;
    public GameObject impactEffect;

    [Header("Effects")]
    public bool applyPoison = false;
    public float poisonDamagePerSecond = 2f;
    public float poisonDuration = 3f;

    public bool applySlow = false;
    [Range(0f, 1f)] public float slowAmount = 0.5f;
    public float slowDuration = 2f;

    [Header("Piercing Option")]
    public bool canPierce = false;
    public int maxPierceCount = 3;
    public float homingRange = 15f;
    private int piercedCount = 0;
    private HashSet<Enemy> hitEnemies = new HashSet<Enemy>();

    public void Seek(Transform _target)
    {
        target = _target;
    }

    void Update()
    {
        if (target == null)
        {
            if (canPierce) FindNextTarget();
            else { Destroy(gameObject); return; }
        }

        Vector3 moveDir = (target != null) ? (target.position - transform.position).normalized : transform.forward;
        transform.position += moveDir * speed * Time.deltaTime;

        if (target != null)
        {
            Quaternion lookRotation = Quaternion.LookRotation(target.position - transform.position);
            transform.rotation = Quaternion.Lerp(transform.rotation, lookRotation, Time.deltaTime * 20f);
        }

        if (canPierce)
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, 0.5f);
            foreach (Collider col in hits)
            {
                Enemy e = col.GetComponent<Enemy>();
                if (e != null && !hitEnemies.Contains(e))
                {
                    ApplyAllEffects(e);
                    hitEnemies.Add(e);
                    piercedCount++;

                    if (piercedCount >= maxPierceCount)
                    {
                        Destroy(gameObject);
                        return;
                    }
                    FindNextTarget();
                }
            }
        }
        else
        {
            if (target != null && Vector3.Distance(transform.position, target.position) <= speed * Time.deltaTime)
            {
                HitTarget(target.GetComponent<Enemy>());
            }
        }
    }

    void FindNextTarget()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, homingRange);
        Enemy closest = null;
        float minDistance = Mathf.Infinity;

        foreach (Collider col in hits)
        {
            Enemy e = col.GetComponent<Enemy>();
            if (e != null && !hitEnemies.Contains(e))
            {
                float dist = Vector3.Distance(transform.position, e.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closest = e;
                }
            }
        }

        target = (closest != null) ? closest.transform : null;
    }

    void HitTarget(Enemy e)
    {
        if (e == null) return;
        if (impactEffect != null) Instantiate(impactEffect, transform.position, transform.rotation);

        if (explosionRadius > 0f) Explode();
        else ApplyAllEffects(e);

        Destroy(gameObject);
    }

    void Explode()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (Collider col in colliders)
        {
            Enemy e = col.GetComponent<Enemy>();
            if (e != null) ApplyAllEffects(e);
        }
    }

    void ApplyAllEffects(Enemy e)
    {
        // Only call TakeDamage; Enemy handles spawning numbers
        e.TakeDamage(damage);

        if (applyPoison) e.ApplyPoison(poisonDamagePerSecond, poisonDuration);
        if (applySlow) e.ApplySlow(slowAmount, slowDuration);
    }

    void OnDrawGizmosSelected()
    {
        if (explosionRadius > 0f)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }

        if (canPierce)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, homingRange);
        }
    }
}
