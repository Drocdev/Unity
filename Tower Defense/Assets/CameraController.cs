using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float moveSpeed = 10f;
    public float rotateSpeed = 5f;

    private float yaw = 0f;   // rotation around Y (left/right)
    private float pitch = 45f; // rotation around X (up/down)

    void Update()
    {
        // --- Camera Movement (WASD) ---
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        Vector3 move = new Vector3(h, 0, v) * moveSpeed * Time.deltaTime;
        transform.Translate(move, Space.Self);

        // --- Camera Rotation (Right Mouse Drag) ---
        if (Input.GetMouseButton(1)) // Right mouse button held
        {
            yaw += Input.GetAxis("Mouse X") * rotateSpeed;
            pitch -= Input.GetAxis("Mouse Y") * rotateSpeed;
            pitch = Mathf.Clamp(pitch, 10f, 80f); // prevent flipping

            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }
    }
}
