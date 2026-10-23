using UnityEngine;

/// <summary>
/// Smoothly follows a target and moves slightly toward the mouse.
/// Attach to the Main Camera.
/// </summary>
public class CameraFollow2D : MonoBehaviour
{
    public Transform target;

    public Vector2 offset = new Vector2(0f, 1.5f);
    public float smoothTime = 0.15f;

    [Header("Mouse Movement")]
    public Vector2 mouseInfluence = new Vector2(0.6f, 0.4f);

    Vector3 velocity;

    void LateUpdate()
    {
        if (target == null) return;

        // Convert mouse position to a range of -1 to 1.
        float mouseX = (Input.mousePosition.x / Screen.width - 0.5f) * 2f;
        float mouseY = (Input.mousePosition.y / Screen.height - 0.5f) * 2f;

        // Calculate the camera's slight mouse-based movement.
        Vector2 mouseOffset = new Vector2(
            mouseX * mouseInfluence.x,
            mouseY * mouseInfluence.y
        );

        // Follow the player and add mouse movement.
        Vector3 goal = new Vector3(
            target.position.x + offset.x + mouseOffset.x,
            target.position.y + offset.y + mouseOffset.y,
            transform.position.z
        );

        // Smooth camera movement.
        transform.position = Vector3.SmoothDamp(
            transform.position,
            goal,
            ref velocity,
            smoothTime
        );
    }
}