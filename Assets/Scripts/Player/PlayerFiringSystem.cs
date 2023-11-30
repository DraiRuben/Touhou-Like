using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerFiringSystem : MonoBehaviour
{
    public bool manuallyUpdate;
    [SerializeField] private ParticleSystem m_firingSystem;
    public float FireRate;
    public float FireSpreadAngle;
    public float BulletCount;
    public float BulletSize;
    public float BulletSpeed;
    public int BulletRebounds;
    //TODO: make this crap work
    public bool ExplodeOnImpact;
    public float ExplosionRadius;
    private void Awake()
    {
        UpdateFireParameters();
    }
    private void Update()
    {
        if (manuallyUpdate)
        {
            UpdateFireParameters();
            manuallyUpdate = false;
        }
    }
    private void OnDisable()
    {
        m_firingSystem.Stop();
    }
    private void OnEnable()
    {
        if(m_firingSystem.isStopped)
            m_firingSystem.Play();
    }
    public void UpdateFireParameters()
    {
        var EmissionModule = m_firingSystem.emission;
        ParticleSystem.Burst ShootBurst = new()
        {
            repeatInterval = 1f / FireRate,
            cycleCount = 0
        };

        var CountCurve = ShootBurst.count;
        CountCurve.constant = BulletCount;
        ShootBurst.count = CountCurve;

        EmissionModule.SetBurst(0, ShootBurst);

        var MainModule = m_firingSystem.main;
        MainModule.startSize = BulletSize;
        MainModule.startSpeed = BulletSpeed;

        var CollisionModule = m_firingSystem.collision;
        CollisionModule.lifetimeLoss = 1 / (BulletRebounds + 1);

        var ShapeModule = m_firingSystem.shape;
        ShapeModule.arc = FireSpreadAngle;
        ShapeModule.rotation = new Vector3(0, 0, 90 - FireSpreadAngle / 2);
    }
}
