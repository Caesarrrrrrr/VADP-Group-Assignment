using UnityEngine;
using Fusion;

public class NetworkHealth : NetworkBehaviour
{
    [Header("Settings")]
    public float maxHealth = 100f;

    [Header("References")]
    public HealthBarUI healthBarUI;

    [Networked]
    public float CurrentHealth { get; set; }

    private ChangeDetector _changes;

    public override void Spawned()
    {
        _changes = GetChangeDetector(ChangeDetector.Source.SimulationState);

        if (HasStateAuthority)
        {
            CurrentHealth = maxHealth;
        }
        UpdateUI();
    }

    public void TakeDamage(float amount)
    {
        if (!HasStateAuthority) return;
        CurrentHealth -= amount;
        if (CurrentHealth < 0) CurrentHealth = 0;
    }

    public override void Render()
    {
        // FIX IS HERE: 
        // 1. Pass 'this' as the first argument.
        // 2. Accept two out variables (previousBuffer, currentBuffer) even if you don't use them.
        foreach (var change in _changes.DetectChanges(this, out var previous, out var current))
        {
            if (change == nameof(CurrentHealth))
            {
                UpdateUI();
            }
        }
    }

    private void UpdateUI()
    {
        if (healthBarUI != null)
        {
            healthBarUI.UpdateHealthBar(CurrentHealth, maxHealth);
        }
    }
}