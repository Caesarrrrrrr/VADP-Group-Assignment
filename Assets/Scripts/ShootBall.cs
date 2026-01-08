using UnityEngine;

public class ShootBall : MonoBehaviour
{
    [Header("Physics Controls")]
    [Tooltip("How strong the ball shoots (Speed)")]
    public float shootForce = 20f;

    [Tooltip("How long the ball exists before disappearing (Range)")]
    public float ballLifeTime = 3f;

    public void ShootingBall(GameObject prefabToSpawn, Vector3 spawnPosition, Quaternion aimDirection)
    {
        // Use the passed 'prefabToSpawn' instead of the default variable
        GameObject ball = Instantiate(prefabToSpawn, spawnPosition, aimDirection);

        Rigidbody rb = ball.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero; // Unity 6 syntax
            rb.AddForce(ball.transform.forward * shootForce, ForceMode.Impulse);
        }

        Destroy(ball, ballLifeTime);
    }
}
