using UnityEngine;

public class TowerPlacer : MonoBehaviour
{
    public LayerMask placementMask;     // Ground layer
    public GameObject[] towerPrefabs;   // All tower types
    public Camera mainCamera;

    private int selectedTowerIndex = 0; // Which tower is currently selected

    void Update()
    {
        SelectTower();
        if (Input.GetMouseButtonDown(0))
            PlaceTower();
    }

    void SelectTower()
    {
        // Example: scroll wheel to switch tower
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0f) selectedTowerIndex = (selectedTowerIndex + 1) % towerPrefabs.Length;
        else if (scroll < 0f) selectedTowerIndex--;
        if (selectedTowerIndex < 0) selectedTowerIndex = towerPrefabs.Length - 1;
    }

    void PlaceTower()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, placementMask))
        {
            Instantiate(towerPrefabs[selectedTowerIndex], hit.point, Quaternion.identity);
        }
    }
}
