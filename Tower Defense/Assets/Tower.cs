using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class Tower : MonoBehaviour
{
    [Header("Combat")]
    public int damage = 10;
    public float range = 10f;
    public float fireRate = 1f;

    [Header("Fire Points (for multi-bullet / shotgun style)")]
    public List<Transform> firePoints;
    public List<GameObject> bulletPrefabs;
    public bool singleTargetPerFirePoint = false;

    [Header("Targeting")]
    public Transform castleTransform; // Automatically found if null

    private float fireCountdown = 0f;

    [Header("Range Visualization")]
    public LineRenderer rangeIndicator;
    public Color rangeColor = Color.green;
    public float rangeWidth = 0.05f;

    [Header("Floating Stats UI Settings")]
    public GameObject statsUIPrefab;
    public Vector3 statsOffset = new Vector3(0, 2f, 0);
    public Color statsColor = Color.white;
    public float statsFontSize = 24f;
    public float statsWorldScale = 1f;
    public Font statsFont;
    public TMP_FontAsset statsTMPFont;
    [Range(0f, 1f)]
    public float rotationLerp = 1f;

    private GameObject statsUIInstance;
    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;

        // Auto-find the castle if not assigned
        if (castleTransform == null)
        {
            GameObject castle = GameObject.FindGameObjectWithTag("Base");
            if (castle != null)
                castleTransform = castle.transform;
        }

        // Setup range indicator
        if (rangeIndicator == null)
        {
            GameObject indicator = new GameObject("RangeIndicator");
            indicator.transform.SetParent(transform);
            indicator.transform.localPosition = Vector3.zero;

            rangeIndicator = indicator.AddComponent<LineRenderer>();
            rangeIndicator.useWorldSpace = true;
        }

        rangeIndicator.startWidth = rangeWidth;
        rangeIndicator.endWidth = rangeWidth;
        rangeIndicator.material = new Material(Shader.Find("Unlit/Color"));
        rangeIndicator.material.color = rangeColor;
        rangeIndicator.enabled = false;
    }

    private void Update()
    {
        UpdateStatsUIPosition();

        if (fireCountdown <= 0f)
        {
            Shoot();
            fireCountdown = 1f / fireRate;
        }

        fireCountdown -= Time.deltaTime;
    }

    private void LateUpdate()
    {
        UpdateStatsUIRotation();
    }

    private void Shoot()
    {
        if (firePoints.Count == 0 || bulletPrefabs.Count == 0 || castleTransform == null) return;

        HashSet<Transform> assignedTargets = new HashSet<Transform>();

        for (int i = 0; i < firePoints.Count; i++)
        {
            Transform fp = firePoints[i];
            GameObject bulletPrefabToUse = bulletPrefabs[i % bulletPrefabs.Count];

            Transform fireTarget = null;

            if (singleTargetPerFirePoint)
            {
                fireTarget = FindClosestToCastleEnemy(assignedTargets);
                if (fireTarget != null)
                    assignedTargets.Add(fireTarget);
                else
                    continue;
            }
            else
            {
                fireTarget = FindClosestToCastleEnemy();
            }

            if (fireTarget == null) continue;

            GameObject bulletGO = Instantiate(bulletPrefabToUse, fp.position, fp.rotation);
            Bullet bullet = bulletGO.GetComponent<Bullet>();
            if (bullet != null)
            {
                bullet.damage = damage;
                bullet.Seek(fireTarget);
            }
        }
    }

    private Transform FindClosestToCastleEnemy(HashSet<Transform> exclude = null)
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        float closestDistance = Mathf.Infinity;
        GameObject nearestEnemy = null;

        foreach (GameObject enemy in enemies)
        {
            Transform t = enemy.transform;
            if (exclude != null && exclude.Contains(t)) continue;

            float distToCastle = Vector3.Distance(t.position, castleTransform.position);
            float distToTower = Vector3.Distance(transform.position, t.position);

            if (distToTower <= range && distToCastle < closestDistance)
            {
                closestDistance = distToCastle;
                nearestEnemy = enemy;
            }
        }

        return nearestEnemy != null ? nearestEnemy.transform : null;
    }

    public void ShowRange(bool show)
    {
        if (rangeIndicator == null) return;

        rangeIndicator.enabled = show;

        if (show)
            DrawRange();
    }

    private void DrawRange()
    {
        int segments = 60;
        float angle = 0f;
        rangeIndicator.positionCount = segments + 1;

        for (int i = 0; i <= segments; i++)
        {
            float x = Mathf.Sin(Mathf.Deg2Rad * angle) * range;
            float z = Mathf.Cos(Mathf.Deg2Rad * angle) * range;
            rangeIndicator.SetPosition(i, new Vector3(x, 0.05f, z) + transform.position);
            angle += 360f / segments;
        }
    }

    public void ShowStatsUI(bool show)
    {
        if (!show)
        {
            if (statsUIInstance != null)
                statsUIInstance.SetActive(false);
            return;
        }

        if (statsUIInstance == null && statsUIPrefab != null)
        {
            statsUIInstance = Instantiate(statsUIPrefab);
            statsUIInstance.transform.position = transform.position + statsOffset;
            statsUIInstance.transform.rotation = Quaternion.identity;
            statsUIInstance.transform.localScale = Vector3.one * statsWorldScale;

            Text uiText = statsUIInstance.GetComponentInChildren<Text>();
            TMP_Text tmpText = statsUIInstance.GetComponentInChildren<TMP_Text>();

            if (uiText != null)
            {
                if (statsFont != null) uiText.font = statsFont;
                uiText.color = statsColor;
                uiText.fontSize = (int)statsFontSize;
                uiText.alignment = TextAnchor.MiddleCenter;
            }
            else if (tmpText != null)
            {
                if (statsTMPFont != null) tmpText.font = statsTMPFont;
                tmpText.color = statsColor;
                tmpText.fontSize = statsFontSize;
                tmpText.alignment = TextAlignmentOptions.Center;
            }
        }

        if (statsUIInstance != null)
        {
            statsUIInstance.SetActive(true);

            string statsText = $"Damage: {damage}  Range: {range}  FireRate: {fireRate}  Bullets: {firePoints.Count}";
            Text uiText = statsUIInstance.GetComponentInChildren<Text>();
            TMP_Text tmpText = statsUIInstance.GetComponentInChildren<TMP_Text>();

            if (uiText != null) uiText.text = statsText;
            else if (tmpText != null) tmpText.text = statsText;
        }
    }

    private void UpdateStatsUIPosition()
    {
        if (statsUIInstance != null && statsUIInstance.activeSelf)
            statsUIInstance.transform.position = transform.position + statsOffset;
    }

    private void UpdateStatsUIRotation()
    {
        if (statsUIInstance != null && statsUIInstance.activeSelf && mainCamera != null)
        {
            Vector3 direction = statsUIInstance.transform.position - mainCamera.transform.position;
            direction.y = 0;
            if (direction.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                statsUIInstance.transform.rotation = Quaternion.Slerp(
                    statsUIInstance.transform.rotation,
                    targetRotation,
                    rotationLerp
                );
            }
        }
    }
}
