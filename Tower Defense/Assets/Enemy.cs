using UnityEngine;           // Core Unity functionality
using System.Collections;     // For IEnumerator and coroutines

public class Enemy : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 5f;          // Enemy movement speed
    private float startSpeed;         // Original speed, used for slows
    private Transform target;         // Current waypoint target
    private int waypointIndex = 0;    // Index of current waypoint

    [Header("Health")]
    public int health = 3;            // Enemy health

    [Header("Rewards")]
    public int goldReward = 5;        // Gold given to player on death

    [Header("Status Effects")]
    private bool isPoisoned = false;  // Track poison status
    private bool isSlowed = false;    // Track slow status

    [Header("Visuals")]
    private Renderer[] renderers;     // All renderers for changing colors
    private Color[] originalColors;   // Original colors to restore after effects
    public ParticleSystem poisonEffect; // Particle system to show poison effect

    [Header("Damage Numbers")]
    public GameObject damageNumberPrefab; // Prefab to show damage numbers

    [Header("Damage Number Offsets")]
    public Vector3 normalDamageOffset = new Vector3(-0.2f, 3f, 0); // Offset for normal damage text
    public Vector3 poisonDamageOffset = new Vector3(0.2f, 3f, 0);  // Offset for poison damage text

    private DamageNumber activeWhiteNumber;  // Reference to currently active normal damage number
    private int accumulatedNormalDamage = 0; // Track accumulated normal damage for single floating number

    void Start()
    {
        startSpeed = speed;                  // Store original speed
        target = Waypoints.points[0];        // Set first waypoint as target
        renderers = GetComponentsInChildren<Renderer>(); // Get all renderers in children
        originalColors = new Color[renderers.Length];    // Array to store original colors

        // Store original colors for restoring after effects
        for (int i = 0; i < renderers.Length; i++)
        {
            originalColors[i] = renderers[i].material.HasProperty("_BaseColor")
                ? renderers[i].material.GetColor("_BaseColor")
                : renderers[i].material.color;
        }
    }

    void Update()
    {
        if (target == null) return; // Stop moving if no target

        Vector3 dir = target.position - transform.position; // Direction to waypoint
        transform.Translate(dir.normalized * speed * Time.deltaTime, Space.World); // Move enemy

        // Rotate enemy to face movement direction smoothly
        if (dir != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, 10f * Time.deltaTime);
        }

        // Check if close enough to waypoint
        if (Vector3.Distance(transform.position, target.position) < 0.2f)
        {
            GetNextWaypoint(); // Move to next waypoint
        }
    }

    void GetNextWaypoint()
    {
        waypointIndex++; // Increment waypoint index
        if (waypointIndex >= Waypoints.points.Length) // If reached end
        {
            // DAMAGE THE BASE via GameManager
            GameManager gm = FindObjectOfType<GameManager>();
            if (gm != null)
            {
                gm.BaseTakeDamage(1); // Reduce base HP by 1
            }

            Destroy(gameObject); // Destroy enemy
            return;
        }
        target = Waypoints.points[waypointIndex]; // Update target to next waypoint
    }

    public void TakeDamage(int dmg, Color? dmgColor = null)
    {
        health -= dmg; // Reduce health
        bool isPoisonDamage = dmgColor == new Color(0.5f, 0f, 0.5f); // Check if damage is poison type

        if (damageNumberPrefab != null && dmg > 0)
        {
            if (!isPoisonDamage) // Normal damage
            {
                if (activeWhiteNumber == null) // If no active number
                {
                    GameObject dmgText = Instantiate(damageNumberPrefab); // Create damage number
                    activeWhiteNumber = dmgText.GetComponent<DamageNumber>();
                    accumulatedNormalDamage = dmg; // Start accumulation
                    activeWhiteNumber.Setup(accumulatedNormalDamage, transform, Color.white, normalDamageOffset);
                }
                else // Add to existing floating number
                {
                    accumulatedNormalDamage += dmg;
                    activeWhiteNumber.UpdateDamage(accumulatedNormalDamage);
                    activeWhiteNumber.ResetFadeTimer();
                }
            }
            else // Poison damage
            {
                GameObject dmgText = Instantiate(damageNumberPrefab);
                var dn = dmgText.GetComponent<DamageNumber>();
                dn.Setup(dmg, transform, new Color(0.5f, 0f, 0.5f), poisonDamageOffset);
            }
        }

        if (health <= 0) Die(); // Kill enemy if health <= 0
    }

    void Die()
    {
        GameManager gm = FindObjectOfType<GameManager>();
        if (gm != null)
        {
            gm.AddGold(goldReward); // Give player gold
        }
        Destroy(gameObject); // Remove enemy from scene
    }

    public void ApplySlow(float slowFactor, float duration)
    {
        if (isSlowed) return; // Ignore if already slowed
        StartCoroutine(SlowCoroutine(slowFactor, duration)); // Start slow coroutine
    }

    IEnumerator SlowCoroutine(float slowFactor, float duration)
    {
        isSlowed = true;             // Mark as slowed
        speed = startSpeed * slowFactor; // Apply slow
        yield return new WaitForSeconds(duration); // Wait for duration
        speed = startSpeed;           // Restore original speed
        isSlowed = false;             // Remove slow status
    }

    public void ApplyPoison(float dps, float duration)
    {
        if (!isPoisoned)
        {
            StartCoroutine(PoisonCoroutine(dps, duration)); // Start poison damage over time
        }
    }

    IEnumerator PoisonCoroutine(float dps, float duration)
    {
        isPoisoned = true;

        // Change color to indicate poison
        foreach (Renderer r in renderers)
        {
            if (r.material.HasProperty("_BaseColor"))
                r.material.SetColor("_BaseColor", new Color(0.5f, 0f, 0.5f));
            else
                r.material.color = new Color(0.5f, 0f, 0.5f);
        }

        if (poisonEffect != null) poisonEffect.Play(); // Start particle effect

        float elapsed = 0f;
        float tickInterval = 1f; // Damage interval in seconds

        while (elapsed < duration)
        {
            yield return new WaitForSeconds(tickInterval); // Wait between ticks
            int tickDamage = Mathf.RoundToInt(dps * tickInterval); // Calculate damage
            TakeDamage(tickDamage, new Color(0.5f, 0f, 0.5f)); // Apply poison damage
            elapsed += tickInterval;
        }

        // Restore original colors after poison ends
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i].material.HasProperty("_BaseColor"))
                renderers[i].material.SetColor("_BaseColor", originalColors[i]);
            else
                renderers[i].material.color = originalColors[i];
        }

        if (poisonEffect != null) poisonEffect.Stop(); // Stop particle effect
        isPoisoned = false;

        if (health <= 0) Die(); // Kill enemy if health <= 0
    }
}
