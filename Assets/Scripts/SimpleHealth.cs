using UnityEngine;

public class SimpleHealth : MonoBehaviour
{
    [Header("Stats")]
    public float maxHealth = 100f;
    private float currentHealth;

    [Header("References")]
    // Drag the GameObject that has the HealthBarUI script here
    public HealthBarUI healthBar;

    void Start()
    {
        currentHealth = maxHealth;
        UpdateUI();
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;

        if (currentHealth < 0) currentHealth = 0;

        UpdateUI();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void UpdateUI()
    {
        if (healthBar != null)
        {
            healthBar.UpdateHealthBar(currentHealth, maxHealth);
        }
    }

    private void Die()
    {
        Destroy(gameObject);
    }
}