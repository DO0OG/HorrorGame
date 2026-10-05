using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_Health : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float currentHealth;
    [SerializeField] private float maxHealth;
    [SerializeField] private float restoreTimer = 0f;

    [Header("Bools")]
    [SerializeField] private bool isHit = false;
    [SerializeField] private bool isDead = false;
    [SerializeField] private bool isRestore = false;

    [Header("Blood")]
    [SerializeField] private CanvasGroup bloodCG;

    public bool IsDead => isDead;

    // Start is called before the first frame update
    void Start()
    {
        // 난이도에 따라 최대 체력 결정 (Normal : 100)
        SetMaxHealth(GameManager.Data.playerMaxHealth);

        isDead = false;
        bloodCG.alpha = 0;
    }

    // Update is called once per frame
    void Update()
    {
        RestoreTimer();

        if (!isHit && !isDead && currentHealth <= maxHealth)
        {
            RestoreHealth();
        }
    }

    private void SetMaxHealth(float amount)
    {
        maxHealth = amount;
        currentHealth = maxHealth;
    }

    private void RestoreTimer()
    {
        if (restoreTimer <= 5f && currentHealth != maxHealth)
        {
            restoreTimer += Time.deltaTime;
        }

        if (restoreTimer >= 3f)
        {
            isRestore = true;
        }
        else
        {
            isRestore = false;
        }
    }

    private void RestoreHealth()
    {
        if (isRestore)
        {
            currentHealth += 5 * Time.deltaTime;
            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

            if (currentHealth < maxHealth)
            {
                bloodCG.alpha -= 0.1f * Time.deltaTime;
                bloodCG.alpha = Mathf.Clamp(bloodCG.alpha, 0f, 1f);
            }
        }
        else
        {
            return;
        }
    }

    public void DecreaseHealth(float amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        restoreTimer = 0f;
        isRestore = false;

        bloodCG.alpha += (amount / maxHealth);
        bloodCG.alpha = Mathf.Clamp(bloodCG.alpha, 0f, 1f);

        // 체력이 모두 닳으면 사망 → 몬스터가 처형을 진행함
        if (currentHealth <= 0f)
            isDead = true;
    }

    private void OnCollisionEnter(Collision collision)
    {
    }
}
