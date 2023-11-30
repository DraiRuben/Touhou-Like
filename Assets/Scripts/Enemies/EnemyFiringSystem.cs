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
                SetupParticleSystemParameters(system);

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

            SetupParticleSystemParameters(system);
            Particle[] Particles = ComputePolygon();

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
            SetupParticleSystemParameters(system);

            Particle[] Particles = ComputeStar();

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
    private Particle[] ComputePolygon()
    {
        float angleSpan = Mathf.Abs(m_currentBehaviour.EndAngle - m_currentBehaviour.StartAngle);
        float angle = 360f / m_currentBehaviour.Vertices;
        int verticeCount = m_currentBehaviour.Vertices;

        int particleCount = verticeCount * m_currentBehaviour.ProjectileCount - verticeCount;
        Particle[] Particles = new Particle[particleCount];
        Vector3 EdgePos = new();
        Vector3 NextEdgePos = new();

        float x1;
        float y1;

        float toRot;
        int index;

        Vector3 AimDir = m_currentBehaviour.CenterDistance * new Vector3(Mathf.Cos(m_currentBehaviour.StartAngle * Mathf.Deg2Rad), Mathf.Sin(m_currentBehaviour.StartAngle * Mathf.Deg2Rad));
        for (int i = 0; i < verticeCount; i++)
        {

            toRot = (angle * i + m_currentBehaviour.StartAngle) * Mathf.Deg2Rad;
            EdgePos.Set(
                Mathf.Cos(toRot) * m_currentBehaviour.CenterDistance,
                Mathf.Sin(toRot) * m_currentBehaviour.CenterDistance, 0);
            NextEdgePos.Set(
                Mathf.Cos((angle * (i + 1) + m_currentBehaviour.StartAngle) * Mathf.Deg2Rad) * m_currentBehaviour.CenterDistance,
                Mathf.Sin((angle * (i + 1) + m_currentBehaviour.StartAngle) * Mathf.Deg2Rad) * m_currentBehaviour.CenterDistance, 0);

            index = i * (m_currentBehaviour.ProjectileCount - 1);
            Particles[index].position = EdgePos;
            Particles[index].velocity = m_currentBehaviour.ProjectileParameters.InitialVelocityStrength * (m_currentBehaviour.CircleCenteredVelocity ? EdgePos : AimDir) / m_currentBehaviour.CenterDistance;
            Particles[index].startSize3D = new Vector3(m_currentBehaviour.ProjectileParameters.InitialScale.x, m_currentBehaviour.ProjectileParameters.InitialScale.y, 1);
            Particles[index].startLifetime = m_currentBehaviour.ProjectileParameters.LifeTime;
            Particles[index].remainingLifetime = m_currentBehaviour.ProjectileParameters.LifeTime;
            Particles[index].startColor = UnityEngine.Color.white;
            Particles[index].rotation3D = new Vector3(0, 0, Mathf.Rad2Deg * Mathf.Atan2(EdgePos.y, EdgePos.x));


            for (int u = 0; u < m_currentBehaviour.ProjectileCount - 2; u++)
            {
                x1 = Mathf.Lerp(EdgePos.x, NextEdgePos.x, (float)(u + 1) / (m_currentBehaviour.ProjectileCount - 1));
                y1 = Mathf.Lerp(EdgePos.y, NextEdgePos.y, (float)(u + 1) / (m_currentBehaviour.ProjectileCount - 1));

                index = u + i * (m_currentBehaviour.ProjectileCount - 1) + 1;
                Particles[index].position = new Vector3(x1, y1);
                Particles[index].velocity = m_currentBehaviour.ProjectileParameters.InitialVelocityStrength * (m_currentBehaviour.CircleCenteredVelocity ? Particles[index].position : AimDir) / m_currentBehaviour.CenterDistance;
                Particles[index].startSize3D = new Vector3(m_currentBehaviour.ProjectileParameters.InitialScale.x, m_currentBehaviour.ProjectileParameters.InitialScale.y, 1);
                Particles[index].startLifetime = m_currentBehaviour.ProjectileParameters.LifeTime;
                Particles[index].remainingLifetime = m_currentBehaviour.ProjectileParameters.LifeTime;
                Particles[index].startColor = UnityEngine.Color.white;
                Particles[index].rotation3D = new Vector3(0, 0, Mathf.Rad2Deg * Mathf.Atan2(y1, x1));
            }
        }
        return Particles;
    }
    private Particle[] ComputeStar()
    {
        float angle = 360f / m_currentBehaviour.Limbs;
        int particleCount = m_currentBehaviour.Limbs * ((m_currentBehaviour.ProjectileCount - 1) * 2 + (int)(m_currentBehaviour.ProjectileCount / 1.2f) - 2);
        Particle[] Particles = new Particle[particleCount];
        Vector3 EdgePos = new();
        Vector3 LeftEdgePos = new();
        Vector3 RightEdgePos = new();
        Vector3 MedianDir = new Vector3(Mathf.Cos((m_currentBehaviour.StartAngle) * Mathf.Deg2Rad), Mathf.Sin((m_currentBehaviour.StartAngle) * Mathf.Deg2Rad));
        float toRot;
        float x1;
        float y1;
        for (int i = 0; i < m_currentBehaviour.Limbs; i++)
        {
            toRot = 2 * Mathf.PI * i / m_currentBehaviour.Limbs + m_currentBehaviour.StartAngle * Mathf.Deg2Rad;
            EdgePos.Set(
                Mathf.Cos(toRot) * m_currentBehaviour.CenterDistance,
                Mathf.Sin(toRot) * m_currentBehaviour.CenterDistance, 0);

            toRot = 2 * Mathf.PI * i / m_currentBehaviour.Limbs - 2 * Mathf.PI / (2 * m_currentBehaviour.Limbs) + m_currentBehaviour.StartAngle * Mathf.Deg2Rad;
            RightEdgePos.Set(
                Mathf.Cos(toRot) * m_currentBehaviour.InnerPointsDist,
                Mathf.Sin(toRot) * m_currentBehaviour.InnerPointsDist, 0);

            toRot = 2 * Mathf.PI * i / m_currentBehaviour.Limbs + 2 * Mathf.PI / (2 * m_currentBehaviour.Limbs) + m_currentBehaviour.StartAngle * Mathf.Deg2Rad;
            LeftEdgePos.Set(
                 Mathf.Cos(toRot) * m_currentBehaviour.InnerPointsDist,
                 Mathf.Sin(toRot) * m_currentBehaviour.InnerPointsDist, 0);


            int firstCornerIndex = i * ((particleCount / m_currentBehaviour.Limbs));
            Particles[firstCornerIndex].position = EdgePos;
            Particles[firstCornerIndex].velocity = m_currentBehaviour.ProjectileParameters.InitialVelocityStrength * (m_currentBehaviour.CircleCenteredVelocity ? EdgePos : MedianDir) / m_currentBehaviour.CenterDistance;
            Particles[firstCornerIndex].startSize3D = new Vector3(m_currentBehaviour.ProjectileParameters.InitialScale.x, m_currentBehaviour.ProjectileParameters.InitialScale.y, 1);
            Particles[firstCornerIndex].startLifetime = m_currentBehaviour.ProjectileParameters.LifeTime;
            Particles[firstCornerIndex].remainingLifetime = m_currentBehaviour.ProjectileParameters.LifeTime;
            Particles[firstCornerIndex].startColor = UnityEngine.Color.white;
            Particles[firstCornerIndex].rotation3D = new Vector3(0, 0, Mathf.Rad2Deg * Mathf.Atan2(EdgePos.y, EdgePos.x));

            for (int u = 1; u < m_currentBehaviour.ProjectileCount - 1; u++)
            {
                x1 = Mathf.Lerp(LeftEdgePos.x, EdgePos.x, (float)u / (m_currentBehaviour.ProjectileCount - 1));
                y1 = Mathf.Lerp(LeftEdgePos.y, EdgePos.y, (float)u / (m_currentBehaviour.ProjectileCount - 1));

                int index = u + firstCornerIndex;
                Particles[index].position = new Vector3(x1, y1);
                Particles[index].velocity = m_currentBehaviour.ProjectileParameters.InitialVelocityStrength * (m_currentBehaviour.CircleCenteredVelocity ? Particles[index].position : MedianDir) / m_currentBehaviour.CenterDistance;
                Particles[index].startSize3D = new Vector3(m_currentBehaviour.ProjectileParameters.InitialScale.x, m_currentBehaviour.ProjectileParameters.InitialScale.y, 1);
                Particles[index].startLifetime = m_currentBehaviour.ProjectileParameters.LifeTime;
                Particles[index].remainingLifetime = m_currentBehaviour.ProjectileParameters.LifeTime;
                Particles[index].startColor = UnityEngine.Color.white;
                Particles[index].rotation3D = new Vector3(0, 0, Mathf.Rad2Deg * Mathf.Atan2(y1, x1));

            }
            for (int u = 0; u < m_currentBehaviour.ProjectileCount - 1; u++)
            {
                x1 = Mathf.Lerp(RightEdgePos.x, EdgePos.x, (float)u / (m_currentBehaviour.ProjectileCount - 1));
                y1 = Mathf.Lerp(RightEdgePos.y, EdgePos.y, (float)u / (m_currentBehaviour.ProjectileCount - 1));

                int index = u + firstCornerIndex + m_currentBehaviour.ProjectileCount - 1;
                Particles[index].position = new Vector3(x1, y1);
                Particles[index].velocity = m_currentBehaviour.ProjectileParameters.InitialVelocityStrength * (m_currentBehaviour.CircleCenteredVelocity ? Particles[index].position : MedianDir) / m_currentBehaviour.CenterDistance;
                Particles[index].startSize3D = new Vector3(m_currentBehaviour.ProjectileParameters.InitialScale.x, m_currentBehaviour.ProjectileParameters.InitialScale.y, 1);
                Particles[index].startLifetime = m_currentBehaviour.ProjectileParameters.LifeTime;
                Particles[index].remainingLifetime = m_currentBehaviour.ProjectileParameters.LifeTime;
                Particles[index].startColor = UnityEngine.Color.white;
                Particles[index].rotation3D = new Vector3(0, 0, Mathf.Rad2Deg * Mathf.Atan2(y1, x1));

            }
            int InnerCount = (int)(m_currentBehaviour.ProjectileCount / 1.2f) - 2;
            for (int u = 0; u < InnerCount; u++)
            {
                x1 = Mathf.Lerp(LeftEdgePos.x, RightEdgePos.x, ((float)(u + 1)) / (InnerCount + 1));
                y1 = Mathf.Lerp(LeftEdgePos.y, RightEdgePos.y, ((float)(u + 1)) / (InnerCount + 1));

                int index = u + firstCornerIndex + 2 * m_currentBehaviour.ProjectileCount - 2;
                Particles[index].position = new Vector3(x1, y1);
                Particles[index].velocity = m_currentBehaviour.ProjectileParameters.InitialVelocityStrength * (m_currentBehaviour.CircleCenteredVelocity ? Particles[index].position : MedianDir) / m_currentBehaviour.CenterDistance;
                Particles[index].startSize3D = new Vector3(m_currentBehaviour.ProjectileParameters.InitialScale.x, m_currentBehaviour.ProjectileParameters.InitialScale.y, 1);
                Particles[index].startLifetime = m_currentBehaviour.ProjectileParameters.LifeTime;
                Particles[index].remainingLifetime = m_currentBehaviour.ProjectileParameters.LifeTime;
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
        float timer = float.PositiveInfinity;
        float timeSinceCoroutineStart = 0;
        Particle[] _copy;
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
                        float angle = Mathf.Rad2Deg * Mathf.Atan2(_closestPlayer.transform.position.y - transform.position.y, _closestPlayer.transform.position.x - transform.position.x);
                        system.transform.rotation = Quaternion.Euler(0, 0, angle);
                    }
                }
                //breaks when using multiple zones in a circle pattern
                if (_Zone.RotationFollowsAim)
                    transform.rotation = system.transform.rotation;

                timer += Time.deltaTime;
                timeSinceCoroutineStart += Time.deltaTime;
                if (_particles != null && timer > _Zone.SpawnFrequency && _Zone.patternType != EnemyProjectileSpawner.ShootZone.PatternType.Circle)
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
                        float angle = Mathf.Rad2Deg * Mathf.Atan2(_closestPlayer.transform.position.y - transform.position.y, _closestPlayer.transform.position.x - transform.position.x);
                        system.transform.rotation = Quaternion.Euler(0, 0, angle);

                    }
                }
                if (_Zone.RotationFollowsAim)
                    transform.rotation = system.transform.rotation;

                timer += Time.deltaTime;
                timeSinceCoroutineStart += Time.deltaTime;
                if (_particles != null && timer > _Zone.SpawnFrequency && _Zone.patternType != EnemyProjectileSpawner.ShootZone.PatternType.Circle)
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
                        float angle = Mathf.Rad2Deg * Mathf.Atan2(_closestPlayer.transform.position.y - transform.position.y, _closestPlayer.transform.position.x - transform.position.x);
                        system.transform.rotation = Quaternion.Euler(0, 0, angle);

                    }
                }
                if (_Zone.RotationFollowsAim)
                    transform.rotation = system.transform.rotation;

                timer += Time.deltaTime;
                timeSinceCoroutineStart += Time.deltaTime;
                if (_particles != null && timer > _Zone.SpawnFrequency && _Zone.patternType != EnemyProjectileSpawner.ShootZone.PatternType.Circle)
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
        system.Stop();
        ProjectilePool.Instance.ReturnToPoolLater(system);
        if (m_healthComp.Health > 0)
            NextPattern();
        else if(death || !ShootSettings.ProjectilePatterns.ContainsKey(EnemyProjectileSpawner.BehaviourChangeType.Death))
            Destroy(gameObject);
    }
    private void SetupParticleSystemParameters(ParticleSystem system)
    {
        //Render Module
        ParticleSystemRenderer particleSystemRenderer = system.GetComponent<ParticleSystemRenderer>();
        particleSystemRenderer.material = m_currentBehaviour.ProjectileParameters.Mat;
        particleSystemRenderer.renderMode = ParticleSystemRenderMode.Mesh;
        particleSystemRenderer.sortMode = ParticleSystemSortMode.None;
        particleSystemRenderer.alignment = ParticleSystemRenderSpace.World;
        //Main Module
        MainModule mainModule = system.main;
        mainModule.startColor = m_currentBehaviour.ProjectileParameters.InitialColor;
        mainModule.startSize3D = true;
        mainModule.startSizeX = m_currentBehaviour.ProjectileParameters.InitialScale.x;
        mainModule.startSizeY = m_currentBehaviour.ProjectileParameters.InitialScale.y;
        mainModule.startLifetime = m_currentBehaviour.ProjectileParameters.LifeTime;
        mainModule.startSpeed = m_currentBehaviour.ProjectileParameters.InitialVelocityStrength;
        mainModule.startColor = m_currentBehaviour.ProjectileParameters.InitialColor;
        mainModule.startRotation3D = true;
        mainModule.loop = true;
        mainModule.playOnAwake = true;
        mainModule.maxParticles = 10000;
        mainModule.simulationSpace = ParticleSystemSimulationSpace.World;

        MinMaxCurve RotZ = mainModule.startRotationZ;
        RotZ.constant = m_currentBehaviour.ProjectileParameters.InitialRotation * Mathf.Deg2Rad;
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
        SpriteModule.SetSprite(0, m_currentBehaviour.ProjectileParameters.Texture);

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
        if (m_currentBehaviour.ProjectileParameters.VariableScale)
        {
            ScaleModule.enabled = true;
            ScaleModule.size = m_currentBehaviour.ProjectileParameters.ScaleOverTime;
        }
        else
        {
            ScaleModule.enabled = false;
        }

        //RotationOverLifetime
        RotationOverLifetimeModule RotationModule = system.rotationOverLifetime;
        if (m_currentBehaviour.ProjectileParameters.VariableRotation)
        {
            RotationModule.enabled = true;
            RotationModule.separateAxes = true;
            RotationModule.z = m_currentBehaviour.ProjectileParameters.RotationOverTime;
        }
        else
        {
            RotationModule.enabled = false;
        }

        //ColorOverLifetime
        ColorOverLifetimeModule ColorModule = system.colorOverLifetime;
        if (m_currentBehaviour.ProjectileParameters.VariableColor)
        {
            ColorModule.enabled = true;
            ColorModule.color = m_currentBehaviour.ProjectileParameters.ColorOverTime;
        }
        else
        {
            ColorModule.enabled = false;
        }

        //VelocityOverLifetime
        VelocityOverLifetimeModule VelocityModule = system.velocityOverLifetime;
        if (m_currentBehaviour.ProjectileParameters.VariableVelocity)
        {
            VelocityModule.enabled = true;
            VelocityModule.space = ParticleSystemSimulationSpace.World;
            VelocityModule.speedModifier = m_currentBehaviour.ProjectileParameters.VelocityOverTime;
        }
        else
        {
            VelocityModule.speedModifier = new(1);
        }

        //Bullet curve
        if (m_currentBehaviour.ProjectileParameters.BulletCurve)
        {
            VelocityModule.enabled = true;
            VelocityModule.space = ParticleSystemSimulationSpace.World;
            VelocityModule.orbitalZ = m_currentBehaviour.ProjectileParameters.TrajectoryOverTime;
        }
        else
        {
            VelocityModule.orbitalZ = new();
        }
        if (!m_currentBehaviour.ProjectileParameters.BulletCurve && !m_currentBehaviour.ProjectileParameters.VariableVelocity)
        {
            VelocityModule.enabled = false;
        }
    }
}
