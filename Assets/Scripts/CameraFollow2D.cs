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
        // Time.deltaTime is 0 while the game is paused (timeScale 0); SmoothDamp divides by it and returns NaN.
        if (target == null || Time.deltaTime <= 0.0001f) return;
        if (float.IsNaN(velocity.x) || float.IsNaN(velocity.y) || float.IsNaN(velocity.z)) velocity = Vector3.zero;

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

        if (float.IsNaN(goal.x) || float.IsNaN(goal.y)) return;

        // Smooth camera movement.
        Vector3 next = Vector3.SmoothDamp(
            transform.position,
            goal,
            ref velocity,
            smoothTime
        );
        if (float.IsNaN(next.x) || float.IsNaN(next.y)) { velocity = Vector3.zero; return; }
        transform.position = next;
    }
}