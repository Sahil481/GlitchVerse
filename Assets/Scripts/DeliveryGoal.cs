using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DeliveryGoal : MonoBehaviour
{
    bool done;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (done) return;
        if (other.GetComponentInParent<PlayerController>() == null) return;
        if (LevelManager.Instance == null) return;
        if (LevelManager.Instance.CurrentState != LevelManager.State.Playing) return;
        done = true;
        LevelManager.Instance.Complete();
    }
}
