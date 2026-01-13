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

    public void TakeDamage(float amount)
    {
        if (!HasStateAuthority) return;
        CurrentHealth -= amount;
        if (CurrentHealth <= 0) {
            CurrentHealth = 0;
            Debug.Log("💀 Died!");
            // DO NOT DESTROY OBJECT HERE or Hands will break. 
            // Just play animation or disable mesh.
        }
    }

    public override void Render()
    {
        foreach (var change in _changes.DetectChanges(this, out var prev, out var curr)) {
            if (change == nameof(CurrentHealth)) UpdateUI();
        }
    }

    private void UpdateUI() { if (healthBarUI != null) healthBarUI.UpdateHealthBar(CurrentHealth, maxHealth); }
}