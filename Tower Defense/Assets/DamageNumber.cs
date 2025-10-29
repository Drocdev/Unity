using UnityEngine;
using UnityEngine.UI;

public class DamageNumber : MonoBehaviour
{
    public Text text;
    public Vector3 offset = new Vector3(0, 2f, 0);
    public float floatSpeed = 1f;
    public float fadeDuration = 1f;

    private Transform enemyTransform;
    private float timer = 0f;
    private Vector3 appliedOffset;

    public void Setup(int damage, Transform enemy, Color? color = null, Vector3? customOffset = null)
    {
        enemyTransform = enemy;

        if (text != null)
        {
            text.text = damage.ToString();
            text.color = color ?? Color.white;
        }

        appliedOffset = customOffset ?? offset;

        if (enemyTransform != null)
            transform.position = enemyTransform.position + appliedOffset;
    }

    public void UpdateDamage(int newDamage)
    {
        if (text != null)
            text.text = newDamage.ToString();
    }

    public void ResetFadeTimer()
    {
        timer = 0f; // prevents fade out while accumulating
    }

    void Update()
    {
        if (enemyTransform != null)
        {
            transform.position = enemyTransform.position + appliedOffset + Vector3.up * (floatSpeed * timer);
            if (Camera.main != null)
                transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position);
        }

        timer += Time.deltaTime;
        if (text != null)
        {
            Color c = text.color;
            c.a = Mathf.Lerp(1f, 0f, timer / fadeDuration);
            text.color = c;
        }

        if (timer >= fadeDuration)
            Destroy(gameObject);
    }
}
