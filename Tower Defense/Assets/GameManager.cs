using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Header("Camera")]
    public Camera mainCamera;

    [Header("Towers")]
    public List<GameObject> towerPrefabs;
    public List<Vector3> towerOffsets;
    public List<int> towerCosts;

    [Header("Placement")]
    public LayerMask placementMask;
    public float towerRotation = 0f;

    [Header("Gold")]
    public int startingGold = 100;
    public int gold = 100;

    [Header("Gold UI")]
    public Canvas goldCanvas;
    public TMP_FontAsset goldFont;
    public int goldFontSize = 36;
    public Color goldColor = Color.yellow;

    [Header("Wave UI")]
    public Color waveColor = Color.white;
    public int waveFontSize = 30;

    [Header("Base")]
    public int baseHP = 5;
    public TMP_FontAsset baseFont;
    public int baseFontSize = 30;
    public Color baseColor = Color.red;
    private TextMeshProUGUI baseText;

    [Header("Game Over UI")]
    public TMP_FontAsset gameOverFont;
    public int gameOverFontSize = 48;
    public Color gameOverColor = Color.red;
    public float gameOverDelay = 3f;
    private TextMeshProUGUI gameOverText;

    [Header("Level Complete UI")]
    public TMP_FontAsset levelCompleteFont;
    public int levelCompleteFontSize = 48;
    public Color levelCompleteColor = Color.green;
    private TextMeshProUGUI levelCompleteText;

    [Header("Level Settings")]
    public string nextLevelSceneName;

    private GameObject ghostObject;
    private int selectedTowerIndex = 0;
    private bool ghostActive = false;
    private Tower selectedTower = null;

    private TextMeshProUGUI goldText;
    private TextMeshProUGUI waveText;
    private int currentWave = 0;

    [HideInInspector]
    public bool isGameOver = false;

    void Start()
    {
        CreateGoldUI();
        CreateWaveUI();
        CreateBaseUI();
        UpdateGoldUI();
        UpdateWaveUI(1);
        UpdateBaseHPUI();
    }

    void Update()
    {
        if (isGameOver) return;

        HandleTowerSelection();
        HandleTowerPlacement();
        HandleTowerClick();
    }

    #region Tower Selection & Placement
    public void HandleTowerSelection()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) ToggleTowerGhost(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) ToggleTowerGhost(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) ToggleTowerGhost(2);
    }

    public void ToggleTowerGhost(int index)
    {
        if (index < 0 || index >= towerPrefabs.Count) return;

        if (ghostActive && selectedTowerIndex == index)
        {
            Destroy(ghostObject);
            ghostObject = null;
            ghostActive = false;
            return;
        }

        selectedTowerIndex = index;

        if (ghostObject != null) Destroy(ghostObject);

        ghostObject = Instantiate(towerPrefabs[selectedTowerIndex]);
        ghostObject.name = towerPrefabs[selectedTowerIndex].name + "_Ghost";

        Quaternion rotationOffset = Quaternion.Euler(0, towerRotation, 0);
        ghostObject.transform.rotation = towerPrefabs[selectedTowerIndex].transform.rotation * rotationOffset;

        // Disable scripts
        MonoBehaviour[] scripts = ghostObject.GetComponentsInChildren<MonoBehaviour>(true);
        foreach (var s in scripts) s.enabled = false;

        // Disable colliders
        Collider[] colliders = ghostObject.GetComponentsInChildren<Collider>(true);
        foreach (var c in colliders) c.enabled = false;

        ApplyGhostTransparency(ghostObject);

        Tower towerComp = ghostObject.GetComponent<Tower>();
        if (towerComp != null) towerComp.ShowRange(true);

        ghostActive = true;
    }

    public void HandleTowerPlacement()
    {
        if (!ghostActive || ghostObject == null) return;

        Vector3 currentOffset = towerOffsets[selectedTowerIndex];
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, placementMask))
        {
            ghostObject.transform.position = hit.point + currentOffset;

            if (Input.GetMouseButtonDown(0)) PlaceTower(hit.point);
        }
    }

    public void PlaceTower(Vector3 position)
    {
        if (selectedTowerIndex < 0 || selectedTowerIndex >= towerPrefabs.Count) return;

        int cost = towerCosts[selectedTowerIndex];
        if (gold < cost) return;

        gold -= cost;
        UpdateGoldUI();

        Vector3 currentOffset = towerOffsets[selectedTowerIndex];

        GameObject placedTower = Instantiate(
            towerPrefabs[selectedTowerIndex],
            position + currentOffset,
            ghostObject.transform.rotation
        );

        // Automatically assign Tower tag if missing
        if (!placedTower.CompareTag("Tower")) placedTower.tag = "Tower";

        Tower towerComp = placedTower.GetComponent<Tower>();
        if (towerComp != null) towerComp.ShowRange(false);
    }

    void ApplyGhostTransparency(GameObject obj)
    {
        var allRenderers = new List<Renderer>();
        allRenderers.AddRange(obj.GetComponentsInChildren<Renderer>(true));
        allRenderers.AddRange(obj.GetComponentsInChildren<SkinnedMeshRenderer>(true));

        foreach (Renderer r in allRenderers)
        {
            Material[] mats = new Material[r.materials.Length];
            for (int i = 0; i < mats.Length; i++)
            {
                Material original = r.materials[i];
                Material mat = new Material(Shader.Find("Standard"));
                mat.CopyPropertiesFromMaterial(original);
                mat.SetFloat("_Mode", 3);

                Color c = mat.color;
                c.a = 0.5f;
                mat.color = c;

                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = 3000;

                mats[i] = mat;
            }
            r.materials = mats;
        }
    }
    #endregion

    #region Tower Click
    public void HandleTowerClick()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Tower clickedTower = hit.collider.GetComponentInParent<Tower>();

                if (clickedTower != null)
                {
                    if (selectedTower != null && selectedTower != clickedTower)
                    {
                        selectedTower.ShowRange(false);
                        selectedTower.ShowStatsUI(false);
                    }

                    selectedTower = clickedTower;
                    selectedTower.ShowRange(true);
                    selectedTower.ShowStatsUI(true);
                }
                else
                {
                    if (selectedTower != null)
                    {
                        selectedTower.ShowRange(false);
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
            GameObject canvasGO = new GameObject("GoldCanvas");
            goldCanvas = canvasGO.AddComponent<Canvas>();
            goldCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
        }

        GameObject textGO = new GameObject("GoldText");
        textGO.transform.SetParent(goldCanvas.transform, false);
        goldText = textGO.AddComponent<TextMeshProUGUI>();
        goldText.font = goldFont != null ? goldFont : TMP_Settings.defaultFontAsset;
        goldText.fontSize = goldFontSize;
        goldText.color = goldColor;
        goldText.alignment = TextAlignmentOptions.TopRight;

        RectTransform rt = goldText.rectTransform;
        rt.anchorMin = new Vector2(1, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(1, 1);
        rt.anchoredPosition = new Vector2(-10, -10);
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
        rt.anchorMin = new Vector2(0, 1);
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
        rt.anchorMin = new Vector2(0.5f, 1);
        rt.anchorMax = new Vector2(0.5f, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.anchoredPosition = new Vector2(0, -10);
        rt.sizeDelta = new Vector2(250, 50);
    }

    public void UpdateWaveUI(int waveNumber)
    {
        currentWave = waveNumber;
        if (waveText != null) waveText.text = $"Wave: {waveNumber}";
    }

    public void AddGold(int amount)
    {
        gold += amount;
        UpdateGoldUI();
    }

    public void UpdateGoldUI()
    {
        if (goldText != null) goldText.text = $"Gold: {gold}";
    }

    public void BaseTakeDamage(int damage)
    {
        baseHP -= damage;
        if (baseHP <= 0)
        {
            baseHP = 0;
            UpdateBaseHPUI();
            GameOver();
        }
        else
        {
            UpdateBaseHPUI();
        }
    }

    public void UpdateBaseHPUI()
    {
        if (baseText != null) baseText.text = $"Base HP: {baseHP}";
    }
    #endregion

    #region Game Over & Level Complete
    private void ShowGameOverUI()
    {
        if (goldCanvas == null) return;

        if (gameOverText != null) Destroy(gameOverText.gameObject);

        GameObject textGO = new GameObject("GameOverText");
        textGO.transform.SetParent(goldCanvas.transform, false);

        gameOverText = textGO.AddComponent<TextMeshProUGUI>();
        gameOverText.font = gameOverFont != null ? gameOverFont : TMP_Settings.defaultFontAsset;
        gameOverText.fontSize = gameOverFontSize;
        gameOverText.color = gameOverColor;
        gameOverText.alignment = TextAlignmentOptions.Center;
        gameOverText.text = "BASE DESTROYED! GAME OVER!";

        RectTransform rt = gameOverText.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
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
        isGameOver = true;

        StopAllCoroutines();

        // Destroy all enemies
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (var e in enemies) Destroy(e);

        // Destroy all towers
        GameObject[] towers = GameObject.FindGameObjectsWithTag("Tower");
        foreach (var t in towers) Destroy(t);

        ShowGameOverUI();

        StartCoroutine(RestartGameAfterDelay());
    }

    private IEnumerator RestartGameAfterDelay()
    {
        yield return new WaitForSeconds(gameOverDelay);

        if (gameOverText != null) Destroy(gameOverText.gameObject);

        baseHP = 5;
        UpdateBaseHPUI();

        gold = startingGold;
        UpdateGoldUI();

        UpdateWaveUI(1);

        isGameOver = false;

        // Reset waves
        EnemySpawner spawner = FindObjectOfType<EnemySpawner>();
        if (spawner != null) spawner.ResetWaves();
    }

    public void LevelComplete()
    {
        Debug.Log("All waves complete! Level finished!");
        isGameOver = true;

        StopAllCoroutines();

        // Destroy all enemies
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (var e in enemies) Destroy(e);

        ShowLevelCompleteUI();

        StartCoroutine(LoadNextLevelAfterDelay());
    }

    private IEnumerator LoadNextLevelAfterDelay()
    {
        yield return new WaitForSeconds(gameOverDelay);

        if (levelCompleteText != null) Destroy(levelCompleteText.gameObject);

        if (!string.IsNullOrEmpty(nextLevelSceneName))
        {
            SceneManager.LoadScene(nextLevelSceneName);
        }
    }
    #endregion
}
