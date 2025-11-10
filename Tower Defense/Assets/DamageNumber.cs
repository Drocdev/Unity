using UnityEngine;       // Gives access to Unity’s core engine classes (MonoBehaviour, Transform, etc.)
using UnityEngine.UI;    // Provides access to UI components like Text.

public class DamageNumber : MonoBehaviour
// A component that displays floating damage numbers above enemies when they are hit.
{
    public Text text;                       // Reference to the UI Text component showing the damage value.
    public Vector3 offset = new Vector3(0, 2f, 0); // Default position offset above the enemy’s position.
    public float floatSpeed = 1f;           // Speed at which the damage number floats upward.
    public float fadeDuration = 1f;         // Time (in seconds) it takes for the number to fully fade out.

    private Transform enemyTransform;       // Reference to the enemy the number follows.
    private float timer = 0f;               // Tracks elapsed time since the damage number appeared.
    private Vector3 appliedOffset;          // The actual offset applied (can be custom).

    public void Setup(int damage, Transform enemy, Color? color = null, Vector3? customOffset = null)
    // Initializes the damage number with values such as damage amount, enemy reference, and color.
    {
        enemyTransform = enemy;             // Store the enemy transform to follow.

        if (text != null)
        {
            text.text = damage.ToString();          // Display the numeric damage value.
            text.color = color ?? Color.white;      // Use custom color if given, otherwise default to white.
        }

        appliedOffset = customOffset ?? offset;     // Use custom offset if given, otherwise default.

        if (enemyTransform != null)
            transform.position = enemyTransform.position + appliedOffset; // Place above the enemy initially.
    }

    public void UpdateDamage(int newDamage)
    // Updates the displayed damage number (useful if stacking hits together).
    {
        if (text != null)
            text.text = newDamage.ToString(); // Change text to new damage amount.
    }

    public void ResetFadeTimer()
    // Resets the fade timer, keeping the number visible longer (used when stacking).
    {
        timer = 0f; // Restart timer to prevent fading out immediately.
    }

    void Update()
    // Called once per frame to handle floating, fading, and destruction.
    {
        if (enemyTransform != null)
        {
            // Make the number follow the enemy and float upward over time.
            transform.position = enemyTransform.position + appliedOffset + Vector3.up * (floatSpeed * timer);

            // Make the damage number always face the camera (billboard effect).
            if (Camera.main != null)
                transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position);
        }

        timer += Time.deltaTime; // Count how much time has passed since creation.

        if (text != null)
        {
            // Gradually fade the text alpha from 1 (fully visible) to 0 (invisible) over fadeDuration seconds.
            Color c = text.color;
            c.a = Mathf.Lerp(1f, 0f, timer / fadeDuration);
            text.color = c;
        }

        // Once the fade duration has fully elapsed, remove the damage number from the scene.
        if (timer >= fadeDuration)
            Destroy(gameObject);
    }
}
