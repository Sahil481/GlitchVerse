using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class TutorialTrigger : MonoBehaviour
{
    public string title;
    [TextArea(2, 5)] public string body;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerController>() == null) return;
        if (LevelManager.Instance == null || LevelManager.Instance.Hud == null) return;
        LevelManager.Instance.Hud.ShowTip(this, title, body);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerController>() == null) return;
        if (LevelManager.Instance == null || LevelManager.Instance.Hud == null) return;
        LevelManager.Instance.Hud.HideTip(this);
    }
}
