using UnityEngine;       // Provides Unity core classes (MonoBehaviour, Transform, etc.)
using UnityEngine.UI;    // Provides access to UI elements (Text)
using TMPro;             // Provides access to TextMeshPro UI components
using System.Collections.Generic; // Enables use of List<T> and HashSet<T>

public class Tower : MonoBehaviour
// Component representing a tower that can shoot at enemies and show stats/range.
{
    [Header("Combat")]
    public int damage = 10;      // Damage each bullet deals
    public float range = 10f;    // Maximum distance tower can attack
    public float fireRate = 1f;  // Shots per second

    [Header("Fire Points (for multi-bullet / shotgun style)")]
    public List<Transform> firePoints;      // Positions from which bullets are fired
    public List<GameObject> bulletPrefabs;  // Prefabs of bullets to instantiate
    public bool singleTargetPerFirePoint = false; // Whether each fire point targets a unique enemy

    [Header("Targeting")]
    public Transform castleTransform; // The target the tower prioritizes enemies towards (auto-found if null)

    private float fireCountdown = 0f; // Timer to control fire rate

    [Header("Range Visualization")]
    public LineRenderer rangeIndicator; // LineRenderer to show tower range circle
    public Color rangeColor = Color.green; // Color of the range circle
    public float rangeWidth = 0.05f; // Width of the range line

    [Header("Floating Stats UI Settings")]
    public GameObject statsUIPrefab;        // Prefab for showing floating stats above tower
    public Vector3 statsOffset = new Vector3(0, 2f, 0); // Offset from tower position
    public Color statsColor = Color.white;  // Color of the stats text
    public float statsFontSize = 24f;       // Font size for the stats
    public float statsWorldScale = 1f;      // Scale of UI in world space
    public Font statsFont;                   // Legacy font (optional)
    public TMP_FontAsset statsTMPFont;       // TextMeshPro font (optional)
    [Range(0f, 1f)]
    public float rotationLerp = 1f;          // How quickly the UI rotates to face camera

    private GameObject statsUIInstance;      // Instance of floating stats UI
    private Camera mainCamera;               // Reference to main camera

    private void Awake()
    // Called once when the tower is initialized
    {
        mainCamera = Camera.main; // Get main camera reference

        // Auto-find the castle if not assigned
        if (castleTransform == null)
        {
            GameObject castle = GameObject.FindGameObjectWithTag("Base"); // Find object with "Base" tag
            if (castle != null)
                castleTransform = castle.transform; // Set castleTransform
        }

        // Setup range indicator if not assigned
        if (rangeIndicator == null)
        {
            GameObject indicator = new GameObject("RangeIndicator"); // Create new empty GameObject
            indicator.transform.SetParent(transform);       // Attach to tower
            indicator.transform.localPosition = Vector3.zero; // Reset local position

            rangeIndicator = indicator.AddComponent<LineRenderer>(); // Add LineRenderer component
            rangeIndicator.useWorldSpace = true; // Use world space positions
        }

        rangeIndicator.startWidth = rangeWidth;            // Set line start width
        rangeIndicator.endWidth = rangeWidth;              // Set line end width
        rangeIndicator.material = new Material(Shader.Find("Unlit/Color")); // Simple unlit color material
        rangeIndicator.material.color = rangeColor;        // Assign color
        rangeIndicator.enabled = false;                    // Initially hidden
    }

    private void Update()
    // Called every frame
    {
        UpdateStatsUIPosition();                          // Update floating stats position

        if (fireCountdown <= 0f)                          // If ready to fire
        {
            Shoot();                                      // Shoot at enemies
            fireCountdown = 1f / fireRate;               // Reset countdown based on fire rate
        }

        fireCountdown -= Time.deltaTime;                  // Reduce countdown
    }

    private void LateUpdate()
    {
        UpdateStatsUIRotation();                          // Rotate stats UI to face camera
    }

    private void Shoot()
    // Handles firing bullets from all fire points
    {
        if (firePoints.Count == 0 || bulletPrefabs.Count == 0 || castleTransform == null) return; // Safety check

        HashSet<Transform> assignedTargets = new HashSet<Transform>(); // Track targets already assigned

        for (int i = 0; i < firePoints.Count; i++)
        {
            Transform fp = firePoints[i]; // Current fire point
            GameObject bulletPrefabToUse = bulletPrefabs[i % bulletPrefabs.Count]; // Select bullet prefab (loop if fewer prefabs)

            Transform fireTarget = null;

            if (singleTargetPerFirePoint) // Assign unique target for each fire point
            {
                fireTarget = FindClosestToCastleEnemy(assignedTargets); // Find closest enemy not already targeted
                if (fireTarget != null)
                    assignedTargets.Add(fireTarget); // Mark target as assigned
                else
                    continue; // Skip if no valid target
            }
            else
            {
                fireTarget = FindClosestToCastleEnemy(); // All fire points can target same enemy
            }

            if (fireTarget == null) continue; // Skip if no target

            GameObject bulletGO = Instantiate(bulletPrefabToUse, fp.position, fp.rotation); // Create bullet
            Bullet bullet = bulletGO.GetComponent<Bullet>();
            if (bullet != null)
            {
                bullet.damage = damage; // Assign damage to bullet
                bullet.Seek(fireTarget); // Set target for bullet
            }
        }
    }

    private Transform FindClosestToCastleEnemy(HashSet<Transform> exclude = null)
    // Finds the enemy closest to the castle within tower range, optionally excluding some
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy"); // Find all enemies
        float closestDistance = Mathf.Infinity; // Initialize closest distance
        GameObject nearestEnemy = null;

        foreach (GameObject enemy in enemies)
        {
            Transform t = enemy.transform;
            if (exclude != null && exclude.Contains(t)) continue; // Skip excluded enemies

            float distToCastle = Vector3.Distance(t.position, castleTransform.position); // Distance to castle
            float distToTower = Vector3.Distance(transform.position, t.position); // Distance to tower

            if (distToTower <= range && distToCastle < closestDistance) // Check within range and closer to castle
            {
                closestDistance = distToCastle; // Update closest distance
                nearestEnemy = enemy; // Set nearest enemy
            }
        }

        return nearestEnemy != null ? nearestEnemy.transform : null; // Return transform or null
    }

    public void ShowRange(bool show)
    // Enables or disables the range indicator
    {
        if (rangeIndicator == null) return;

        rangeIndicator.enabled = show; // Enable/disable

        if (show)
            DrawRange();   // Draw the circular range
    }

    private void DrawRange()
    // Draws a circle using the LineRenderer to indicate tower range
    {
        int segments = 60; // Number of segments for circle
        float angle = 0f;
        rangeIndicator.positionCount = segments + 1; // Total points in circle

        for (int i = 0; i <= segments; i++)
        {
            float x = Mathf.Sin(Mathf.Deg2Rad * angle) * range; // X-coordinate
            float z = Mathf.Cos(Mathf.Deg2Rad * angle) * range; // Z-coordinate
            rangeIndicator.SetPosition(i, new Vector3(x, 0.05f, z) + transform.position); // Slightly above ground
            angle += 360f / segments; // Increment angle
        }
    }

    public void ShowStatsUI(bool show)
    // Shows or hides the floating stats UI above the tower
    {
        if (!show)
        {
            if (statsUIInstance != null)
                statsUIInstance.SetActive(false); // Hide UI
            return;
        }

        if (statsUIInstance == null && statsUIPrefab != null)
        {
            statsUIInstance = Instantiate(statsUIPrefab);         // Create stats UI
            statsUIInstance.transform.position = transform.position + statsOffset;
            statsUIInstance.transform.rotation = Quaternion.identity;
            statsUIInstance.transform.localScale = Vector3.one * statsWorldScale;

            Text uiText = statsUIInstance.GetComponentInChildren<Text>(); // Legacy text
            TMP_Text tmpText = statsUIInstance.GetComponentInChildren<TMP_Text>(); // TMP text

            if (uiText != null)
            {
                if (statsFont != null) uiText.font = statsFont; // Assign font
                uiText.color = statsColor;                       // Set color
                uiText.fontSize = (int)statsFontSize;           // Set font size
                uiText.alignment = TextAnchor.MiddleCenter;     // Center text
            }
            else if (tmpText != null)
            {
                if (statsTMPFont != null) tmpText.font = statsTMPFont; // Assign TMP font
                tmpText.color = statsColor;                              // Set color
                tmpText.fontSize = statsFontSize;                        // Set font size
                tmpText.alignment = TextAlignmentOptions.Center;         // Center text
            }
        }

        if (statsUIInstance != null)
        {
            statsUIInstance.SetActive(true); // Show UI

            string statsText = $"Damage: {damage}  Range: {range}  FireRate: {fireRate}  Bullets: {firePoints.Count}"; // Stats text
            Text uiText = statsUIInstance.GetComponentInChildren<Text>();
            TMP_Text tmpText = statsUIInstance.GetComponentInChildren<TMP_Text>();

            if (uiText != null) uiText.text = statsText; // Update legacy text
            else if (tmpText != null) tmpText.text = statsText; // Update TMP text
        }
    }

    private void UpdateStatsUIPosition()
    // Keeps the stats UI positioned above the tower
    {
        if (statsUIInstance != null && statsUIInstance.activeSelf)
            statsUIInstance.transform.position = transform.position + statsOffset;
    }

    private void UpdateStatsUIRotation()
    // Rotates stats UI to face the camera smoothly
    {
        if (statsUIInstance != null && statsUIInstance.activeSelf && mainCamera != null)
        {
            Vector3 direction = statsUIInstance.transform.position - mainCamera.transform.position; // Direction to camera
            direction.y = 0; // Only rotate around Y-axis
            if (direction.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction); // Desired rotation
                statsUIInstance.transform.rotation = Quaternion.Slerp(
                    statsUIInstance.transform.rotation,
                    targetRotation,
                    rotationLerp // Smooth rotation
                );
            }
        }
    }
}
