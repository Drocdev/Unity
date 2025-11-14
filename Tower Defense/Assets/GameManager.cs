using UnityEngine;                   // Core Unity engine functionality
using System.Collections;            // Enables IEnumerator / coroutines
using System.Collections.Generic;    // Enables List<T>
using TMPro;                         // TextMeshPro for UI text
using UnityEngine.UI;                // Unity UI components like Canvas, Image, etc.
using UnityEngine.SceneManagement;   // Scene management for loading/unloading scenes

public class GameManager : MonoBehaviour
{
    [Header("Camera")]
    public Camera mainCamera; // Reference to main camera for raycasting

    [Header("Towers")]
    public List<GameObject> towerPrefabs; // List of tower prefabs available for placement
    public List<Vector3> towerOffsets;    // Position offsets for each tower
    public List<int> towerCosts;          // Gold cost of each tower

    [Header("Placement")]
    public LayerMask placementMask;       // Layer mask for valid tower placement
    public float towerRotation = 0f;      // Rotation applied to towers when placed

    [Header("Gold")]
    public int startingGold = 100; // Starting gold amount
    public int gold = 100;         // Current gold

    [Header("Gold UI")]
    public Canvas goldCanvas;       // Canvas for displaying gold
    public TMP_FontAsset goldFont;  // Font for gold UI text
    public int goldFontSize = 36;   // Font size
    public Color goldColor = Color.yellow; // Text color

    [Header("Wave UI")]
    public Color waveColor = Color.white; // Color of wave text
    public int waveFontSize = 30;         // Font size for wave UI

    [Header("Base")]
    public int baseHP = 5;                 // Base health points
    public TMP_FontAsset baseFont;         // Font for base HP UI
    public int baseFontSize = 30;          // Font size
    public Color baseColor = Color.red;    // Color of base HP text
    private TextMeshProUGUI baseText;      // Reference to base HP text component

    [Header("Game Over UI")]
    public TMP_FontAsset gameOverFont; // Font for game over message
    public int gameOverFontSize = 48;  // Font size
    public Color gameOverColor = Color.red; // Text color
    public float gameOverDelay = 3f;       // Delay before restarting level
    private TextMeshProUGUI gameOverText;  // Reference to game over text object

    [Header("Level Complete UI")]
    public TMP_FontAsset levelCompleteFont; // Font for level complete message
    public int levelCompleteFontSize = 48;  // Font size
    public Color levelCompleteColor = Color.green; // Text color
    private TextMeshProUGUI levelCompleteText; // Reference to level complete text

    [Header("Level Settings")]
    public string nextLevelSceneName; // Scene name to load when level is complete

    private GameObject ghostObject;         // Temporary ghost tower for preview
    private int selectedTowerIndex = 0;     // Currently selected tower index
    private bool ghostActive = false;       // Is ghost tower active?
    private Tower selectedTower = null;     // Currently selected tower for stats display

    private TextMeshProUGUI goldText;       // Reference to gold UI text
    private TextMeshProUGUI waveText;       // Reference to wave UI text
    private int currentWave = 0;            // Tracks current wave number

    [HideInInspector]
    public bool isGameOver = false;         // Game over flag

    void Start()
    {
        CreateGoldUI();        // Create gold UI
        CreateWaveUI();        // Create wave UI
        CreateBaseUI();        // Create base HP UI
        UpdateGoldUI();        // Update gold display
        UpdateWaveUI(1);       // Initialize wave number
        UpdateBaseHPUI();      // Update base HP display
    }

    void Update()
    {
        if (isGameOver) return;       // Stop game updates if game over

        HandleTowerSelection();       // Check for tower selection input
        HandleTowerPlacement();       // Handle placement of ghost tower
        HandleTowerClick();           // Handle clicking on towers
    }

    #region Tower Selection & Placement
    public void HandleTowerSelection()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) ToggleTowerGhost(0); // Press 1 → select tower 0
        if (Input.GetKeyDown(KeyCode.Alpha2)) ToggleTowerGhost(1); // Press 2 → select tower 1
        if (Input.GetKeyDown(KeyCode.Alpha3)) ToggleTowerGhost(2); // Press 3 → select tower 2
    }

    public void ToggleTowerGhost(int index)
    {
        if (index < 0 || index >= towerPrefabs.Count) return; // Validate index

        if (ghostActive && selectedTowerIndex == index)
        {
            Destroy(ghostObject);   // Remove ghost if same tower selected
            ghostObject = null;
            ghostActive = false;
            return;
        }

        selectedTowerIndex = index;     // Update selected tower

        if (ghostObject != null) Destroy(ghostObject); // Remove previous ghost

        ghostObject = Instantiate(towerPrefabs[selectedTowerIndex]); // Create ghost tower
        ghostObject.name = towerPrefabs[selectedTowerIndex].name + "_Ghost"; // Rename

        Quaternion rotationOffset = Quaternion.Euler(0, towerRotation, 0); // Apply rotation
        ghostObject.transform.rotation = towerPrefabs[selectedTowerIndex].transform.rotation * rotationOffset;

        // Disable all scripts so ghost does not behave
        MonoBehaviour[] scripts = ghostObject.GetComponentsInChildren<MonoBehaviour>(true);
        foreach (var s in scripts) s.enabled = false;

        // Disable colliders so ghost does not collide
        Collider[] colliders = ghostObject.GetComponentsInChildren<Collider>(true);
        foreach (var c in colliders) c.enabled = false;

        ApplyGhostTransparency(ghostObject); // Make semi-transparent

        Tower towerComp = ghostObject.GetComponent<Tower>();
        if (towerComp != null) towerComp.ShowRange(true); // Show tower range

        ghostActive = true; // Ghost is now active
    }

    public void HandleTowerPlacement()
    {
        if (!ghostActive || ghostObject == null) return; // Only place if ghost exists

        Vector3 currentOffset = towerOffsets[selectedTowerIndex]; // Get placement offset
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition); // Raycast from mouse

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, placementMask))
        {
            ghostObject.transform.position = hit.point + currentOffset; // Move ghost to hit point

            if (Input.GetMouseButtonDown(0)) PlaceTower(hit.point); // Left click places tower
        }
    }

    public void PlaceTower(Vector3 position)
    {
        if (selectedTowerIndex < 0 || selectedTowerIndex >= towerPrefabs.Count) return; // Validate index

        int cost = towerCosts[selectedTowerIndex]; // Get tower cost
        if (gold < cost) return; // Check if player has enough gold

        gold -= cost; // Deduct gold
        UpdateGoldUI(); // Update UI

        Vector3 currentOffset = towerOffsets[selectedTowerIndex]; // Placement offset

        GameObject placedTower = Instantiate(
            towerPrefabs[selectedTowerIndex],
            position + currentOffset,
            ghostObject.transform.rotation // Copy ghost rotation
        );

        if (!placedTower.CompareTag("Tower")) placedTower.tag = "Tower"; // Assign tag if missing

        Tower towerComp = placedTower.GetComponent<Tower>();
        if (towerComp != null) towerComp.ShowRange(false); // Hide range after placement
    }

    void ApplyGhostTransparency(GameObject obj)
    {
        var allRenderers = new List<Renderer>();
        allRenderers.AddRange(obj.GetComponentsInChildren<Renderer>(true)); // Add all normal renderers
        allRenderers.AddRange(obj.GetComponentsInChildren<SkinnedMeshRenderer>(true)); // Add skinned mesh renderers

        foreach (Renderer r in allRenderers)
        {
            Material[] mats = new Material[r.materials.Length];
            for (int i = 0; i < mats.Length; i++)
            {
                Material original = r.materials[i];
                Material mat = new Material(Shader.Find("Standard")); // Create new material
                mat.CopyPropertiesFromMaterial(original);            // Copy original properties
                mat.SetFloat("_Mode", 3);                            // Set transparency mode

                Color c = mat.color;
                c.a = 0.5f; // Half-transparent
                mat.color = c;

                // Set blending mode for transparency
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = 3000;

                mats[i] = mat; // Assign material
            }
            r.materials = mats; // Apply materials to renderer
        }
    }
    #endregion

    #region Tower Click
    public void HandleTowerClick()
    {
        if (Input.GetMouseButtonDown(0)) // Left mouse click
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition); // Raycast
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Tower clickedTower = hit.collider.GetComponentInParent<Tower>(); // Check for tower

                if (clickedTower != null)
                {
                    if (selectedTower != null && selectedTower != clickedTower)
                    {
                        selectedTower.ShowRange(false);    // Hide old tower range
                        selectedTower.ShowStatsUI(false);  // Hide stats UI
                    }

                    selectedTower = clickedTower;
                    selectedTower.ShowRange(true);  // Show new tower range
                    selectedTower.ShowStatsUI(true); // Show stats UI
                }
                else
                {
                    if (selectedTower != null)
                    {
                        selectedTower.ShowRange(false);   // Hide range if click empty
                        selectedTower.ShowStatsUI(false);
                        selectedTower = null;
                    }
                }
            }
        }
    }
    #endregion

    #region UI
    public void CreateGoldUI()
    {
        if (goldCanvas == null)
        {
            GameObject canvasGO = new GameObject("GoldCanvas"); // Create canvas if missing
            goldCanvas = canvasGO.AddComponent<Canvas>();
            goldCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
        }

        GameObject textGO = new GameObject("GoldText"); // Create text object
        textGO.transform.SetParent(goldCanvas.transform, false);
        goldText = textGO.AddComponent<TextMeshProUGUI>();
        goldText.font = goldFont != null ? goldFont : TMP_Settings.defaultFontAsset;
        goldText.fontSize = goldFontSize;
        goldText.color = goldColor;
        goldText.alignment = TextAlignmentOptions.TopRight;

        RectTransform rt = goldText.rectTransform;
        rt.anchorMin = new Vector2(1, 1); // Top-right corner
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(1, 1);
        rt.anchoredPosition = new Vector2(-10, -10); // Slight offset
        rt.sizeDelta = new Vector2(250, 50);
    }

    public void CreateWaveUI()
    {
        GameObject textGO = new GameObject("WaveText");
        textGO.transform.SetParent(goldCanvas.transform, false);
        waveText = textGO.AddComponent<TextMeshProUGUI>();
        waveText.font = goldFont != null ? goldFont : TMP_Settings.defaultFontAsset;
        waveText.fontSize = waveFontSize;
        waveText.color = waveColor;
        waveText.alignment = TextAlignmentOptions.TopLeft;

        RectTransform rt = waveText.rectTransform;
        rt.anchorMin = new Vector2(0, 1); // Top-left corner
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(10, -10);
        rt.sizeDelta = new Vector2(250, 50);
    }

    public void CreateBaseUI()
    {
        GameObject textGO = new GameObject("BaseHPText");
        textGO.transform.SetParent(goldCanvas.transform, false);
        baseText = textGO.AddComponent<TextMeshProUGUI>();
        baseText.font = baseFont != null ? baseFont : TMP_Settings.defaultFontAsset;
        baseText.fontSize = baseFontSize;
        baseText.color = baseColor;
        baseText.alignment = TextAlignmentOptions.Top;

        RectTransform rt = baseText.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 1); // Top center
        rt.anchorMax = new Vector2(0.5f, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.anchoredPosition = new Vector2(0, -10);
        rt.sizeDelta = new Vector2(250, 50);
    }

    public void UpdateWaveUI(int waveNumber)
    {
        currentWave = waveNumber;                     // Update current wave number
        if (waveText != null) waveText.text = $"Wave: {waveNumber}"; // Update text
    }

    public void AddGold(int amount)
    {
        gold += amount;   // Add gold
        UpdateGoldUI();   // Refresh UI
    }

    public void UpdateGoldUI()
    {
        if (goldText != null) goldText.text = $"Gold: {gold}"; // Update gold text
    }

    public void BaseTakeDamage(int damage)
    {
        baseHP -= damage;          // Reduce base HP
        if (baseHP <= 0)
        {
            baseHP = 0;            // Clamp at 0
            UpdateBaseHPUI();      // Update UI
            GameOver();            // Trigger game over
        }
        else
        {
            UpdateBaseHPUI();      // Update UI
        }
    }

    public void UpdateBaseHPUI()
    {
        if (baseText != null) baseText.text = $"Base HP: {baseHP}"; // Update base HP UI
    }
    #endregion

    #region Game Over & Level Complete
    private void ShowGameOverUI()
    {
        if (goldCanvas == null) return; // Require canvas

        if (gameOverText != null) Destroy(gameOverText.gameObject); // Remove old text

        GameObject textGO = new GameObject("GameOverText");
        textGO.transform.SetParent(goldCanvas.transform, false);

        gameOverText = textGO.AddComponent<TextMeshProUGUI>();
        gameOverText.font = gameOverFont != null ? gameOverFont : TMP_Settings.defaultFontAsset;
        gameOverText.fontSize = gameOverFontSize;
        gameOverText.color = gameOverColor;
        gameOverText.alignment = TextAlignmentOptions.Center;
        gameOverText.text = "BASE DESTROYED! GAME OVER!";

        RectTransform rt = gameOverText.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0.5f); // Center
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(600, 200);
    }

    private void ShowLevelCompleteUI()
    {
        if (goldCanvas == null) return;

        if (levelCompleteText != null) Destroy(levelCompleteText.gameObject);

        GameObject textGO = new GameObject("LevelCompleteText");
        textGO.transform.SetParent(goldCanvas.transform, false);

        levelCompleteText = textGO.AddComponent<TextMeshProUGUI>();
        levelCompleteText.font = levelCompleteFont != null ? levelCompleteFont : TMP_Settings.defaultFontAsset;
        levelCompleteText.fontSize = levelCompleteFontSize;
        levelCompleteText.color = levelCompleteColor;
        levelCompleteText.alignment = TextAlignmentOptions.Center;
        levelCompleteText.text = "LEVEL COMPLETE!";

        RectTransform rt = levelCompleteText.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(600, 200);
    }

    public void GameOver()
    {
        Debug.Log("Base destroyed! Game Over!");
        isGameOver = true;             // Stop game updates

        StopAllCoroutines();           // Stop running coroutines

        // Destroy all enemies
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (var e in enemies) Destroy(e);

        // Destroy all towers
        GameObject[] towers = GameObject.FindGameObjectsWithTag("Tower");
        foreach (var t in towers) Destroy(t);

        ShowGameOverUI();               // Show game over UI

        StartCoroutine(RestartGameAfterDelay()); // Restart after delay
    }

    private IEnumerator RestartGameAfterDelay()
    {
        yield return new WaitForSeconds(gameOverDelay); // Wait before restarting

        if (gameOverText != null) Destroy(gameOverText.gameObject); // Remove UI

        baseHP = 5;                    // Reset base HP
        UpdateBaseHPUI();

        gold = startingGold;           // Reset gold
        UpdateGoldUI();

        UpdateWaveUI(1);               // Reset wave

        isGameOver = false;            // Allow gameplay again

        EnemySpawner spawner = FindObjectOfType<EnemySpawner>(); // Find spawner
        if (spawner != null) spawner.ResetWaves();               // Reset waves
    }

    public void LevelComplete()
    {
        Debug.Log("All waves complete! Level finished!");
        isGameOver = true;

        StopAllCoroutines();           // Stop any active coroutines

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy"); // Remove enemies
        foreach (var e in enemies) Destroy(e);

        ShowLevelCompleteUI();         // Show level complete UI

        StartCoroutine(LoadNextLevelAfterDelay());
    }
    private IEnumerator LoadNextLevelAfterDelay()
    {
        yield return new WaitForSeconds(gameOverDelay); // Wait before loading next level

        if (levelCompleteText != null) Destroy(levelCompleteText.gameObject); // Remove UI text

        if (!string.IsNullOrEmpty(nextLevelSceneName)) // Check if a next level is assigned
        {
            SceneManager.LoadScene(nextLevelSceneName); // Load the next level by scene name
        }
    }
    #endregion
} // End of GameManager class
