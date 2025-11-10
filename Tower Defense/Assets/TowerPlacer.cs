using UnityEngine;
// Imports the UnityEngine namespace, which provides access to Unity's core classes like MonoBehaviour, GameObject, etc.

public class TowerPlacer : MonoBehaviour
// Defines a new class called TowerPlacer that inherits from MonoBehaviour so it can be attached to GameObjects in Unity.
{
    public LayerMask placementMask;     // Defines which layers can be used for tower placement (e.g., ground).
    public GameObject[] towerPrefabs;   // An array holding all available tower prefab objects that can be placed.
    public Camera mainCamera;           // Reference to the main camera used to raycast from the mouse position.

    private int selectedTowerIndex = 0; // Keeps track of which tower type is currently selected (starting at 0).

    void Update()
    // Unity’s built-in method called once per frame.
    {
        SelectTower();                  // Calls a method to handle tower selection logic each frame.
        if (Input.GetMouseButtonDown(0))// Checks if the left mouse button (button index 0) was just pressed.
            PlaceTower();               // If so, calls the PlaceTower() method to try to place a tower.
    }

    void SelectTower()
    // Handles switching between different tower prefabs (using scroll wheel input).
    {
        // Example: scroll wheel to switch tower
        float scroll = Input.GetAxis("Mouse ScrollWheel"); // Gets the scroll wheel input value (positive or negative).
        if (scroll > 0f)                                   // If the scroll wheel was moved forward (up).
            selectedTowerIndex = (selectedTowerIndex + 1) % towerPrefabs.Length; // Go to the next tower, wrapping around if at the end.
        else if (scroll < 0f)                              // If the scroll wheel was moved backward (down).
            selectedTowerIndex--;                          // Go to the previous tower.
        if (selectedTowerIndex < 0)                        // If index went below 0.
            selectedTowerIndex = towerPrefabs.Length - 1;  // Wrap around to the last tower in the list.
    }

    void PlaceTower()
    // Handles the logic for placing a tower on the ground.
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition); // Creates a ray from the camera through the current mouse position.
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, placementMask)) // Shoots the ray and checks if it hits something within 100 units on the allowed layer(s).
        {
            Instantiate(towerPrefabs[selectedTowerIndex], hit.point, Quaternion.identity);
            // Creates (spawns) a copy of the currently selected tower prefab at the hit position, with no rotation (identity).
        }
    }
}
