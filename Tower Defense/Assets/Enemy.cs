using UnityEngine;
using System.Collections;

public class Enemy : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 5f;
    private float startSpeed;

    private Transform target;
    private int waypointIndex = 0;

    [Header("Health")]
    public int health = 3;

    [Header("Status Effects")]
    private bool isPoisoned = false;
    private bool isSlowed = false;

    [Header("Visuals")]
    private Renderer rend;
    private Color originalColor;

    // Optional: particle system for poison
    public ParticleSystem poisonEffect;

    void Start()
    {
        startSpeed = speed;
        target = Waypoints.points[0];

        rend = GetComponent<Renderer>();
        if (rend != null)
        {
            originalColor = rend.material.color;
        }
    }

    void Update()
    {
        if (target == null) return;

        // Move toward current waypoint
        Vector3 dir = target.position - transform.position;
        transform.Translate(dir.normalized * speed * Time.deltaTime, Space.World);

        // Check if reached waypoint
        if (Vector3.Distance(transform.position, target.position) < 0.2f)
        {
            GetNextWaypoint();
        }
    }

    void GetNextWaypoint()
    {
        waypointIndex++;
        if (waypointIndex >= Waypoints.points.Length)
        {
            Destroy(gameObject); // Reached end
            return;
        }
        target = Waypoints.points[waypointIndex];
    }

    public void TakeDamage(int dmg)
    {
        health -= dmg;
        if (health <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Destroy(gameObject);
    }

    // ---------------- Status Effects ----------------

    public void ApplySlow(float slowFactor, float duration)
    {
        if (isSlowed) return;
        StartCoroutine(SlowCoroutine(slowFactor, duration));
    }

    IEnumerator SlowCoroutine(float slowFactor, float duration)
    {
        isSlowed = true;
        speed = startSpeed * slowFactor;

        yield return new WaitForSeconds(duration);

        speed = startSpeed;
        isSlowed = false;
    }

    public void ApplyPoison(float dps, float duration)
    {
        if (!isPoisoned)
        {
            StartCoroutine(PoisonCoroutine(dps, duration));
        }
    }

    IEnumerator PoisonCoroutine(float dps, float duration)
    {
        isPoisoned = true;

        // Change color to purple
        if (rend != null)
        {
            rend.material.color = new Color(0.5f, 0f, 0.5f); // RGB purple
        }

        // Play poison particle if assigned
        if (poisonEffect != null) poisonEffect.Play();

        float elapsed = 0f;
        while (elapsed < duration)
        {
            TakeDamage(Mathf.RoundToInt(dps * Time.deltaTime));
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Reset color
        if (rend != null) rend.material.color = originalColor;

        // Stop particle
        if (poisonEffect != null) poisonEffect.Stop();

        isPoisoned = false;
    }
}
