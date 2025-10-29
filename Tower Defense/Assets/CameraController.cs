using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 10f;    // Horizontal movement speed
    public float heightSpeed = 10f;  // Up/down movement speed
    public float zoomSpeed = 50f;    // Scroll wheel zoom speed

    [Header("Rotation")]
    public float rotateSpeed = 5f;   // Mouse rotation sensitivity
    private float yaw = 0f;          // Y-axis rotation (left/right)
    private float pitch = 45f;       // X-axis rotation (up/down)

    [Header("Limits")]
    public float minHeight = 5f;     // Min camera height
    public float maxHeight = 50f;    // Max camera height

    void Update()
    {
        // --- Horizontal movement (WASD) ---
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        Vector3 move = new Vector3(h, 0, v) * moveSpeed * Time.deltaTime;
        transform.Translate(move, Space.Self);

        // --- Vertical movement (Q/E) ---
        float yMove = 0f;
        if (Input.GetKey(KeyCode.E)) yMove += 1f;
        if (Input.GetKey(KeyCode.Q)) yMove -= 1f;
        transform.Translate(Vector3.up * yMove * heightSpeed * Time.deltaTime, Space.World);

        // --- Scroll wheel zoom ---
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        transform.Translate(Vector3.forward * scroll * zoomSpeed * Time.deltaTime, Space.Self);

        // Clamp camera height
        Vector3 pos = transform.position;
        pos.y = Mathf.Clamp(pos.y, minHeight, maxHeight);
        transform.position = pos;

        // --- Free rotation (Right Mouse Button) ---
        if (Input.GetMouseButton(1))
        {
            yaw += Input.GetAxis("Mouse X") * rotateSpeed;
            pitch -= Input.GetAxis("Mouse Y") * rotateSpeed;

            // Optional: keep pitch in reasonable range to avoid float overflow
            if (pitch > 360f) pitch -= 360f;
            if (pitch < -360f) pitch += 360f;

            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }
    }
}
