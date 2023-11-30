using System;
using UnityEngine;
using UnityEngine.Events;

public class EntityHealthHandler : MonoBehaviour
{
    private int m_health;
    private bool m_isInvincible = false;

    public bool m_isPlayer;
    public int DeathCount;

    [SerializeField] private GameObject m_toMakeInvincible;
    [SerializeField][Min(1)] private int m_maxHealth = 1;
    [NonSerialized] public UnityEvent OnHealthChanged = new();
    [NonSerialized] public UnityEvent OnDeath = new();
    [NonSerialized] public UnityEvent OnMaxHealthChanged = new();
    public int Health
    {
        get { return m_health; }
        set
        {
            if(value > m_maxHealth)
            {
                m_maxHealth = value;
                m_health = value;
                OnHealthChanged.Invoke();
                return;
            }
            if (!m_isInvincible)
            {
                m_health = value;
                if (m_isPlayer)
                {
                    m_toMakeInvincible.layer = LayerMask.NameToLayer("PlayerInvincible");
                    m_isInvincible = true;
                    Invoke(nameof(SetVulnerable), .5f);
                }
                if (m_health > 0)
                {
                    OnHealthChanged.Invoke();
                }
                else
                {
                    if (m_isPlayer) DeathCount++;
                    OnDeath.Invoke();
                }
            }
        }
    }
    private void SetVulnerable()
    {
        m_toMakeInvincible.layer = LayerMask.NameToLayer("Player");
        m_isInvincible = false;
    }
    public int MaxHealth { get { return m_maxHealth; } 
        set 
        { 
            m_maxHealth = value; 
            m_health = value; 
            OnMaxHealthChanged.Invoke(); 
        } 
    }

    private void Awake()
    {
        m_health = m_maxHealth;
    }

}
