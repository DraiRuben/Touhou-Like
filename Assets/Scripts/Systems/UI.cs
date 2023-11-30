using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI : MonoBehaviour
{
    [SerializeField] private Animator m_borderAnimator;
    [SerializeField] private TextMeshProUGUI m_scoreText;
    [SerializeField] private Image m_ability;
    [SerializeField] private Image m_shield;
    [SerializeField] private Image m_health;
    [SerializeField] private Image m_player;
    [SerializeField] private TextMeshProUGUI m_healthCount;
    private PlayerInputReceiver m_playerInput;
    private EntityHealthHandler m_healthHandler;

    private void Start()
    {
        m_borderAnimator.SetInteger("PlayerNumber", PlayerManager.Instance.m_players.Count);
        m_scoreText.text = "0";
        m_ability.enabled = true;
        m_shield.enabled = true;
        m_playerInput = PlayerManager.Instance.m_players[PlayerManager.Instance.m_players.Count - 1].GetComponent<PlayerInputReceiver>();
        m_player.sprite = PlayerManager.Instance.m_playerSprites[PlayerManager.Instance.m_players.Count - 1];
        
        //need to subscribe to on score changed event
        m_healthHandler = m_playerInput.GetComponent<EntityHealthHandler>();

        m_playerInput.OnAbilityUse.AddListener(() => StartCoroutine(RefreshAbility()));
        m_playerInput.OnShieldUse.AddListener(() => StartCoroutine(RefreshShield()));
        m_playerInput.OnScoreChange.AddListener (()=> m_scoreText.text = m_playerInput.Score.ToString());
        m_healthHandler.OnHealthChanged.AddListener(UpdateHealthDisplay);

        EndScreen.Instance.Scores.Add(m_scoreText.gameObject);
        UpdateHealthDisplay();
    }
    private IEnumerator RefreshAbility()
    {
        m_ability.enabled = false;
        yield return new WaitForSeconds(m_playerInput.AbilityCooldown);
        m_ability.enabled = true;
    }
    private IEnumerator RefreshShield()
    {
        m_shield.enabled = false;
        yield return new WaitForSeconds(m_playerInput.ShieldCooldown);
        m_shield.enabled = true;
    }
    private void UpdateHealthDisplay()
    {
        m_health.fillAmount = (float)m_healthHandler.Health / m_healthHandler.MaxHealth;
        m_healthCount.text = $"{m_healthHandler.Health}/{m_healthHandler.MaxHealth}";
    }
}
