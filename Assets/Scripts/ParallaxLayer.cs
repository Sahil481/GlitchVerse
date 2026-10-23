using System.Collections;
using UnityEngine;

public class ParallaxLayer : MonoBehaviour
{
    [Tooltip("1 = moves with the camera (sky). Lower = closer layers that slide past faster.")]
    [Range(0f, 1f)] public float followX = 0.9f;
    [Range(0f, 1f)] public float followY = 0.9f;
    [Tooltip("Width of one repeat of the artwork in world units. 0 = no tiling.")]
    public float period = 24f;
    public Vector2 offset;

    Camera cam;
    float startCamY;
    bool ready;

    void LateUpdate()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        Vector3 c = cam.transform.position;
        if (!ready) { startCamY = c.y; ready = true; }

        float x = c.x;
        if (period > 0.01f) x = c.x - Mathf.Repeat(c.x * (1f - followX), period);
        else x = c.x * followX;

        float y = Mathf.Lerp(startCamY, c.y, followY) + offset.y;
        transform.position = new Vector3(x + offset.x, y, transform.position.z);
    }
}
