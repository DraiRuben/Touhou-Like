using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EntityHealthHandler : MonoBehaviour
{
    private int m_health;
    private bool m_isInvincible = false;
    [SerializeField] [Min(1)] private int m_maxHealth = 1;
    [NonSerialized] public UnityEvent OnHealthChanged = new();
    [NonSerialized] public UnityEvent OnMaxHealthChanged = new();
    public int Health {  get { return m_health; } 
        set 
        {
            if (!m_isInvincible) 
            { 
                m_health = value; 
                if (gameObject.layer == LayerMask.NameToLayer("Player"))
                {
                    gameObject.layer = LayerMask.NameToLayer("PlayerInvincible");
                    Invoke(nameof(SetVulnerable), 1f);
                }
                OnHealthChanged.Invoke();
            }
        } 
    }
    private void SetVulnerable()
    {
        gameObject.layer = LayerMask.NameToLayer("Player");
    }
    public int MaxHealth {  get { return m_health; } set { m_maxHealth = value; m_health = value; OnMaxHealthChanged.Invoke(); } }

    private void Awake()
    {
        m_health = m_maxHealth;
    }
   
}
