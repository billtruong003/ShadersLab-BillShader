using BrushHit;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PlayerCollisionHandler : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        // Kiểm tra xem có va chạm với layer Obstacle không
        if (collision.gameObject.layer == LayerMask.NameToLayer("Obstacle"))
        {
            GameManager.Instance?.TriggerGameOver();
        }
    }
}