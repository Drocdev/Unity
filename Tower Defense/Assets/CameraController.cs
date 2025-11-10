using UnityEngine;  // Provides Unity core classes

public class CameraController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 10f;    // Speed for horizontal movement (WASD)
    public float heightSpeed = 10f;  // Speed for vertical movement (Q/E)
    public float zoomSpeed = 50f;    // Speed for zooming in/out with mouse scroll

    [Header("Rotation")]
    public float rotateSpeed = 5f;   // Mouse sensitivity for rotation
    private float yaw = 0f;          // Rotation around Y-axis (left/right)
    private float pitch = 45f;       // Rotation around X-axis (up/down)

    [Header("Limits")]
    public float minHeight = 5f;     // Minimum camera height
    public float maxHeight = 50f;    // Maximum camera height

    void Update()
    // Called once per frame
    {
        // --- Horizontal movement (WASD) ---
        float h = Input.GetAxis("Horizontal"); // A/D or Left/Right arrows
        float v = Input.GetAxis("Vertical");   // W/S or Up/Down arrows
        Vector3 move = new Vector3(h, 0, v) * moveSpeed * Time.deltaTime; // Movement vector
        transform.Translate(move, Space.Self); // Move relative to camera's local axes

        // --- Vertical movement (Q/E) ---
        float yMove = 0f;
        if (Input.GetKey(KeyCode.E)) yMove += 1f; // Move up
        if (Input.GetKey(KeyCode.Q)) yMove -= 1f; // Move down
        transform.Translate(Vector3.up * yMove * heightSpeed * Time.deltaTime, Space.World); // Move vertically in world space

        // --- Scroll wheel zoom ---
        float scroll = Input.GetAxis("Mouse ScrollWheel"); // Get scroll input
        transform.Translate(Vector3.forward * scroll * zoomSpeed * Time.deltaTime, Space.Self); // Zoom forward/back relative to camera

        // Clamp camera height to min/max limits
        Vector3 pos = transform.position;
        pos.y = Mathf.Clamp(pos.y, minHeight, maxHeight);
        transform.position = pos;

        // --- Free rotation (Right Mouse Button) ---
        if (Input.GetMouseButton(1)) // Right mouse button held
        {
            yaw += Input.GetAxis("Mouse X") * rotateSpeed;    // Horizontal rotation
            pitch -= Input.GetAxis("Mouse Y") * rotateSpeed;  // Vertical rotation

            // Keep pitch in reasonable range to avoid overflow
            if (pitch > 360f) pitch -= 360f;
            if (pitch < -360f) pitch += 360f;

            transform.rotation = Quaternion.Euler(pitch, yaw, 0f); // Apply rotation
        }
    }
}
