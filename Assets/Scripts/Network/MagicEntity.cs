using UnityEngine;
using Fusion;
using System.Collections.Generic;

public class MagicEntity : NetworkBehaviour
{
    public enum MagicType { Projectile, Shield, AreaEffect }

    [Header("Config")]
    public MagicType type = MagicType.Projectile;
    public float lifeTime = 5f;
    public LayerMask hitMask; 

    [Header("Projectile")]
    public float speed = 12f;
    public GameObject impactParticle; 

    [Header("Shield/Area")]
    public int maxHealth = 100;
    [Networked] public int CurrentHealth { get; set; }
    public float areaRadius = 2.0f;
    public float damageInterval = 0.5f;
    public int damageAmount = 10;

    [Networked] private TickTimer lifeTimer { get; set; }
    [Networked] private TickTimer damageTickTimer { get; set; }

    public override void Spawned()
    {
        lifeTimer = TickTimer.CreateFromSeconds(Runner, lifeTime);
        damageTickTimer = TickTimer.CreateFromSeconds(Runner, damageInterval);
        CurrentHealth = maxHealth;
    }

    public override void FixedUpdateNetwork()
    {
        if (lifeTimer.Expired(Runner)) { Runner.Despawn(Object); return; }

        switch (type) {
            case MagicType.Projectile: UpdateProjectile(); break;
            case MagicType.Shield: if (CurrentHealth <= 0) Runner.Despawn(Object); break;
            case MagicType.AreaEffect: UpdateAreaEffect(); break;
        }
    }

    void UpdateProjectile()
    {
        float moveDist = speed * Runner.DeltaTime;
        Vector3 dir = transform.forward;

        if (Runner.GetPhysicsScene().Raycast(transform.position, dir, out var hit, moveDist, hitMask))
        {
            // 1. Check for Player Hit
            if (hit.collider.TryGetComponent<NetworkHealth>(out var targetHealth))
            {
                bool isDummy = hit.collider.gameObject.layer == LayerMask.NameToLayer("Dummy");

                // SAFETY: If I hit MYSELF, keep moving (Don't stuck, Don't despawn)
                if (!isDummy && targetHealth.Object.InputAuthority == Object.InputAuthority) {
                    transform.position += dir * moveDist; 
                    return; 
                }
                
                // If it's an ENEMY or Dummy, take damage
                targetHealth.TakeDamage(damageAmount);
            }
            // 2. Check for Shield Hit
            else if (hit.collider.TryGetComponent<MagicEntity>(out var shield)) {
                shield.TakeDamage(damageAmount);
            }

            // 3. HIT WALL / GROUND / ENEMY -> DESPAWN NOW
            // This ensures it never gets "stuck" on a wall or visual mesh
            if (impactParticle != null && Runner.IsForward) {
                Quaternion rot = (hit.normal != Vector3.zero) ? Quaternion.LookRotation(hit.normal) : transform.rotation;
                GameObject vfx = Instantiate(impactParticle, hit.point, rot);
                Destroy(vfx, 2.0f);
            }
            
            Runner.Despawn(Object);
        }
        else 
        { 
            // Nothing hit, move forward
            transform.position += dir * moveDist; 
        }
    }

    void UpdateAreaEffect()
    {
        if (damageTickTimer.Expired(Runner)) {
            damageTickTimer = TickTimer.CreateFromSeconds(Runner, damageInterval);
            List<LagCompensatedHit> hits = new List<LagCompensatedHit>();
            Runner.LagCompensation.OverlapSphere(transform.position, areaRadius, Object.InputAuthority, hits, hitMask);

            foreach (var hit in hits) {
                if (hit.GameObject == gameObject) continue; 
                if (hit.GameObject.TryGetComponent<MagicEntity>(out var entity)) entity.TakeDamage(damageAmount);
                else if (hit.GameObject.TryGetComponent<NetworkHealth>(out var hp)) hp.TakeDamage(damageAmount);
            }
        }
    }

    public void TakeDamage(int amount) {
        if ((type == MagicType.Shield || type == MagicType.AreaEffect) && HasStateAuthority) CurrentHealth -= amount;
    }
}