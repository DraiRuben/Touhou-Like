using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.ParticleSystem;

[RequireComponent(typeof(EntityHealthHandler))]
public class EnemyFiringSystem : MonoBehaviour
{
    [SerializeField] private EnemyProjectileSpawner ShootSettings;

    [SerializeField] private EnemyProjectileSpawner.BehaviourChangeType m_currentBehaviourType;

    private int m_nextBehaviourIndex;
    private int m_lifeAlreadyChecked = int.MaxValue;
    private bool m_stopEmission;

    private List<int> m_alreadyUsedPatterns = new();

    private EntityHealthHandler m_healthComp;
    private EnemyProjectileSpawner.ShootZone m_currentBehaviour;
    private Transform m_transform;
    private EnemyMovement m_movementHandler;

    [SerializeField] private AudioSource m_audioPlayer;
    private void Awake()
    {
        m_healthComp = GetComponent<EntityHealthHandler>();
        m_healthComp.OnHealthChanged.AddListener(() => IsInPriorityShootingBehaviour = true);
        m_healthComp.OnDeath.AddListener(() => IsInPriorityShootingBehaviour = true);
        m_healthComp.OnDeath.AddListener(() => EnemySpawnManager.Instance.TrySpawnUpgrade(transform));
        m_transform = transform;
        m_movementHandler = GetComponent<EnemyMovement>();
    }
    private void Start()
    {
        NextPattern();
    }
    private bool m_priority = false;
    private bool IsInPriorityShootingBehaviour { get { return m_priority; } set { m_priority = value; if (value) NextPattern(); } }
    [NonSerialized] public bool HasCollided = false;
    private void TryDoLifeBehaviour()
    {
        if (ShootSettings.ProjectilePatterns.ContainsKey(EnemyProjectileSpawner.BehaviourChangeType.Life))
        {
            for (int i = 0; i < ShootSettings.ProjectilePatterns[EnemyProjectileSpawner.BehaviourChangeType.Life].ShootZones.Count; i++)
            {
                if (m_currentBehaviourType != EnemyProjectileSpawner.BehaviourChangeType.Life)
                {
                    m_nextBehaviourIndex = 0;
                    m_alreadyUsedPatterns.Clear();
                }
                float maxValue = ShootSettings.ProjectilePatterns[EnemyProjectileSpawner.BehaviourChangeType.Life].ShootZones[i].BehaviourEnterValue;
                float minValue = ShootSettings.ProjectilePatterns[EnemyProjectileSpawner.BehaviourChangeType.Life].ShootZones[i].BehaviourExitValue;
                if (m_healthComp.Health <= maxValue && m_lifeAlreadyChecked > maxValue
                && m_healthComp.Health > minValue
                    && m_currentBehaviour != ShootSettings.ProjectilePatterns[EnemyProjectileSpawner.BehaviourChangeType.Life].ShootZones[i])
                {
                    m_currentBehaviour = ShootSettings.ProjectilePatterns[EnemyProjectileSpawner.BehaviourChangeType.Life].ShootZones[i];
                    m_nextBehaviourIndex = i;
                    m_currentBehaviourType = EnemyProjectileSpawner.BehaviourChangeType.Life;
                    m_movementHandler.TriggerNextMovementBehaviour = true;
                    ShootBehaviourLaunch();
                }
            }
            m_lifeAlreadyChecked = m_healthComp.Health;
        }
    }
    private void NextPattern()
    {
        if (!m_priority) //if we aren't in a behaviour for collision or death since those ones interrupt any other pattern
        {
            //firstly check if we can trigger a health pattern, else simply do a timed pattern
            TryDoLifeBehaviour();
            //if we reached there, it means we don't have any usable health patters so we can check for time patterns
            if (ShootSettings.ProjectilePatterns.ContainsKey(EnemyProjectileSpawner.BehaviourChangeType.Time))
            {
                HasCollided = false;
                ChooseNewBehaviour(EnemyProjectileSpawner.BehaviourChangeType.Time);
                return;
            }
        }
        else
        {
            if (ShootSettings.ProjectilePatterns.ContainsKey(EnemyProjectileSpawner.BehaviourChangeType.Collision)
                && m_healthComp.Health > 0 && HasCollided) //damaged
            {
                HasCollided = false;
                ChooseNewBehaviour(EnemyProjectileSpawner.BehaviourChangeType.Collision);
                return;
            }
            else if (ShootSettings.ProjectilePatterns.ContainsKey(EnemyProjectileSpawner.BehaviourChangeType.Death)
                && m_healthComp.Health <= 0)//death
            {
                HasCollided = false;
                GetComponent<Rigidbody2D>().velocity = Vector3.zero;
                GetComponent<SpriteRenderer>().enabled = false;
                GetComponent<Collider2D>().enabled = false;
                transform.GetChild(0).GetComponent<SpriteRenderer>().enabled = false;
                m_movementHandler.enabled = false;
                m_stopEmission = true;
                m_movementHandler.StopAllCoroutines();
                ChooseNewBehaviour(EnemyProjectileSpawner.BehaviourChangeType.Death);
                return;
            }
            TryDoLifeBehaviour();
        }


    }
    private void ChooseNewBehaviour(EnemyProjectileSpawner.BehaviourChangeType _newBehaviourType)
    {
        if (m_currentBehaviourType != _newBehaviourType)
        {
            m_nextBehaviourIndex = 0;
            m_alreadyUsedPatterns.Clear();
        }
        switch (ShootSettings.ProjectilePatterns[_newBehaviourType].ChoiceMethod)
        {
            case EnemyProjectileSpawner.ShootBehaviour.BehaviourChooseMethod.Sequence:
                m_currentBehaviour = ShootSettings.ProjectilePatterns[_newBehaviourType].ShootZones[m_nextBehaviourIndex];
                //makes it so that when index is beyond max index, it wraps back to 0
                m_nextBehaviourIndex = (m_nextBehaviourIndex + 1) % ShootSettings.ProjectilePatterns[_newBehaviourType].ShootZones.Count;
                break;
            case EnemyProjectileSpawner.ShootBehaviour.BehaviourChooseMethod.Random:
                m_currentBehaviour = ShootSettings.ProjectilePatterns[_newBehaviourType].ShootZones[UnityEngine.Random.Range(0, ShootSettings.ProjectilePatterns[_newBehaviourType].ShootZones.Count)];
                break;
            case EnemyProjectileSpawner.ShootBehaviour.BehaviourChooseMethod.RandomNonRepeating:
                //reset list if we already did all possible behaviours
                if (m_alreadyUsedPatterns.Count >= ShootSettings.ProjectilePatterns[_newBehaviourType].ShootZones.Count)
                {
                    m_alreadyUsedPatterns.Clear();
                }
                //generates new random number until we get one that hasn't been already used (if it was used, it's in the list)
                int index = UnityEngine.Random.Range(0, ShootSettings.ProjectilePatterns[_newBehaviourType].ShootZones.Count);
                while (m_alreadyUsedPatterns.Contains(index))
                {
                    index = UnityEngine.Random.Range(0, ShootSettings.ProjectilePatterns[_newBehaviourType].ShootZones.Count);
                }
                m_alreadyUsedPatterns.Add(index);
                m_currentBehaviour = ShootSettings.ProjectilePatterns[_newBehaviourType].ShootZones[index];
                break;
        }
        m_currentBehaviourType = _newBehaviourType;
        ShootBehaviourLaunch();
    }
    private Func<bool> GetStopCondition()
    {
        float MaxHealth = m_currentBehaviour.BehaviourEnterValue;
        float MinHealth = m_currentBehaviour.BehaviourExitValue;
        return () => m_healthComp.Health <= MaxHealth && m_healthComp.Health > MinHealth;
    }
    private void ShootBehaviourLaunch()
    {
        if (m_currentBehaviour.patternType == EnemyProjectileSpawner.ShootZone.PatternType.Circle)
        {
            List<ParticleSystem> _toUse = new();
            for (int i = 0; i < m_currentBehaviour.ZoneCount; i++)
                _toUse.Add(ProjectilePool.Instance.GetEmitter().GetComponent<ParticleSystem>());

            for (int i = 0; i < m_currentBehaviour.ZoneCount; i++)
            {
                ParticleSystem system = _toUse[i];
                system.transform.rotation = Quaternion.Euler(0, 0, 360f - 360f / (i + 1));
                SetupParticleSystemParameters(system, m_currentBehaviour);

                //Shape Module
                ShapeModule ShapeModule = system.shape;
                ShapeModule.enabled = true;
                ShapeModule.shapeType = ParticleSystemShapeType.Cone;
                ShapeModule.radius = 0.01f;
                ShapeModule.arc = Mathf.Abs(m_currentBehaviour.EndAngle - m_currentBehaviour.StartAngle);
                ShapeModule.arcMode = m_currentBehaviour.RandomPosition ? ParticleSystemShapeMultiModeValue.Random : ParticleSystemShapeMultiModeValue.BurstSpread;
                system.transform.SetPositionAndRotation(transform.position, Quaternion.Euler(0, 0, i * (-360f / m_currentBehaviour.ZoneCount)));

                //Burst Emission
                Burst[] burst = new Burst[1];
                MinMaxCurve countCurve = burst[0].count;
                countCurve.constant = m_currentBehaviour.ProjectileCount;
                burst[0].count = countCurve;
                burst[0].repeatInterval = m_currentBehaviour.SpawnFrequency;
                burst[0].cycleCount = 0;
                system.emission.SetBursts(burst);

                //Emission Module
                EmissionModule EmissionModule = system.emission;
                EmissionModule.rateOverDistance = 0;
                EmissionModule.rateOverTime = 0;

                ParticleSystemRenderer particleSystemRenderer = system.GetComponent<ParticleSystemRenderer>();
                particleSystemRenderer.alignment = ParticleSystemRenderSpace.Velocity;
                particleSystemRenderer.renderMode = ParticleSystemRenderMode.Billboard;
                particleSystemRenderer.sortMode = ParticleSystemSortMode.OldestInFront;


                if (m_currentBehaviourType == EnemyProjectileSpawner.BehaviourChangeType.Time)
                    StartCoroutine(EmissionRoutine(system, m_currentBehaviour, !m_currentBehaviour.InfiniteDuration ? m_currentBehaviour.BehaviourExitValue : -1));
                else if (m_currentBehaviourType == EnemyProjectileSpawner.BehaviourChangeType.Life)
                    StartCoroutine(EmissionRoutine(system, m_currentBehaviour, -1, GetStopCondition()));
                else
                {
                    Burst _burst = EmissionModule.GetBurst(0);
                    _burst.cycleCount = (int)m_currentBehaviour.BehaviourExitValue;
                    EmissionModule.SetBurst(0, _burst);
                    var MainModule = system.main;
                    MainModule.loop = false;
                    if (system.isStopped) system.Play();
                    ProjectilePool.Instance.ReturnToPoolLater(system);
                    Destroy(gameObject,.1f);
                }
            }
        }
        else if (m_currentBehaviour.patternType == EnemyProjectileSpawner.ShootZone.PatternType.Polygon)
        {
            ParticleSystem system = ProjectilePool.Instance.GetEmitter().GetComponent<ParticleSystem>();

            SetupParticleSystemParameters(system, m_currentBehaviour);
            Particle[] Particles = ComputePolygon(m_currentBehaviour);

            ShapeModule ShapeModule = system.shape;
            ShapeModule.enabled = false;

            MainModule MainModule = system.main;
            MainModule.loop = false;
            MainModule.playOnAwake = false;

            EmissionModule EmissionModule = system.emission;
            EmissionModule.enabled = false;

            if (m_currentBehaviourType == EnemyProjectileSpawner.BehaviourChangeType.Time)
                StartCoroutine(EmissionRoutine(system, m_currentBehaviour, !m_currentBehaviour.InfiniteDuration ? m_currentBehaviour.BehaviourExitValue : -1, null, Particles));
            else if (m_currentBehaviourType == EnemyProjectileSpawner.BehaviourChangeType.Life)
                StartCoroutine(EmissionRoutine(system, m_currentBehaviour, -1, GetStopCondition(), Particles));
            else
            {
                StartCoroutine(EmissionRoutine(system, m_currentBehaviour, m_currentBehaviour.BehaviourExitValue, null, Particles));
            }
        }
        else if (m_currentBehaviour.patternType == EnemyProjectileSpawner.ShootZone.PatternType.Star)
        {
            ParticleSystem system = ProjectilePool.Instance.GetEmitter().GetComponent<ParticleSystem>();
            SetupParticleSystemParameters(system, m_currentBehaviour);

            Particle[] Particles = ComputeStar(m_currentBehaviour);

            ShapeModule ShapeModule = system.shape;
            ShapeModule.enabled = false;

            MainModule MainModule = system.main;
            MainModule.loop = false;
            MainModule.playOnAwake = false;
            EmissionModule EmissionModule = system.emission;
            EmissionModule.enabled = false;

            if (m_currentBehaviourType == EnemyProjectileSpawner.BehaviourChangeType.Time)
                StartCoroutine(EmissionRoutine(system, m_currentBehaviour, !m_currentBehaviour.InfiniteDuration ? m_currentBehaviour.BehaviourExitValue : -1, null, Particles));
            else if (m_currentBehaviourType == EnemyProjectileSpawner.BehaviourChangeType.Life)
                StartCoroutine(EmissionRoutine(system, m_currentBehaviour, -1, GetStopCondition(), Particles));
            else
            {
                StartCoroutine(EmissionRoutine(system, m_currentBehaviour, m_currentBehaviour.BehaviourExitValue, null, Particles));
            }
        }
    }
    public static Particle[] ComputePolygon(EnemyProjectileSpawner.ShootZone _attackInfo)
    {
        float angleSpan = Mathf.Abs(_attackInfo.EndAngle - _attackInfo.StartAngle);
        float angle = 360f / _attackInfo.Vertices;
        int verticeCount = _attackInfo.Vertices;

        int particleCount = verticeCount * _attackInfo.ProjectileCount - verticeCount;
        Particle[] Particles = new Particle[particleCount];
        Vector3 EdgePos = new();
        Vector3 NextEdgePos = new();

        float x1;
        float y1;

        float toRot;
        int index;

        Vector3 AimDir = _attackInfo.CenterDistance * new Vector3(Mathf.Cos(_attackInfo.StartAngle * Mathf.Deg2Rad), Mathf.Sin(_attackInfo.StartAngle * Mathf.Deg2Rad));
        for (int i = 0; i < verticeCount; i++)
        {

            toRot = (angle * i + _attackInfo.StartAngle) * Mathf.Deg2Rad;
            EdgePos.Set(
                Mathf.Cos(toRot) * _attackInfo.CenterDistance,
                Mathf.Sin(toRot) * _attackInfo.CenterDistance, 0);
            NextEdgePos.Set(
                Mathf.Cos((angle * (i + 1) + _attackInfo.StartAngle) * Mathf.Deg2Rad) * _attackInfo.CenterDistance,
                Mathf.Sin((angle * (i + 1) + _attackInfo.StartAngle) * Mathf.Deg2Rad) * _attackInfo.CenterDistance, 0);

            index = i * (_attackInfo.ProjectileCount - 1);
            Particles[index].position = EdgePos;
            Particles[index].velocity = _attackInfo.ProjectileParameters.InitialVelocityStrength * (_attackInfo.CircleCenteredVelocity ? EdgePos : AimDir) / _attackInfo.CenterDistance;
            Particles[index].startSize3D = new Vector3(_attackInfo.ProjectileParameters.InitialScale.x, _attackInfo.ProjectileParameters.InitialScale.y, 1);
            Particles[index].startLifetime = _attackInfo.ProjectileParameters.LifeTime;
            Particles[index].remainingLifetime = _attackInfo.ProjectileParameters.LifeTime;
            Particles[index].startColor = UnityEngine.Color.white;
            Particles[index].rotation3D = new Vector3(0, 0, Mathf.Rad2Deg * Mathf.Atan2(EdgePos.y, EdgePos.x));


            for (int u = 0; u < _attackInfo.ProjectileCount - 2; u++)
            {
                x1 = Mathf.Lerp(EdgePos.x, NextEdgePos.x, (float)(u + 1) / (_attackInfo.ProjectileCount - 1));
                y1 = Mathf.Lerp(EdgePos.y, NextEdgePos.y, (float)(u + 1) / (_attackInfo.ProjectileCount - 1));

                index = u + i * (_attackInfo.ProjectileCount - 1) + 1;
                Particles[index].position = new Vector3(x1, y1);
                Particles[index].velocity = _attackInfo.ProjectileParameters.InitialVelocityStrength * (_attackInfo.CircleCenteredVelocity ? Particles[index].position : AimDir) / _attackInfo.CenterDistance;
                Particles[index].startSize3D = new Vector3(_attackInfo.ProjectileParameters.InitialScale.x, _attackInfo.ProjectileParameters.InitialScale.y, 1);
                Particles[index].startLifetime = _attackInfo.ProjectileParameters.LifeTime;
                Particles[index].remainingLifetime = _attackInfo.ProjectileParameters.LifeTime;
                Particles[index].startColor = UnityEngine.Color.white;
                Particles[index].rotation3D = new Vector3(0, 0, Mathf.Rad2Deg * Mathf.Atan2(y1, x1));
            }
        }
        return Particles;
    }
    public static Particle[] ComputeStar(EnemyProjectileSpawner.ShootZone _attackInfo)
    {
        float angle = 360f / _attackInfo.Limbs;
        int particleCount = _attackInfo.Limbs * ((_attackInfo.ProjectileCount - 1) * 2 + (int)(_attackInfo.ProjectileCount / 1.2f) - 2);
        Particle[] Particles = new Particle[particleCount];
        Vector3 EdgePos = new();
        Vector3 LeftEdgePos = new();
        Vector3 RightEdgePos = new();
        Vector3 MedianDir = new Vector3(Mathf.Cos((_attackInfo.StartAngle) * Mathf.Deg2Rad), Mathf.Sin((_attackInfo.StartAngle) * Mathf.Deg2Rad));
        float toRot;
        float x1;
        float y1;
        for (int i = 0; i < _attackInfo.Limbs; i++)
        {
            toRot = 2 * Mathf.PI * i / _attackInfo.Limbs + _attackInfo.StartAngle * Mathf.Deg2Rad;
            EdgePos.Set(
                Mathf.Cos(toRot) * _attackInfo.CenterDistance,
                Mathf.Sin(toRot) * _attackInfo.CenterDistance, 0);

            toRot = 2 * Mathf.PI * i / _attackInfo.Limbs - 2 * Mathf.PI / (2 * _attackInfo.Limbs) + _attackInfo.StartAngle * Mathf.Deg2Rad;
            RightEdgePos.Set(
                Mathf.Cos(toRot) * _attackInfo.InnerPointsDist,
                Mathf.Sin(toRot) * _attackInfo.InnerPointsDist, 0);

            toRot = 2 * Mathf.PI * i / _attackInfo.Limbs + 2 * Mathf.PI / (2 * _attackInfo.Limbs) + _attackInfo.StartAngle * Mathf.Deg2Rad;
            LeftEdgePos.Set(
                 Mathf.Cos(toRot) * _attackInfo.InnerPointsDist,
                 Mathf.Sin(toRot) * _attackInfo.InnerPointsDist, 0);


            int firstCornerIndex = i * ((particleCount / _attackInfo.Limbs));
            Particles[firstCornerIndex].position = EdgePos;
            Particles[firstCornerIndex].velocity = _attackInfo.ProjectileParameters.InitialVelocityStrength * (_attackInfo.CircleCenteredVelocity ? EdgePos : MedianDir) / _attackInfo.CenterDistance;
            Particles[firstCornerIndex].startSize3D = new Vector3(_attackInfo.ProjectileParameters.InitialScale.x, _attackInfo.ProjectileParameters.InitialScale.y, 1);
            Particles[firstCornerIndex].startLifetime = _attackInfo.ProjectileParameters.LifeTime;
            Particles[firstCornerIndex].remainingLifetime = _attackInfo.ProjectileParameters.LifeTime;
            Particles[firstCornerIndex].startColor = UnityEngine.Color.white;
            Particles[firstCornerIndex].rotation3D = new Vector3(0, 0, Mathf.Rad2Deg * Mathf.Atan2(EdgePos.y, EdgePos.x));

            for (int u = 1; u < _attackInfo.ProjectileCount - 1; u++)
            {
                x1 = Mathf.Lerp(LeftEdgePos.x, EdgePos.x, (float)u / (_attackInfo.ProjectileCount - 1));
                y1 = Mathf.Lerp(LeftEdgePos.y, EdgePos.y, (float)u / (_attackInfo.ProjectileCount - 1));

                int index = u + firstCornerIndex;
                Particles[index].position = new Vector3(x1, y1);
                Particles[index].velocity = _attackInfo.ProjectileParameters.InitialVelocityStrength * (_attackInfo.CircleCenteredVelocity ? Particles[index].position : MedianDir) / _attackInfo.CenterDistance;
                Particles[index].startSize3D = new Vector3(_attackInfo.ProjectileParameters.InitialScale.x, _attackInfo.ProjectileParameters.InitialScale.y, 1);
                Particles[index].startLifetime = _attackInfo.ProjectileParameters.LifeTime;
                Particles[index].remainingLifetime = _attackInfo.ProjectileParameters.LifeTime;
                Particles[index].startColor = UnityEngine.Color.white;
                Particles[index].rotation3D = new Vector3(0, 0, Mathf.Rad2Deg * Mathf.Atan2(y1, x1));

            }
            for (int u = 0; u < _attackInfo.ProjectileCount - 1; u++)
            {
                x1 = Mathf.Lerp(RightEdgePos.x, EdgePos.x, (float)u / (_attackInfo.ProjectileCount - 1));
                y1 = Mathf.Lerp(RightEdgePos.y, EdgePos.y, (float)u / (_attackInfo.ProjectileCount - 1));

                int index = u + firstCornerIndex + _attackInfo.ProjectileCount - 1;
                Particles[index].position = new Vector3(x1, y1);
                Particles[index].velocity = _attackInfo.ProjectileParameters.InitialVelocityStrength * (_attackInfo.CircleCenteredVelocity ? Particles[index].position : MedianDir) / _attackInfo.CenterDistance;
                Particles[index].startSize3D = new Vector3(_attackInfo.ProjectileParameters.InitialScale.x, _attackInfo.ProjectileParameters.InitialScale.y, 1);
                Particles[index].startLifetime = _attackInfo.ProjectileParameters.LifeTime;
                Particles[index].remainingLifetime = _attackInfo.ProjectileParameters.LifeTime;
                Particles[index].startColor = UnityEngine.Color.white;
                Particles[index].rotation3D = new Vector3(0, 0, Mathf.Rad2Deg * Mathf.Atan2(y1, x1));

            }
            int InnerCount = (int)(_attackInfo.ProjectileCount / 1.2f) - 2;
            for (int u = 0; u < InnerCount; u++)
            {
                x1 = Mathf.Lerp(LeftEdgePos.x, RightEdgePos.x, ((float)(u + 1)) / (InnerCount + 1));
                y1 = Mathf.Lerp(LeftEdgePos.y, RightEdgePos.y, ((float)(u + 1)) / (InnerCount + 1));

                int index = u + firstCornerIndex + 2 * _attackInfo.ProjectileCount - 2;
                Particles[index].position = new Vector3(x1, y1);
                Particles[index].velocity = _attackInfo.ProjectileParameters.InitialVelocityStrength * (_attackInfo.CircleCenteredVelocity ? Particles[index].position : MedianDir) / _attackInfo.CenterDistance;
                Particles[index].startSize3D = new Vector3(_attackInfo.ProjectileParameters.InitialScale.x, _attackInfo.ProjectileParameters.InitialScale.y, 1);
                Particles[index].startLifetime = _attackInfo.ProjectileParameters.LifeTime;
                Particles[index].remainingLifetime = _attackInfo.ProjectileParameters.LifeTime;
                Particles[index].startColor = UnityEngine.Color.white;
                Particles[index].rotation3D = new Vector3(0, 0, Mathf.Rad2Deg * Mathf.Atan2(y1, x1));

            }

        }
        return Particles;
    }
    
    private IEnumerator EmissionRoutine(ParticleSystem system, EnemyProjectileSpawner.ShootZone _Zone, float stopTime = -1, Func<bool> condition = null, Particle[] _particles = null)
    {
        if (system.isStopped)
            system.Play();
        bool death = m_stopEmission;
        float timer = 1000;
        float timeSinceCoroutineStart = 0;
        Particle[] _copy;

        //Believe it or not, this huge code repetition makes it more optimised than if we were to put everything in a single While loop
        //this is due to the fact that we would have to check each frame for a lot of conditions instead of a select few
        if (stopTime > 0)
        {
            while (timeSinceCoroutineStart < stopTime && (!m_stopEmission || death))
            {
                system.transform.position = m_transform.position;
                if (_Zone.Spin)
                {
                    system.transform.rotation = Quaternion.Euler(0, 0, (system.transform.rotation.eulerAngles.z + _Zone.SpinSpeed * Time.deltaTime) % 360f);
                }
                else if (_Zone.AimAtClosestPlayer)
                {
                    GameObject _closestPlayer = PlayerManager.Instance.GetClosestPlayer(transform.position);
                    if (_closestPlayer != null)
                    {
                        float x = _closestPlayer.transform.position.x - transform.position.x;
                        float y = _closestPlayer.transform.position.y - transform.position.y;
                        float angle =0;
                        if (!(x == 0 && y == 0) && x!=float.NaN && y!=float.NaN) //checking for NaN since for some fucking reason it can happen
                        {
                            angle = Mathf.Rad2Deg * Mathf.Atan2(y, x);
                        }
                        system.transform.rotation = Quaternion.Euler(0, 0, angle);
                    }
                }
                //breaks when using multiple zones in a circle pattern
                if (_Zone.RotationFollowsAim)
                    transform.rotation = system.transform.rotation;

                timer += Time.deltaTime;
                timeSinceCoroutineStart += Time.deltaTime;
                if (timer > _Zone.SpawnFrequency)
                {
                    timer = 0;
                    m_audioPlayer.PlayOneShot(m_audioPlayer.clip);
                    if (_particles != null && _Zone.patternType != EnemyProjectileSpawner.ShootZone.PatternType.Circle)
                    {

                        _copy = _particles.ToArray();
                        for (int i = 0; i < _copy.Length; i++)
                        {
                            Vector3 newPos = _copy[i].position.x * system.transform.right + _copy[i].position.y * system.transform.up;
                            _copy[i].velocity = _Zone.ProjectileParameters.InitialVelocityStrength * (_Zone.CircleCenteredVelocity ? newPos/ _Zone.CenterDistance : system.transform.right);
                            _copy[i].rotation3D = new(0, 0, Mathf.Rad2Deg * Mathf.Atan2(newPos.normalized.y, newPos.normalized.x));
                            newPos += system.transform.position;
                            _copy[i].position = new(newPos.x, newPos.y, 0);

                        }
                        system.SetParticles(_copy, _copy.Length, system.particleCount);
                        if (system.isStopped) system.Play();
                    }
                }
                yield return new WaitForFixedUpdate();
            }
        }
        else if (condition != null)
        {
            while (condition.Invoke() && !m_stopEmission)
            {
                system.transform.position = m_transform.position;
                if (_Zone.Spin)
                {
                    system.transform.rotation = Quaternion.Euler(0, 0, (system.transform.rotation.eulerAngles.z + _Zone.SpinSpeed * Time.deltaTime) % 360f);
                }
                else if (_Zone.AimAtClosestPlayer)
                {
                    GameObject _closestPlayer = PlayerManager.Instance.GetClosestPlayer(transform.position);
                    if (_closestPlayer != null)
                    {
                        float x = _closestPlayer.transform.position.x - transform.position.x;
                        float y = _closestPlayer.transform.position.y - transform.position.y;
                        float angle = 0;
                        if (!(x == 0 && y == 0) && x != float.NaN && y != float.NaN) //checking for NaN since for some fucking reason it can happen
                        {
                            angle = Mathf.Rad2Deg * Mathf.Atan2(y, x);
                        }
                        system.transform.rotation = Quaternion.Euler(0, 0, angle);

                    }
                }
                if (_Zone.RotationFollowsAim)
                    transform.rotation = system.transform.rotation;

                timer += Time.deltaTime;
                timeSinceCoroutineStart += Time.deltaTime;
                if(timer > _Zone.SpawnFrequency)
                {
                    timer = 0;
                    m_audioPlayer.PlayOneShot(m_audioPlayer.clip);
                    if (_particles != null && _Zone.patternType != EnemyProjectileSpawner.ShootZone.PatternType.Circle)
                    {

                        _copy = _particles.ToArray();
                        for (int i = 0; i < _copy.Length; i++)
                        {
                            Vector3 newPos = _copy[i].position.x * system.transform.right + _copy[i].position.y * system.transform.up;
                            _copy[i].velocity = _Zone.ProjectileParameters.InitialVelocityStrength * (_Zone.CircleCenteredVelocity ? newPos/ _Zone.CenterDistance : system.transform.right);
                            _copy[i].rotation3D = new(0, 0, Mathf.Rad2Deg * Mathf.Atan2(newPos.normalized.y, newPos.normalized.x));
                            newPos += system.transform.position;
                            _copy[i].position = new(newPos.x, newPos.y, 0);

                        }
                        system.SetParticles(_copy, _copy.Length, system.particleCount);
                        if (system.isStopped) system.Play();
                    }
                }
                
                yield return new WaitForFixedUpdate();
            }
        }
        else
        {
            while (!m_stopEmission)
            {
                system.transform.position = m_transform.position;
                if (_Zone.Spin)
                {
                    system.transform.rotation = Quaternion.Euler(0, 0, (system.transform.rotation.eulerAngles.z + _Zone.SpinSpeed * Time.deltaTime) % 360f);
                }
                else if (_Zone.AimAtClosestPlayer)
                {
                    GameObject _closestPlayer = PlayerManager.Instance.GetClosestPlayer(transform.position);
                    if (_closestPlayer != null)
                    {
                        float x = _closestPlayer.transform.position.x - transform.position.x;
                        float y = _closestPlayer.transform.position.y - transform.position.y;
                        float angle = 0;
                        if (!(x == 0 && y == 0) && x != float.NaN && y != float.NaN) //checking for NaN since for some fucking reason it can happen
                        {
                            angle = Mathf.Rad2Deg * Mathf.Atan2(y, x);
                        }
                        system.transform.rotation = Quaternion.Euler(0, 0, angle);

                    }
                }
                if (_Zone.RotationFollowsAim)
                    transform.rotation = system.transform.rotation;

                timer += Time.deltaTime;
                timeSinceCoroutineStart += Time.deltaTime;
                if (timer > _Zone.SpawnFrequency)
                {
                    timer = 0;
                    m_audioPlayer.PlayOneShot(m_audioPlayer.clip);
                    if (_particles != null && _Zone.patternType != EnemyProjectileSpawner.ShootZone.PatternType.Circle)
                    {

                        _copy = _particles.ToArray();
                        for (int i = 0; i < _copy.Length; i++)
                        {
                            Vector3 newPos = _copy[i].position.x * system.transform.right + _copy[i].position.y * system.transform.up;
                            _copy[i].velocity = _Zone.ProjectileParameters.InitialVelocityStrength * (_Zone.CircleCenteredVelocity ? newPos/ _Zone.CenterDistance : system.transform.right);
                            _copy[i].rotation3D = new(0, 0, Mathf.Rad2Deg * Mathf.Atan2(newPos.normalized.y, newPos.normalized.x));
                            newPos += system.transform.position;
                            _copy[i].position = new(newPos.x, newPos.y, 0);

                        }
                        system.SetParticles(_copy, _copy.Length, system.particleCount);
                        if (system.isStopped) system.Play();
                    }
                }
                yield return new WaitForFixedUpdate();
            }
        }
        system.Stop();
        ProjectilePool.Instance.ReturnToPoolLater(system);
        if (m_healthComp.Health > 0)
            NextPattern();
        else if(death || !ShootSettings.ProjectilePatterns.ContainsKey(EnemyProjectileSpawner.BehaviourChangeType.Death))
            Destroy(gameObject);
    }
    public static void SetupParticleSystemParameters(ParticleSystem system, EnemyProjectileSpawner.ShootZone _attackInfo)
    {
        //Render Module
        ParticleSystemRenderer particleSystemRenderer = system.GetComponent<ParticleSystemRenderer>();
        particleSystemRenderer.material = _attackInfo.ProjectileParameters.Mat;
        particleSystemRenderer.renderMode = ParticleSystemRenderMode.Mesh;
        particleSystemRenderer.sortMode = ParticleSystemSortMode.None;
        particleSystemRenderer.alignment = ParticleSystemRenderSpace.World;
        //Main Module
        MainModule mainModule = system.main;
        mainModule.startColor = _attackInfo.ProjectileParameters.InitialColor;
        mainModule.startSize3D = true;
        mainModule.startSizeX = _attackInfo.ProjectileParameters.InitialScale.x;
        mainModule.startSizeY = _attackInfo.ProjectileParameters.InitialScale.y;
        mainModule.startLifetime = _attackInfo.ProjectileParameters.LifeTime;
        mainModule.startSpeed = _attackInfo.ProjectileParameters.InitialVelocityStrength;
        mainModule.startColor = _attackInfo.ProjectileParameters.InitialColor;
        mainModule.startRotation3D = true;
        mainModule.loop = true;
        mainModule.playOnAwake = true;
        mainModule.maxParticles = 10000;
        mainModule.simulationSpace = ParticleSystemSimulationSpace.World;

        MinMaxCurve RotZ = mainModule.startRotationZ;
        RotZ.constant = _attackInfo.ProjectileParameters.InitialRotation * Mathf.Deg2Rad;
        mainModule.startRotationZ = RotZ;

        //Emitter shape module
        ShapeModule EmitterModule = system.shape;
        EmitterModule.enabled = true;

        //Emission module
        EmissionModule EmissionModule = system.emission;
        EmissionModule.enabled = true;

        //Sprite Module
        TextureSheetAnimationModule SpriteModule = system.textureSheetAnimation;
        SpriteModule.enabled = true;
        SpriteModule.mode = ParticleSystemAnimationMode.Sprites;
        SpriteModule.SetSprite(0, _attackInfo.ProjectileParameters.Texture);

        //Collision module
        CollisionModule CollisionModule = system.collision;
        CollisionModule.enabled = true;
        CollisionModule.type = ParticleSystemCollisionType.World;
        CollisionModule.mode = ParticleSystemCollisionMode.Collision2D;
        CollisionModule.lifetimeLoss = 1;
        CollisionModule.collidesWith = LayerMask.GetMask("Player");
        CollisionModule.sendCollisionMessages = true;

        //ScaleOverLifetime
        SizeOverLifetimeModule ScaleModule = system.sizeOverLifetime;
        if (_attackInfo.ProjectileParameters.VariableScale)
        {
            ScaleModule.enabled = true;
            ScaleModule.size = _attackInfo.ProjectileParameters.ScaleOverTime;
        }
        else
        {
            ScaleModule.enabled = false;
        }

        //RotationOverLifetime
        RotationOverLifetimeModule RotationModule = system.rotationOverLifetime;
        if (_attackInfo.ProjectileParameters.VariableRotation)
        {
            RotationModule.enabled = true;
            RotationModule.separateAxes = true;
            RotationModule.z = _attackInfo.ProjectileParameters.RotationOverTime;
        }
        else
        {
            RotationModule.enabled = false;
        }

        //ColorOverLifetime
        ColorOverLifetimeModule ColorModule = system.colorOverLifetime;
        if (_attackInfo.ProjectileParameters.VariableColor)
        {
            ColorModule.enabled = true;
            ColorModule.color = _attackInfo.ProjectileParameters.ColorOverTime;
        }
        else
        {
            ColorModule.enabled = false;
        }

        //VelocityOverLifetime
        VelocityOverLifetimeModule VelocityModule = system.velocityOverLifetime;
        if (_attackInfo.ProjectileParameters.VariableVelocity)
        {
            VelocityModule.enabled = true;
            VelocityModule.space = ParticleSystemSimulationSpace.World;
            VelocityModule.speedModifier = _attackInfo.ProjectileParameters.VelocityOverTime;
        }
        else
        {
            VelocityModule.speedModifier = new(1);
        }

        //Bullet curve
        if (_attackInfo.ProjectileParameters.BulletCurve)
        {
            VelocityModule.enabled = true;
            VelocityModule.space = ParticleSystemSimulationSpace.World;
            VelocityModule.orbitalZ = _attackInfo.ProjectileParameters.TrajectoryOverTime;

            var OX = VelocityModule.orbitalX;
            OX.mode = VelocityModule.orbitalZ.mode;
            OX.curveMultiplier = 0;
            OX.constant = 0;
            VelocityModule.orbitalX = OX;

            var OY = VelocityModule.orbitalY;
            OY.mode = VelocityModule.orbitalZ.mode;
            OY.curveMultiplier = 0;
            OY.constant = 0;
            VelocityModule.orbitalY = OY;

        }
        else
        {
            VelocityModule.orbitalZ = new();
        }
        if (!_attackInfo.ProjectileParameters.BulletCurve && !_attackInfo.ProjectileParameters.VariableVelocity)
        {
            VelocityModule.enabled = false;
        }
    }
}
