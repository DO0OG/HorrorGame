using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Monster_Health : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float currentHealth;
    internal float maxHealth;

    [Header("Bools")]
    public bool isDead = false;
    public bool ableToKill = false;

    // Update is called once per frame
    void Update()
    {
        CheckHealth();
    }

    public void CheckHealth()
    {
        if (currentHealth <= 0 && ableToKill)
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

        // 총기로 죽일 수 없는 몬스터는 데미지 대신 일시적으로 경직됨
        if (!ableToKill)
        {
            Monster_Controller controller = GetComponent<Monster_Controller>();
            if (controller != null)
                controller.Stun(GameManager.Data.stunDuration);
            return;
        }

        currentHealth -= damage;

        Debug.Log(currentHealth);
    }
}
