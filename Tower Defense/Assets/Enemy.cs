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
    private Renderer[] renderers;
    private Color[] originalColors;

    // Optional: particle system for poison
    public ParticleSystem poisonEffect;

    void Start()
    {
        startSpeed = speed;
        target = Waypoints.points[0];

        // Get all renderers in the prefab (including children)
        renderers = GetComponentsInChildren<Renderer>();

        // Save original colors for all renderers
        originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i].material.HasProperty("_BaseColor"))
                originalColors[i] = renderers[i].material.GetColor("_BaseColor");
            else
                originalColors[i] = renderers[i].material.color;
        }
    }

    void Update()
    {
        if (target == null) return;

        // Direction to next waypoint
        Vector3 dir = target.position - transform.position;

        // Move toward current waypoint
        transform.Translate(dir.normalized * speed * Time.deltaTime, Space.World);

        // Rotate smoothly toward direction
        if (dir != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, 10f * Time.deltaTime);
        }

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

        // Change all renderers to purple
        foreach (Renderer r in renderers)
        {
            if (r.material.HasProperty("_BaseColor"))
                r.material.SetColor("_BaseColor", new Color(0.5f, 0f, 0.5f));
            else
                r.material.color = new Color(0.5f, 0f, 0.5f);
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

        // Reset all renderers to original colors
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i].material.HasProperty("_BaseColor"))
                renderers[i].material.SetColor("_BaseColor", originalColors[i]);
            else
                renderers[i].material.color = originalColors[i];
        }

        // Stop particle
        if (poisonEffect != null) poisonEffect.Stop();

        isPoisoned = false;
    }
}
