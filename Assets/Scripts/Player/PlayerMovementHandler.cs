using System;
using UnityEngine;

public class PlayerMovementHandler : MonoBehaviour
{
    [SerializeField] private float m_movementSpeed;
    [SerializeField] private AnimationCurve m_velocityOverTime;
    [SerializeField] private AudioSource m_hurtAudio;
    [SerializeField] private AudioSource m_healAudio;
    [SerializeField] private AudioSource m_upgradeAudio;
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
    private void OnDisable()
    {
        m_rb.velocity = Vector2.zero;
        m_timeSinceInput = 0;
    }
    private void OnParticleCollision(GameObject other)
    {
        m_healthHandler.Health--;
        m_hurtAudio.PlayOneShot(m_hurtAudio.clip);
        GFXManager.Instance.DisplayEffect(transform.position, GFXManager.EffectType.Hit);
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        //collided with upgrade
        if (collision.gameObject.CompareTag("Upgrade"))
        {
            Upgrade upgrade = collision.gameObject.GetComponent<Upgrade>();
            Upgrade.UpgradeType type = upgrade.GetRandomUpgrade();
            //could make this shorter but eh~
            switch (type)
            {
                case Upgrade.UpgradeType.Health:
                    m_healthHandler.Health += 1;
                    m_healAudio.PlayOneShot(m_healAudio.clip);
                    break;
                case Upgrade.UpgradeType.FireRate:
                    m_firingSystem.FireRate *=1.1f;
                    m_firingSystem.UpdateFireParameters();
                    m_upgradeAudio.PlayOneShot(m_upgradeAudio.clip);
                    break;
                case Upgrade.UpgradeType.Spread:
                    m_firingSystem.FireSpreadAngle *=1.25f;
                    m_firingSystem.UpdateFireParameters();
                    m_upgradeAudio.PlayOneShot(m_upgradeAudio.clip);
                    break;
                case Upgrade.UpgradeType.BulletCount:
                    m_firingSystem.BulletCount += 1;
                    m_firingSystem.FireSpreadAngle += 3f;
                    m_firingSystem.UpdateFireParameters();
                    m_upgradeAudio.PlayOneShot(m_upgradeAudio.clip);
                    break;
                case Upgrade.UpgradeType.Rebound:
                    m_firingSystem.BulletRebounds += 1;
                    m_firingSystem.UpdateFireParameters();
                    m_upgradeAudio.PlayOneShot(m_upgradeAudio.clip);
                    break;
                case Upgrade.UpgradeType.BulletSize:
                    m_firingSystem.BulletSize *=1.1f;
                    m_firingSystem.UpdateFireParameters();
                    m_upgradeAudio.PlayOneShot(m_upgradeAudio.clip);
                    break;
                case Upgrade.UpgradeType.BulletSpeed:
                    m_firingSystem.BulletSpeed *=1.1f;
                    m_firingSystem.UpdateFireParameters();
                    m_upgradeAudio.PlayOneShot(m_upgradeAudio.clip);
                    break;
            }
            Destroy(collision.gameObject);
        }
    }
}
