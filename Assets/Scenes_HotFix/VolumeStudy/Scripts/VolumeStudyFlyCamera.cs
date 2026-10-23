using UnityEngine;

/// <summary>
/// Hold the right mouse button to look. WASD moves, Q/E goes down/up, Left Shift speeds up.
/// </summary>
public class VolumeStudyFlyCamera : MonoBehaviour
{
    public float moveSpeed = 6f;
    public float fastMultiplier = 3f;
    public float lookSpeed = 2.2f;

    void Update()
    {
        if (Input.GetMouseButton(1))
        {
            float yaw = Input.GetAxis("Mouse X") * lookSpeed;
            float pitch = -Input.GetAxis("Mouse Y") * lookSpeed;
            Vector3 euler = transform.eulerAngles;
            euler.x += pitch;
            euler.y += yaw;
            euler.z = 0f;
            transform.eulerAngles = euler;
        }

        float speed = moveSpeed;
        if (Input.GetKey(KeyCode.LeftShift))
            speed *= fastMultiplier;

        Vector3 move = Vector3.zero;
        if (Input.GetKey(KeyCode.W)) move += Vector3.forward;
        if (Input.GetKey(KeyCode.S)) move += Vector3.back;
        if (Input.GetKey(KeyCode.A)) move += Vector3.left;
        if (Input.GetKey(KeyCode.D)) move += Vector3.right;
        if (Input.GetKey(KeyCode.E)) move += Vector3.up;
        if (Input.GetKey(KeyCode.Q)) move += Vector3.down;
        transform.Translate(move * speed * Time.deltaTime, Space.Self);
    }
}
