using Sirenix.OdinInspector;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class EntityHealthHandler : MonoBehaviour
{

    public static int AlivePlayers;

    public bool IsPlayer;
    [ShowIf(nameof(IsPlayer))][SerializeField] private PlayerFiringSystem m_playerWeapon;
    [ShowIf(nameof(IsPlayer))][SerializeField] private PlayerMovementHandler m_playerMovement;
    [ShowIf(nameof(IsPlayer))][SerializeField] private PlayerAbility m_playerAbility;
    [ShowIf(nameof(IsPlayer))][SerializeField] private BoxCollider2D m_playerCollider;
    [ShowIf(nameof(IsPlayer))][SerializeField] private SpriteRenderer m_playerSprite;

    [SerializeField] private GameObject m_toMakeInvincible;
    [SerializeField][Min(1)] private int m_maxHealth = 1;
    [SerializeField] private AudioSource m_deathAudio;

    private int m_deathCount;
    private int m_health;
    private bool m_isInvincible = false;

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
                if (IsPlayer)
                {
                    m_toMakeInvincible.layer = LayerMask.NameToLayer("PlayerInvincible");
                    m_isInvincible = true;
                    Invoke(nameof(SetVulnerable), .5f);
                }
                if (m_health > 0)
                {
                    OnHealthChanged.Invoke();
                }
                else if (m_health ==0)
                {
                    if (IsPlayer) m_deathCount++;
                    OnDeath.Invoke();
                    m_deathAudio.PlayOneShot(m_deathAudio.clip);
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
        if (IsPlayer)
            OnDeath.AddListener(()=>StartCoroutine(PlayerDeath()));
    }
    private IEnumerator PlayerDeath()
    {
        AlivePlayers--;
        Debug.Log("PlayerDead, remaining:" + AlivePlayers);
        m_playerMovement.enabled = false;
        m_playerCollider.enabled = false;
        m_playerSprite.enabled = false;
        m_playerWeapon.enabled = false;
        m_playerAbility.enabled = false;
        if (AlivePlayers <= 0)
        {
            EndScreen.Instance.DisplayScores();
            yield return null;
        }
        else
        {
            int respawnWave = EnemySpawnManager.Instance.m_currentWave + m_deathCount;
            yield return new WaitWhile(() => EnemySpawnManager.Instance.m_currentWave < respawnWave);
            m_playerMovement.enabled = true;
            m_playerWeapon.enabled = true;
            m_playerCollider.enabled = true;
            m_playerSprite.enabled = true;
            m_playerAbility.enabled = true;
            Health = m_maxHealth;
            AlivePlayers++;
            Debug.Log("PlayerRevived, remaining:" + AlivePlayers);
        }

    }
}
