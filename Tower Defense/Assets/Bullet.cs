using UnityEngine;           // Provides Unity core classes
using System.Collections.Generic; // Enables use of HashSet<T>

public class Bullet : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 20f;   // Speed of the bullet
    private Transform target;   // Current target the bullet is moving toward

    [Header("Damage")]
    public int damage = 1;      // Damage applied to the enemy

    [Header("Splash Damage")]
    public float explosionRadius = 0f;   // Radius of AoE damage; 0 = no explosion
    public GameObject impactEffect;      // Optional visual effect on hit

    [Header("Effects")]
    public bool applyPoison = false;     // Whether bullet applies poison
    public float poisonDamagePerSecond = 2f; // Damage per second for poison
    public float poisonDuration = 3f;         // Duration of poison effect

    public bool applySlow = false;       // Whether bullet applies slow
    [Range(0f, 1f)] public float slowAmount = 0.5f; // Fraction to slow enemy speed
    public float slowDuration = 2f;      // Duration of slow effect

    [Header("Piercing Option")]
    public bool canPierce = false;       // Whether bullet can pierce multiple enemies
    public int maxPierceCount = 3;       // Maximum number of enemies bullet can pierce
    public float homingRange = 15f;      // Range for homing to next enemy
    private int piercedCount = 0;        // Counter for pierced enemies
    private HashSet<Enemy> hitEnemies = new HashSet<Enemy>(); // Track already hit enemies

    public void Seek(Transform _target)
    // Assigns the target for the bullet
    {
        target = _target;
    }

    void Update()
    // Called every frame
    {
        if (target == null) // If no target
        {
            if (canPierce) FindNextTarget(); // Try to find a new target if piercing
            else { Destroy(gameObject); return; } // Otherwise, destroy bullet
        }

        // Move bullet toward target or forward if no target
        Vector3 moveDir = (target != null) ? (target.position - transform.position).normalized : transform.forward;
        transform.position += moveDir * speed * Time.deltaTime;

        // Rotate bullet to face target smoothly
        if (target != null)
        {
            Quaternion lookRotation = Quaternion.LookRotation(target.position - transform.position);
            transform.rotation = Quaternion.Lerp(transform.rotation, lookRotation, Time.deltaTime * 20f);
        }

        if (canPierce) // Handle piercing logic
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, 0.5f); // Small overlap to detect enemies
            foreach (Collider col in hits)
            {
                Enemy e = col.GetComponent<Enemy>();
                if (e != null && !hitEnemies.Contains(e))
                {
                    ApplyAllEffects(e);     // Apply damage and status effects
                    hitEnemies.Add(e);      // Mark as hit
                    piercedCount++;         // Increment pierced counter

                    if (piercedCount >= maxPierceCount) // Check if max pierce reached
                    {
                        Destroy(gameObject);
                        return;
                    }
                    FindNextTarget(); // Homing to next target
                }
            }
        }
        else // Normal bullet
        {
            if (target != null && Vector3.Distance(transform.position, target.position) <= speed * Time.deltaTime)
            {
                HitTarget(target.GetComponent<Enemy>()); // Hit the target if close enough
            }
        }
    }

    void FindNextTarget()
    // Finds the closest enemy in homing range that hasn't been hit
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, homingRange); // Detect enemies in range
        Enemy closest = null;
        float minDistance = Mathf.Infinity;

        foreach (Collider col in hits)
        {
            Enemy e = col.GetComponent<Enemy>();
            if (e != null && !hitEnemies.Contains(e)) // Skip already hit enemies
            {
                float dist = Vector3.Distance(transform.position, e.transform.position);
                if (dist < minDistance) // Find closest
                {
                    minDistance = dist;
                    closest = e;
                }
            }
        }

        target = (closest != null) ? closest.transform : null; // Assign next target
    }

    void HitTarget(Enemy e)
    // Handles hitting a single target
    {
        if (e == null) return;
        if (impactEffect != null) Instantiate(impactEffect, transform.position, transform.rotation); // Spawn impact effect

        if (explosionRadius > 0f) Explode(); // AoE damage
        else ApplyAllEffects(e);             // Single-target damage

        Destroy(gameObject);                 // Destroy bullet after hit
    }

    void Explode()
    // Applies AoE damage to all enemies in explosion radius
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (Collider col in colliders)
        {
            Enemy e = col.GetComponent<Enemy>();
            if (e != null) ApplyAllEffects(e);
        }
    }

    void ApplyAllEffects(Enemy e)
    // Applies damage and any status effects to an enemy
    {
        e.TakeDamage(damage); // Apply direct damage

        if (applyPoison) e.ApplyPoison(poisonDamagePerSecond, poisonDuration); // Apply poison if enabled
        if (applySlow) e.ApplySlow(slowAmount, slowDuration);                  // Apply slow if enabled
    }

    void OnDrawGizmosSelected()
    // Draws gizmos in the editor to visualize explosion and homing ranges
    {
        if (explosionRadius > 0f)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, explosionRadius); // Draw AoE radius
        }

        if (canPierce)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, homingRange); // Draw homing range
        }
    }
}
