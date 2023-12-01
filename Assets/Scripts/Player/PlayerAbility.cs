using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static EnemyProjectileSpawner;
using static UnityEngine.ParticleSystem;

public class PlayerAbility : MonoBehaviour
{
    [SerializeField] private EnemyProjectileSpawner.ShootZone Pattern;
    private PlayerInputReceiver m_inputHandler;
    private bool m_stopEmission;
    private Transform m_transform;
    private void Awake()
    {
        m_inputHandler = GetComponent<PlayerInputReceiver>();
        m_transform = transform;
        m_inputHandler.OnAbilityUse.AddListener(UseAbility);
    }
    private void OnDisable()
    {
        m_stopEmission = true;
    }
    private void OnEnable()
    {
        m_stopEmission = false;
    }
    private void UseAbility()
    {
        switch(Pattern.patternType)
        {
            case ShootZone.PatternType.Circle:
                {
                    List<ParticleSystem> _toUse = new();
                    for (int i = 0; i < Pattern.ZoneCount; i++)
                    {
                        _toUse.Add(ProjectilePool.Instance.GetEmitter().GetComponent<ParticleSystem>());
                    }

                    for (int i = 0; i < Pattern.ZoneCount; i++)
                    {
                        ParticleSystem system = _toUse[i];
                        system.transform.rotation = Quaternion.Euler(0, 0, 360f - 360f / (i + 1));
                        EnemyFiringSystem.SetupParticleSystemParameters(system, Pattern);
                        system.transform.localScale = new(system.transform.localScale.x, system.transform.localScale.y,m_inputHandler.PlayerIndex);
                        
                        //Shape Module
                        ShapeModule ShapeModule = system.shape;
                        ShapeModule.enabled = true;
                        ShapeModule.shapeType = ParticleSystemShapeType.Cone;
                        ShapeModule.radius = 0.01f;
                        ShapeModule.arc = Mathf.Abs(Pattern.EndAngle - Pattern.StartAngle);
                        ShapeModule.arcMode = Pattern.RandomPosition ? ParticleSystemShapeMultiModeValue.Random : ParticleSystemShapeMultiModeValue.BurstSpread;
                        system.transform.SetPositionAndRotation(transform.position, Quaternion.Euler(0, 0, i * (-360f / Pattern.ZoneCount)));

                        //Burst Emission
                        Burst[] burst = new Burst[1];
                        MinMaxCurve countCurve = burst[0].count;
                        countCurve.constant = Pattern.ProjectileCount;
                        burst[0].count = countCurve;
                        burst[0].repeatInterval = Pattern.SpawnFrequency;
                        burst[0].cycleCount = 0;
                        system.emission.SetBursts(burst);

                        //Emission Module
                        EmissionModule EmissionModule = system.emission;
                        EmissionModule.rateOverDistance = 0;
                        EmissionModule.rateOverTime = 0;

                        //Collision Module
                        CollisionModule CollisionModule = system.collision;
                        CollisionModule.collidesWith = LayerMask.GetMask("Enemies");
                        //Render module
                        ParticleSystemRenderer particleSystemRenderer = system.GetComponent<ParticleSystemRenderer>();
                        particleSystemRenderer.alignment = ParticleSystemRenderSpace.Velocity;
                        particleSystemRenderer.renderMode = ParticleSystemRenderMode.Billboard;
                        particleSystemRenderer.sortMode = ParticleSystemSortMode.OldestInFront;

                        StartCoroutine(EmissionRoutine(_toUse[i], Pattern, Pattern.BehaviourExitValue));
                    }
                    break;
                }
            case ShootZone.PatternType.Polygon:
                {
                    ParticleSystem system = ProjectilePool.Instance.GetEmitter().GetComponent<ParticleSystem>();
                    system.transform.localScale = new(system.transform.localScale.x, system.transform.localScale.y, m_inputHandler.PlayerIndex);

                    EnemyFiringSystem.SetupParticleSystemParameters(system, Pattern);
                    Particle[] Particles = EnemyFiringSystem.ComputePolygon(Pattern);

                    ShapeModule ShapeModule = system.shape;
                    ShapeModule.enabled = false;

                    MainModule MainModule = system.main;
                    MainModule.loop = false;
                    MainModule.playOnAwake = false;

                    EmissionModule EmissionModule = system.emission;
                    EmissionModule.enabled = false;
                    //Collision Module
                    CollisionModule CollisionModule = system.collision;
                    CollisionModule.collidesWith = LayerMask.GetMask("Enemies");
                    StartCoroutine(EmissionRoutine(system, Pattern, Pattern.BehaviourExitValue,Particles));
                    break;
                }
            case ShootZone.PatternType.Star:
                {
                    ParticleSystem system = ProjectilePool.Instance.GetEmitter().GetComponent<ParticleSystem>();
                    system.transform.localScale = new(system.transform.localScale.x, system.transform.localScale.y, m_inputHandler.PlayerIndex);
                    
                    EnemyFiringSystem.SetupParticleSystemParameters(system, Pattern);
                    Particle[] Particles = EnemyFiringSystem.ComputeStar(Pattern);

                    ShapeModule ShapeModule = system.shape;
                    ShapeModule.enabled = false;

                    MainModule MainModule = system.main;
                    MainModule.loop = false;
                    MainModule.playOnAwake = false;
                    EmissionModule EmissionModule = system.emission;
                    EmissionModule.enabled = false;
                    //Collision Module
                    CollisionModule CollisionModule = system.collision;
                    CollisionModule.collidesWith = LayerMask.GetMask("Enemies");
                    StartCoroutine(EmissionRoutine(system,Pattern, Pattern.BehaviourExitValue,Particles));
                    break;
                }
        }
    }
#if UNITY_EDITOR
    private void OnValidate()
    {

        if (Pattern.StartAngle > Pattern.EndAngle)
        {
            (Pattern.StartAngle, Pattern.EndAngle) = (Pattern.EndAngle, Pattern.StartAngle);
        }
            
    }
#endif
    private IEnumerator EmissionRoutine(ParticleSystem system, EnemyProjectileSpawner.ShootZone _Zone, float stopTime = -1, Particle[] _particles = null)
    {
        if (system.isStopped)
            system.Play();
        float timer = float.PositiveInfinity;
        float timeSinceCoroutineStart = 0;
        Particle[] _copy;
        if (stopTime > 0)
        {
            while (timeSinceCoroutineStart < stopTime && !m_stopEmission)
            {
                system.transform.position = m_transform.position;
                if (_Zone.Spin)
                {
                    system.transform.rotation = Quaternion.Euler(0, 0, (system.transform.rotation.eulerAngles.z + _Zone.SpinSpeed * Time.deltaTime) % 360f);
                }
                else if (_Zone.AimAtClosestPlayer)
                {
                    //need to make the equivalent for enemies
                    GameObject _closestEnemy = EnemySpawnManager.Instance.GetClosestEnemy(transform.position);
                    if (_closestEnemy != null)
                    {
                        float angle = Mathf.Rad2Deg * Mathf.Atan2(_closestEnemy.transform.position.y - transform.position.y, _closestEnemy.transform.position.x - transform.position.x);
                        system.transform.rotation = Quaternion.Euler(0, 0, angle);
                    }
                }
                timer += Time.deltaTime;
                timeSinceCoroutineStart += Time.deltaTime;

                if (_particles != null && timer > _Zone.SpawnFrequency)
                {
                    timer = 0;

                    _copy = _particles.ToArray();
                    for (int i = 0; i < _copy.Length; i++)
                    {
                        Vector3 newPos = _copy[i].position.x * system.transform.right + _copy[i].position.y * system.transform.up;
                        _copy[i].velocity = _Zone.ProjectileParameters.InitialVelocityStrength * (_Zone.CircleCenteredVelocity ? newPos : system.transform.right);
                        _copy[i].rotation3D = new(0, 0, Mathf.Rad2Deg * Mathf.Atan2(newPos.normalized.y, newPos.normalized.x));
                        newPos += system.transform.position;
                        _copy[i].position = new(newPos.x, newPos.y, 0);

                    }
                    system.SetParticles(_copy, _copy.Length, system.particleCount);
                    if (system.isStopped) system.Play();
                }
                yield return new WaitForFixedUpdate();

            }
        }
        yield return null;
        system.Stop();
        ProjectilePool.Instance.ReturnToPoolLater(system);
    }
}
