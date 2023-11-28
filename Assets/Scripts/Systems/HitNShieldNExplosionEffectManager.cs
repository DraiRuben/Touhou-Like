using UnityEngine;

public class HitNShieldNExplosionEffectManager : MonoBehaviour
{
    public static HitNShieldNExplosionEffectManager Instance;
    [SerializeField] private ParticleSystem m_hitParticleSystem;
    [SerializeField] private ParticleSystem m_shieldParticleSystem;
    [SerializeField] private ParticleSystem m_explosionParticleSystem;
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void DisplayEffect(Vector2 _worldPos, EffectType _effectType)
    {
        ParticleSystem.Particle[] newParticle = new ParticleSystem.Particle[1];
        newParticle[0].position = _worldPos;
        newParticle[0].startSize = 1;
        if (_effectType == EffectType.Hit)
        {
            newParticle[0].startLifetime = 0.5f;
            newParticle[0].remainingLifetime = 0.5f;
            m_hitParticleSystem.SetParticles(newParticle, 1, m_hitParticleSystem.particleCount);
            if(m_hitParticleSystem.isStopped)m_hitParticleSystem.Play();
        }
        else if(_effectType != EffectType.Shield)
        {
            newParticle[0].startLifetime = 0.9f;
            newParticle[0].remainingLifetime = 0.9f;
            m_shieldParticleSystem.SetParticles(newParticle, 1, m_shieldParticleSystem.particleCount);
            if(m_shieldParticleSystem.isStopped) m_shieldParticleSystem.Play();
        }
        else
        {
            newParticle[0].startLifetime = 0.5f;
            newParticle[0].remainingLifetime = 0.5f;
            m_explosionParticleSystem.SetParticles(newParticle,1, m_explosionParticleSystem.particleCount);
            if(m_explosionParticleSystem.isStopped) m_explosionParticleSystem.Play();
        }
    }
    public enum EffectType
    {
        Hit,
        Shield,
        Explosion
    }
}
