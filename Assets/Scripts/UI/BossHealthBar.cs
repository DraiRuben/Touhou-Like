using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BossHealthBar : MonoBehaviour
{
    private EntityHealthHandler m_bossHealth;
    private Image m_healthImage;
    public static BossHealthBar Instance;
    private Animator m_animator;
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        m_animator = GetComponent<Animator>();
        m_healthImage = GetComponent<Image>();
    }
    public void StartDisplay()
    {
        StartCoroutine(DisplayLater());
    }
    private IEnumerator DisplayLater()
    {
        yield return null;
        yield return null;
        m_bossHealth = EnemySpawnManager.Instance.GetClosestEnemy(Vector2.zero).GetComponent<EntityHealthHandler>();
        m_bossHealth.OnHealthChanged.AddListener(UpdateHealthDisplay);
        m_bossHealth.OnDeath.AddListener(UpdateHealthDisplay);
        m_bossHealth.OnDeath.AddListener(() => StartCoroutine(HealthDisplayStop()));
        m_animator.SetTrigger("ChangeState");
    }
    private void UpdateHealthDisplay()
    {
        m_healthImage.fillAmount = (float)m_bossHealth.Health/m_bossHealth.MaxHealth;
    }
    private IEnumerator HealthDisplayStop()
    {
        m_animator.SetTrigger("ChangeState");
        yield return new WaitForSeconds(.7f);
    }
}
