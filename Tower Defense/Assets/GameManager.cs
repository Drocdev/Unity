using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    [Header("Camera")]
    public Camera mainCamera;

    [Header("Towers")]
    public List<GameObject> towerPrefabs;

    [Header("Tower Offsets")]
    public List<Vector3> towerOffsets;

    [Header("Placement")]
    public LayerMask placementMask;
    public float towerRotation = 0f;

    [Header("Currency")]
    public int gold = 100;

    [Header("Tower Costs")]
    public List<int> towerCosts;

    private GameObject ghostObject;
    private int selectedTowerIndex = 0;
    private bool ghostActive = false;
    private Tower selectedTower = null;

    void Update()
    {
        HandleTowerSelection();
        HandleTowerPlacement();
        HandleTowerClick();
    }

    void HandleTowerSelection()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) ToggleTowerGhost(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) ToggleTowerGhost(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) ToggleTowerGhost(2);
    }

    void ToggleTowerGhost(int index)
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

        if (ghostObject != null)
            Destroy(ghostObject);

        ghostObject = Instantiate(towerPrefabs[selectedTowerIndex]);
        ghostObject.name = towerPrefabs[selectedTowerIndex].name + "_Ghost";

        Quaternion rotationOffset = Quaternion.Euler(0, towerRotation, 0);
        ghostObject.transform.rotation = towerPrefabs[selectedTowerIndex].transform.rotation * rotationOffset;

        MonoBehaviour[] scripts = ghostObject.GetComponentsInChildren<MonoBehaviour>(true);
        foreach (var s in scripts) s.enabled = false;

        Collider[] colliders = ghostObject.GetComponentsInChildren<Collider>(true);
        foreach (var c in colliders) c.enabled = false;

        ApplyGhostTransparency(ghostObject);

        Tower towerComp = ghostObject.GetComponent<Tower>();
        if (towerComp != null)
            towerComp.ShowRange(true);

        ghostActive = true;
    }

    void HandleTowerPlacement()
    {
        if (!ghostActive || ghostObject == null) return;

        Vector3 currentOffset = towerOffsets[selectedTowerIndex];

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, placementMask))
        {
            ghostObject.transform.position = hit.point + currentOffset;

            if (Input.GetMouseButtonDown(0))
                PlaceTower(hit.point);
        }
    }

    void PlaceTower(Vector3 position)
    {
        if (selectedTowerIndex < 0 || selectedTowerIndex >= towerPrefabs.Count) return;

        int cost = towerCosts[selectedTowerIndex];
        if (gold < cost) return;

        gold -= cost;

        Vector3 currentOffset = towerOffsets[selectedTowerIndex];

        GameObject placedTower = Instantiate(
            towerPrefabs[selectedTowerIndex],
            position + currentOffset,
            ghostObject.transform.rotation
        );

        Tower towerComp = placedTower.GetComponent<Tower>();
        if (towerComp != null)
            towerComp.ShowRange(false);
    }

    void HandleTowerClick()
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
}
