using System;
using UnityEngine;

public class PlayerMovementHandler : MonoBehaviour
{
    [SerializeField] private float m_movementSpeed;
    [SerializeField] private AnimationCurve m_velocityOverTime;
    [NonSerialized] public Vector2 MovementInput;
    private float m_timeSinceInput;

    private Rigidbody2D m_rb;
    private EntityHealthHandler m_healthHandler;
    private PlayerFiringSystem m_firingSystem;
    private void Awake()
    {
        m_rb = GetComponent<Rigidbody2D>();
        m_firingSystem = GetComponent<PlayerFiringSystem>();
        m_healthHandler = GetComponent<EntityHealthHandler>();
    }
    private void FixedUpdate()
    {
        if (MovementInput.magnitude != 0)
        {
            m_rb.velocity = m_movementSpeed * m_velocityOverTime.Evaluate(m_timeSinceInput) * MovementInput.normalized;
            m_timeSinceInput += Time.deltaTime;
        }
        else
        {
            m_timeSinceInput = 0;
        }
    }
    private void OnParticleCollision(GameObject other)
    {
        m_healthHandler.Health--;
        HitNShieldNExplosionEffectManager.Instance.DisplayEffect(transform.position, HitNShieldNExplosionEffectManager.EffectType.Hit);
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        //collided with upgrade
        if (collision.gameObject.CompareTag("Upgrade"))
        {
            Upgrade upgrade = collision.gameObject.GetComponent<Upgrade>();
            switch (upgrade.UpgradeInfo)
            {
                case Upgrade.UpgradeType.Health:
                    m_healthHandler.Health += upgrade.IntValue;
                    break;
                case Upgrade.UpgradeType.FireRate:
                    m_firingSystem.FireRate += upgrade.FloatValue;
                    m_firingSystem.UpdateFireParameters();
                    break;
                case Upgrade.UpgradeType.Spread:
                    m_firingSystem.FireSpreadAngle += upgrade.FloatValue;
                    m_firingSystem.UpdateFireParameters();
                    break;
                case Upgrade.UpgradeType.BulletCount:
                    m_firingSystem.BulletCount += upgrade.IntValue;
                    m_firingSystem.UpdateFireParameters();
                    break;
                case Upgrade.UpgradeType.Rebound:
                    m_firingSystem.BulletRebounds += upgrade.IntValue;
                    m_firingSystem.UpdateFireParameters();
                    break;
                case Upgrade.UpgradeType.BulletSize:
                    m_firingSystem.BulletSize += upgrade.FloatValue;
                    m_firingSystem.UpdateFireParameters();
                    break;
                case Upgrade.UpgradeType.BulletSpeed:
                    m_firingSystem.BulletSpeed += upgrade.FloatValue;
                    m_firingSystem.UpdateFireParameters();
                    break;
            }
        }
    }
}
