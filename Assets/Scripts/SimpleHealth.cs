using UnityEngine;
using System.Collections; // Required for IEnumerator

public class SimpleHealth : MonoBehaviour
{
    [Header("Stats")]
    public float maxHealth = 100f;
    private float currentHealth;

    [Header("References")]
    public HealthBarUI healthBar;
    private Animator animator;

    [Header("Dummy Settings")]
    [Tooltip("How long to stay 'dead' before reviving")]
    public float reviveDelay = 2.0f;

    void Start()
    {
        currentHealth = maxHealth;
        if (animator != null) animator = GetComponent<Animator>();
        UpdateUI();
    }

    public void TakeDamage(float amount)
    {
        // Prevent taking damage if already dead (waiting to revive)
        if (currentHealth <= 0) return;

        currentHealth -= amount;
        if (currentHealth < 0) currentHealth = 0;

        UpdateUI();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (gameObject.CompareTag("TrainingDummy"))
        {
            if (animator != null)
            {
                // 1. Play Death Animation
                animator.SetTrigger("isDie");
                
                // 2. Start timer to bring it back to life
                StartCoroutine(ReviveRoutine());
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private IEnumerator ReviveRoutine()
    {
        Destroy(gameObject);
        // Wait for the animation to finish or for the specified delay
        yield return new WaitForSeconds(reviveDelay);

        // 3. Reset Stats
        currentHealth = maxHealth;
        UpdateUI();

        // 4. Reset Animation Logic
        if (animator != null)
        {
            // Unset the 'isDie' trigger so it doesn't get stuck
            animator.ResetTrigger("isDie");

            // Option A: Force the animation back to "Idle" instantly
            // (Use the exact name of your Idle state, e.g., "Idle", "Stand", "Entry")
            animator.Play("pushed"); 

            // Option B: If you have a 'Revive' trigger in your Animator, use this:
            // animator.SetTrigger("Revive");
        }
    }

    private void UpdateUI()
    {
        if (healthBar != null)
        {
            healthBar.UpdateHealthBar(currentHealth, maxHealth);
        }
    }
}