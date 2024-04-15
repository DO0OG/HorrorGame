using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Monster_Health : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] public float maxHealth = 100f;
    [SerializeField] private float currentHealth;

    [Header("Bools")]
    public static bool isDead = false;

    // Start is called before the first frame update
    void OnEnable()
    {
        SetHealth(maxHealth);
    }

    // Update is called once per frame
    void Update()
    {
        CheckHealth();
    }

    public void CheckHealth()
    {
        if (currentHealth <= 0)
        {
            isDead = true;
            gameObject.SetActive(false);
        }
        else
        {
            isDead = false;
        }
    }

    public void SetHealth(float maxHP)
    {
        maxHealth = maxHP;
        currentHealth = maxHP;
    }

    public void TakeDamage(float damage)
    {
        if (isDead)
        {
            return;
        }

        currentHealth -= damage;

        Debug.Log(currentHealth);
    }
}
