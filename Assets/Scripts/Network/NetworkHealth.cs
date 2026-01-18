using UnityEngine;
using Fusion;

public class NetworkHealth : NetworkBehaviour
{
    public float maxHealth = 100f;
    public HealthBarUI healthBarUI;
    [Networked] public float CurrentHealth { get; set; }
    private ChangeDetector _changes;

    public override void Spawned()
    {
        _changes = GetChangeDetector(ChangeDetector.Source.SimulationState);
        if (HasStateAuthority) CurrentHealth = maxHealth;
        UpdateUI();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_TakeDamage(float amount)
    {
        CurrentHealth -= amount;
        
        Debug.Log($"[Health] Took {amount} damage. Current: {CurrentHealth}");

        if (CurrentHealth <= 0) 
        {
            CurrentHealth = 0;
            Die();
        }
    }

    void Die()
    {
        Debug.Log("💀 Died!");
    }

    public override void Render()
    {
        foreach (var change in _changes.DetectChanges(this, out var prev, out var curr)) {
            if (change == nameof(CurrentHealth)) UpdateUI();
        }
    }

    private void UpdateUI() { if (healthBarUI != null) healthBarUI.UpdateHealthBar(CurrentHealth, maxHealth); }
}