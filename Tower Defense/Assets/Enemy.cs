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

    public ParticleSystem poisonEffect;

    [Header("Damage Numbers")]
    public GameObject damageNumberPrefab;

    [Header("Damage Number Offsets")]
    public Vector3 normalDamageOffset = new Vector3(-0.2f, 3f, 0);
    public Vector3 poisonDamageOffset = new Vector3(0.2f, 3f, 0);

    // Reference to current stacked normal damage number
    private DamageNumber activeWhiteNumber;
    private int accumulatedNormalDamage = 0;

    void Start()
    {
        startSpeed = speed;
        target = Waypoints.points[0];

        renderers = GetComponentsInChildren<Renderer>();
        originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            originalColors[i] = renderers[i].material.HasProperty("_BaseColor") ?
                renderers[i].material.GetColor("_BaseColor") : renderers[i].material.color;
        }
    }

    void Update()
    {
        if (target == null) return;

        Vector3 dir = target.position - transform.position;
        transform.Translate(dir.normalized * speed * Time.deltaTime, Space.World);

        if (dir != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, 10f * Time.deltaTime);
        }

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
            Destroy(gameObject);
            return;
        }
        target = Waypoints.points[waypointIndex];
    }

    // ------------ DAMAGE ------------

    public void TakeDamage(int dmg, Color? dmgColor = null)
    {
        health -= dmg;
        bool isPoisonDamage = dmgColor == new Color(0.5f, 0f, 0.5f);

        if (damageNumberPrefab != null && dmg > 0)
        {
            if (!isPoisonDamage)
            {
                // --- Handle stacked white number ---
                if (activeWhiteNumber == null)
                {
                    GameObject dmgText = Instantiate(damageNumberPrefab);
                    activeWhiteNumber = dmgText.GetComponent<DamageNumber>();
                    accumulatedNormalDamage = dmg;
                    activeWhiteNumber.Setup(accumulatedNormalDamage, transform, Color.white, normalDamageOffset);
                }
                else
                {
                    accumulatedNormalDamage += dmg;
                    activeWhiteNumber.UpdateDamage(accumulatedNormalDamage);
                    activeWhiteNumber.ResetFadeTimer(); // prevent it from fading out too early
                }
            }
            else
            {
                // --- Spawn separate purple poison number ---
                GameObject dmgText = Instantiate(damageNumberPrefab);
                var dn = dmgText.GetComponent<DamageNumber>();
                dn.Setup(dmg, transform, new Color(0.5f, 0f, 0.5f), poisonDamageOffset);
            }
        }

        if (health <= 0) Die();
    }

    void Die()
    {
        Destroy(gameObject);
    }

    // ------------ STATUS EFFECTS ------------

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

        foreach (Renderer r in renderers)
        {
            if (r.material.HasProperty("_BaseColor"))
                r.material.SetColor("_BaseColor", new Color(0.5f, 0f, 0.5f));
            else
                r.material.color = new Color(0.5f, 0f, 0.5f);
        }

        if (poisonEffect != null) poisonEffect.Play();

        float elapsed = 0f;
        float tickInterval = 1f;
        while (elapsed < duration)
        {
            yield return new WaitForSeconds(tickInterval);

            int tickDamage = Mathf.RoundToInt(dps * tickInterval);
            TakeDamage(tickDamage, new Color(0.5f, 0f, 0.5f));

            elapsed += tickInterval;
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i].material.HasProperty("_BaseColor"))
                renderers[i].material.SetColor("_BaseColor", originalColors[i]);
            else
                renderers[i].material.color = originalColors[i];
        }

        if (poisonEffect != null) poisonEffect.Stop();
        isPoisoned = false;

        if (health <= 0) Die();
    }
}
