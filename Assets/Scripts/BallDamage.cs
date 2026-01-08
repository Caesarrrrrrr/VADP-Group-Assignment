using UnityEngine;

public class BallDamage : MonoBehaviour
{
    [Header("Settings")]
    public float damageAmount = 20f; // How much health to remove

    private void OnCollisionEnter(Collision collision)
    {
        // 1. Try to find the SimpleHealth script on the object we hit
        SimpleHealth enemyHealth = collision.gameObject.GetComponent<SimpleHealth>();

        // 2. If the object has health, damage it
        if (enemyHealth != null)
        {
            enemyHealth.TakeDamage(damageAmount);
        }

        // 3. Destroy the ball immediately after hitting something solid
        Destroy(gameObject);
    }
}