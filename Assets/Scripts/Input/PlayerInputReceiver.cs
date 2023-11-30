using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerMovementHandler))]
public class PlayerInputReceiver : MonoBehaviour
{
    private PlayerMovementHandler m_movementHandler;
    public int Score { get { return m_score; } set { m_score = value; OnScoreChange.Invoke(); } }
    private int m_score;
    [NonSerialized] public UnityEvent OnScoreChange = new();
 
    private bool CanUseShield = true;
    public float ShieldCooldown;
    [NonSerialized] public UnityEvent OnShieldUse = new();

    private bool CanUseAbility = true;
    public float AbilityCooldown;
    [NonSerialized] public UnityEvent OnAbilityUse = new();

    private void Awake()
    {
        m_movementHandler = GetComponent<PlayerMovementHandler>();
    }
    public void MovementInput(InputAction.CallbackContext ctx)
    {
        m_movementHandler.MovementInput = ctx.ReadValue<Vector2>();
    }

    public void Ability(InputAction.CallbackContext ctx)
    {
        if (ctx.performed && CanUseAbility)
        {
            CanUseAbility = false;
            OnAbilityUse.Invoke();
            StartCoroutine(RefreshAbility());
        }
    }
    private IEnumerator RefreshAbility()
    {
        yield return new WaitForSeconds(AbilityCooldown);
        CanUseAbility = true;
    }
    public void Shield(InputAction.CallbackContext ctx)
    {
        if (ctx.performed && CanUseShield)
        {
            CanUseShield = false;
            OnShieldUse.Invoke();
            HitNShieldNExplosionEffectManager.Instance.DisplayEffect(transform.position, HitNShieldNExplosionEffectManager.EffectType.Shield);
            StartCoroutine(RefreshShield());
        }
    }
    private IEnumerator RefreshShield()
    {
        yield return new WaitForSeconds(ShieldCooldown);
        CanUseShield = true;
    }
    public void Pause(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {

        }
    }
}
