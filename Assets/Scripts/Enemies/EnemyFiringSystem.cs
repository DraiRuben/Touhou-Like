using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.ParticleSystem;

[RequireComponent(typeof(EntityHealthHandler))]
public class EnemyFiringSystem : MonoBehaviour
{
    [SerializeField] private EnemyProjectileSpawner ShootSettings;
    private List<ParticleSystem> m_usedEmitters = new();
    private EntityHealthHandler m_healthComp;
    private EnemyProjectileSpawner.ShootZone m_currentBehaviour;
    private int m_nextBehaviourIndex;
    private List<int> m_alreadyUsedPatterns = new();
    private EnemyProjectileSpawner.BehaviourChangeType m_currentBehaviourType;
    private Transform m_transform;
    private void Awake()
    {
        m_healthComp = GetComponent<EntityHealthHandler>();
        m_healthComp.OnHealthChanged.AddListener(() => IsInPriorityShootingBehaviour = true);
        m_healthComp.OnDeath.AddListener(() => IsInPriorityShootingBehaviour = true);
        m_transform = transform;
    }
    private void Start()
    {
        NextPattern();
    }
    private bool IsInPriorityShootingBehaviour;
    private void NextPattern()
    {
        if (!IsInPriorityShootingBehaviour) //if we aren't in a behaviour for collision or death since those ones interrupt any other pattern
        {
            //firstly check if we can trigger a health pattern, else simply do a timed pattern
            if (ShootSettings.ProjectilePatterns.ContainsKey(EnemyProjectileSpawner.BehaviourChangeType.Life))
            {
                for (int i = 0; i < ShootSettings.ProjectilePatterns[EnemyProjectileSpawner.BehaviourChangeType.Life].ShootZones.Count; i++)
                {
                    if (m_currentBehaviourType != EnemyProjectileSpawner.BehaviourChangeType.Life)
                    {
                        m_nextBehaviourIndex = 0;
                        m_alreadyUsedPatterns.Clear();
                    }
                    if (m_healthComp.Health >= ShootSettings.ProjectilePatterns[EnemyProjectileSpawner.BehaviourChangeType.Life].ShootZones[i].BehaviourExitValue)
                    {
                        m_currentBehaviour = ShootSettings.ProjectilePatterns[EnemyProjectileSpawner.BehaviourChangeType.Life].ShootZones[i];
                        m_nextBehaviourIndex = i;
                        m_currentBehaviourType = EnemyProjectileSpawner.BehaviourChangeType.Life;
                        StartCoroutine(ShootRoutine());
                        return;
                    }
                }
            }
            //if we reached there, it means we don't have any usable health patters so we can check for time patterns
            if (ShootSettings.ProjectilePatterns.ContainsKey(EnemyProjectileSpawner.BehaviourChangeType.Time))
            {
                ChooseNewBehaviour(EnemyProjectileSpawner.BehaviourChangeType.Time);
                return;
            }
        }
        else
        {
            if (ShootSettings.ProjectilePatterns.ContainsKey(EnemyProjectileSpawner.BehaviourChangeType.Collision)) //damaged
            {
                ChooseNewBehaviour(EnemyProjectileSpawner.BehaviourChangeType.Collision);
            }
            else if (ShootSettings.ProjectilePatterns.ContainsKey(EnemyProjectileSpawner.BehaviourChangeType.Death))//death
            {
                ChooseNewBehaviour(EnemyProjectileSpawner.BehaviourChangeType.Death);
            }
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
        StartCoroutine(ShootRoutine());
        m_currentBehaviourType = _newBehaviourType;
    }

    private IEnumerator ShootRoutine()
    {
        StopEmission();
        if (m_currentBehaviour.patternType == EnemyProjectileSpawner.ShootZone.PatternType.Circle)
        {
            if (m_currentBehaviour.ZoneCount > m_usedEmitters.Count)
            {
                for (int i = m_usedEmitters.Count; i < m_currentBehaviour.ZoneCount; i++)
                    m_usedEmitters.Add(ProjectilePool.Instance.GetProjectileEmitter().GetComponent<ParticleSystem>());
            }
            else if (m_usedEmitters.Count > m_currentBehaviour.ZoneCount)
            {
                ReturnUnsusedSystem(m_currentBehaviour.ZoneCount);
            }
            for (int i = 0; i < m_currentBehaviour.ZoneCount; i++)
            {
                m_usedEmitters[i].transform.rotation = Quaternion.Euler(0, 0, 360f - 360f / (i + 1));

                SetupParticleSystemParameters(m_usedEmitters[i]);

                //Shape Module
                ShapeModule ShapeModule = m_usedEmitters[i].shape;
                ShapeModule.enabled = true;
                ShapeModule.shapeType = ParticleSystemShapeType.Cone;
                ShapeModule.radius = 0.01f;
                ShapeModule.arc = Mathf.Abs(m_currentBehaviour.EndAngle - m_currentBehaviour.StartAngle);
                ShapeModule.arcMode = ParticleSystemShapeMultiModeValue.BurstSpread;
                m_usedEmitters[i].transform.position = transform.position;

                //Burst Emission
                Burst[] burst = new Burst[1];
                burst[0].count = m_currentBehaviour.ProjectileCount;
                burst[0].repeatInterval = m_currentBehaviour.SpawnFrequency;
                burst[0].cycleCount = 0;
                m_usedEmitters[i].emission.SetBursts(burst);

                //Emission Module
                EmissionModule EmissionModule = m_usedEmitters[i].emission;
                EmissionModule.rateOverDistance = 0;
                EmissionModule.rateOverTime = 0;

                ParticleSystemRenderer particleSystemRenderer = m_usedEmitters[i].GetComponent<ParticleSystemRenderer>();
                particleSystemRenderer.alignment = ParticleSystemRenderSpace.Velocity;
                particleSystemRenderer.renderMode = ParticleSystemRenderMode.Billboard;
                particleSystemRenderer.sortMode = ParticleSystemSortMode.OldestInFront;

                StartCoroutine(EmissionRoutine());
            }
        }
        else if (m_currentBehaviour.patternType == EnemyProjectileSpawner.ShootZone.PatternType.Polygon)
        {
            if (m_usedEmitters.Count <= 0)
            {
                m_usedEmitters.Add(ProjectilePool.Instance.GetProjectileEmitter().GetComponent<ParticleSystem>());
            }
            else if (m_usedEmitters.Count > 1)
            {
                ReturnUnsusedSystem(1);
            }
            SetupParticleSystemParameters(m_usedEmitters[0]);
            Particle[] Particles = ComputePolygon();

            ShapeModule ShapeModule = m_usedEmitters[0].shape;
            ShapeModule.enabled = false;

            MainModule MainModule = m_usedEmitters[0].main;
            MainModule.loop = false;
            MainModule.playOnAwake = false;

            EmissionModule EmissionModule = m_usedEmitters[0].emission;
            EmissionModule.enabled = false;

            StartCoroutine(EmissionRoutine(Particles));
        }
        else if (m_currentBehaviour.patternType == EnemyProjectileSpawner.ShootZone.PatternType.Star)
        {
            if (m_usedEmitters.Count <= 0)
            {
                m_usedEmitters.Add(ProjectilePool.Instance.GetProjectileEmitter().GetComponent<ParticleSystem>());
            }
            else if (m_usedEmitters.Count > 1)
            {
                ReturnUnsusedSystem(1);
            }
            SetupParticleSystemParameters(m_usedEmitters[0]);

            Particle[] Particles = ComputeStar();

            ShapeModule ShapeModule = m_usedEmitters[0].shape;
            ShapeModule.enabled = false;

            MainModule MainModule = m_usedEmitters[0].main;
            MainModule.loop = false;
            MainModule.playOnAwake = false;
            EmissionModule EmissionModule = m_usedEmitters[0].emission;
            EmissionModule.enabled = false;

            StartCoroutine(EmissionRoutine(Particles));
        }
        if (m_currentBehaviour.ZoneCount > 1)
            for (int i = 1; i < m_currentBehaviour.ZoneCount; i++)
            {
                m_usedEmitters[i].transform.rotation = Quaternion.Euler(0, 0, i * (-360f / m_currentBehaviour.ZoneCount));
            }

        yield break;

    }
    private void StopEmission()
    {
        if (m_usedEmitters.Count > 0)
            for (int i = 0; i < m_usedEmitters.Count; i++)
            {
                m_usedEmitters[i].Stop();
            }
    }
    //called on enemy death or pattern change
    private void ReturnUnsusedSystem(int usedSystems)
    {
        for (int i = usedSystems; i < m_usedEmitters.Count; i++)
        {
            ProjectilePool.Instance.ReturnToPool(m_usedEmitters[i].gameObject);
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
            Particles[index].velocity = m_currentBehaviour.ProjectileParameters.InitialVelocityStrength / m_currentBehaviour.CenterDistance * (m_currentBehaviour.CircleCenteredVelocity ? EdgePos : AimDir);
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
                Particles[index].velocity = m_currentBehaviour.ProjectileParameters.InitialVelocityStrength / m_currentBehaviour.CenterDistance * (m_currentBehaviour.CircleCenteredVelocity ? Particles[index].position : AimDir);
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
        int particleCount = m_currentBehaviour.Limbs * ((m_currentBehaviour.ProjectileCount - 1) * 2 + m_currentBehaviour.ProjectileCount - 2);
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
            Particles[firstCornerIndex].velocity = m_currentBehaviour.ProjectileParameters.InitialVelocityStrength * (m_currentBehaviour.CircleCenteredVelocity ? EdgePos : MedianDir);
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
                Particles[index].velocity = m_currentBehaviour.ProjectileParameters.InitialVelocityStrength * (m_currentBehaviour.CircleCenteredVelocity ? Particles[index].position : MedianDir);
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
                Particles[index].velocity = m_currentBehaviour.ProjectileParameters.InitialVelocityStrength * (m_currentBehaviour.CircleCenteredVelocity ? Particles[index].position : MedianDir);
                Particles[index].startSize3D = new Vector3(m_currentBehaviour.ProjectileParameters.InitialScale.x, m_currentBehaviour.ProjectileParameters.InitialScale.y, 1);
                Particles[index].startLifetime = m_currentBehaviour.ProjectileParameters.LifeTime;
                Particles[index].remainingLifetime = m_currentBehaviour.ProjectileParameters.LifeTime;
                Particles[index].startColor = UnityEngine.Color.white;
                Particles[index].rotation3D = new Vector3(0, 0, Mathf.Rad2Deg * Mathf.Atan2(y1, x1));

            }
            for (int u = 0; u < m_currentBehaviour.ProjectileCount - 2; u++)
            {
                x1 = Mathf.Lerp(LeftEdgePos.x, RightEdgePos.x, (float)(u + 1) / (m_currentBehaviour.ProjectileCount - 1));
                y1 = Mathf.Lerp(LeftEdgePos.y, RightEdgePos.y, (float)(u + 1) / (m_currentBehaviour.ProjectileCount - 1));

                int index = u + firstCornerIndex + 2 * m_currentBehaviour.ProjectileCount - 2;
                Particles[index].position = new Vector3(x1, y1);
                Particles[index].velocity = m_currentBehaviour.ProjectileParameters.InitialVelocityStrength * (m_currentBehaviour.CircleCenteredVelocity ? Particles[index].position : MedianDir);
                Particles[index].startSize3D = new Vector3(m_currentBehaviour.ProjectileParameters.InitialScale.x, m_currentBehaviour.ProjectileParameters.InitialScale.y, 1);
                Particles[index].startLifetime = m_currentBehaviour.ProjectileParameters.LifeTime;
                Particles[index].remainingLifetime = m_currentBehaviour.ProjectileParameters.LifeTime;
                Particles[index].startColor = UnityEngine.Color.white;
                Particles[index].rotation3D = new Vector3(0, 0, Mathf.Rad2Deg * Mathf.Atan2(y1, x1));

            }

        }
        return Particles;
    }
    private IEnumerator EmissionRoutine(Particle[] _particles = null)
    {
        EnemyProjectileSpawner.BehaviourChangeType routineType = m_currentBehaviourType;
        float timer = float.PositiveInfinity;
        float timeSinceCoroutineStart = 0;
        Particle[] _copy;
        while (routineType == m_currentBehaviourType && (m_currentBehaviour.InfiniteDuration || timeSinceCoroutineStart < m_currentBehaviour.BehaviourExitValue))
        {
            for (int i = 0; i < m_usedEmitters.Count; i++)
            {
                m_usedEmitters[i].transform.position = m_transform.position;
                if (m_currentBehaviour.Spin)
                {
                    m_usedEmitters[i].transform.rotation = Quaternion.Euler(0, 0, (m_usedEmitters[i].transform.rotation.eulerAngles.z + m_currentBehaviour.SpinSpeed * Time.deltaTime) % 360f);
                }
                else if (m_currentBehaviour.AimAtClosestPlayer)
                {
                    GameObject _closestPlayer = PlayerManager.Instance.GetClosestPlayer(transform.position);
                    if (_closestPlayer != null)
                    {
                        float angle = Mathf.Rad2Deg * Mathf.Atan2(_closestPlayer.transform.position.y - transform.position.y, _closestPlayer.transform.position.x - transform.position.x);
                        m_usedEmitters[i].transform.rotation = Quaternion.Euler(0, 0, angle);

                    }
                }
            }
            timer += Time.deltaTime;
            timeSinceCoroutineStart += Time.deltaTime;
            if (_particles != null && timer > m_currentBehaviour.SpawnFrequency && m_currentBehaviour.patternType != EnemyProjectileSpawner.ShootZone.PatternType.Circle)
            {
                timer = 0;

                _copy = _particles.ToArray();
                for (int i = 0; i < _copy.Length; i++)
                {
                    Vector3 newPos = _copy[i].position.x * m_usedEmitters[0].transform.right + _copy[i].position.y * m_usedEmitters[0].transform.up;
                    _copy[i].velocity = m_currentBehaviour.ProjectileParameters.InitialVelocityStrength * (m_currentBehaviour.CircleCenteredVelocity ? newPos : m_usedEmitters[0].transform.right);
                    _copy[i].rotation3D = new(0, 0, Mathf.Rad2Deg * Mathf.Atan2(newPos.normalized.y, newPos.normalized.x));
                    newPos += m_usedEmitters[0].transform.position;
                    _copy[i].position = new(newPos.x, newPos.y, 0);

                }
                m_usedEmitters[0].SetParticles(_copy, _copy.Length, m_usedEmitters[0].particleCount);
                if (m_usedEmitters[0].isStopped) m_usedEmitters[0].Play();
                yield return null;
                continue;
            }
            yield return null;
        }
        NextPattern();
    }
    private void SetupParticleSystemParameters(ParticleSystem system)
    {
        //Render Module
        ParticleSystemRenderer particleSystemRenderer = system.GetComponent<ParticleSystemRenderer>();
        particleSystemRenderer.material = m_currentBehaviour.ProjectileParameters.Mat;
        particleSystemRenderer.alignment = ParticleSystemRenderSpace.Facing;
        particleSystemRenderer.renderMode = ParticleSystemRenderMode.Mesh;
        particleSystemRenderer.sortMode = ParticleSystemSortMode.None;

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
        mainModule.maxParticles = (int)(m_currentBehaviour.ProjectileCount / m_currentBehaviour.SpawnFrequency * 1.5f);
        mainModule.simulationSpace = ParticleSystemSimulationSpace.World;

        MinMaxCurve RotZ = mainModule.startRotationZ;
        RotZ.constant = m_currentBehaviour.ProjectileParameters.InitialRotation * Mathf.Deg2Rad;
        mainModule.startRotationZ = RotZ;

        //Emitter shape module
        ShapeModule EmitterModule = system.shape;
        EmitterModule.enabled = true;

        //Emissiob module
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
            VelocityModule.speedModifier = m_currentBehaviour.ProjectileParameters.VelocityOverTime;
        }
        else
        {
            VelocityModule.enabled = false;
        }


    }
}
