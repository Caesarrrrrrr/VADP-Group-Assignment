using UnityEngine;
using Fusion;

// ATTACH THIS TO YOUR FIREBALL PREFAB
public class MagicProjectile : NetworkBehaviour
{
    [Header("Settings")]
    public float speed = 12f;
    public float lifeTime = 4f;
    public int damage = 10;
    public GameObject impactParticle; // Optional visual

    [Networked] private TickTimer lifeTimer { get; set; }

    public override void Spawned()
    {
        // Start the countdown immediately
        lifeTimer = TickTimer.CreateFromSeconds(Runner, lifeTime);
    }

    public override void FixedUpdateNetwork()
    {
        // 1. Check Lifetime
        if (lifeTimer.Expired(Runner))
        {
            Runner.Despawn(Object);
            return;
        }

        // 2. Calculate Movement
        float distance = speed * Runner.DeltaTime;
        Vector3 direction = transform.forward;

        // 3. Raycast BEFORE moving (Prevents going through walls)
        if (Runner.GetPhysicsScene().Raycast(transform.position, direction, out var hit, distance))
        {
            // We Hit Something!
            Debug.Log($"Magic Hit: {hit.collider.name}");
            
            // Optional: Spawn explosion logic here
            // if (impactParticle != null) Instantiate(impactParticle, hit.point, Quaternion.LookRotation(hit.normal));

            Runner.Despawn(Object); // Destroy bullet
        }
        else
        {
            // Path Clear -> Move
            transform.position += direction * distance;
        }
    }
}