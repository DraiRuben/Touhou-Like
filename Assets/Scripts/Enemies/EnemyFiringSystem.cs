using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.VirtualTexturing;
using static UnityEditor.Experimental.GraphView.GraphView;

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
    private void Awake()
    {
        m_healthComp = GetComponent<EntityHealthHandler>();
        m_healthComp.OnHealthChanged.AddListener(() => IsInPriorityShootingBehaviour = true);
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
        EnemyProjectileSpawner.BehaviourChangeType routineType = m_currentBehaviourType;
        if (!m_currentBehaviour.AimAtClosestPlayer)
        {
            transform.rotation = Quaternion.Euler(0, 0, m_currentBehaviour.ShootRotation);
        }
        while (routineType == m_currentBehaviourType)
        {
            
            /*if (m_currentBehaviour.ProjectileCount > m_usedEmitters.Count)
            {
                for(int i = m_usedEmitters.Count; i < m_currentBehaviour.ProjectileCount; i++)
                m_usedEmitters.Add(ProjectilePool.Instance.GetProjectileEmitter().GetComponent<ParticleSystem>());
            }*/
            if(m_currentBehaviour.patternType == EnemyProjectileSpawner.ShootZone.PatternType.Circle)
            {
                if (m_currentBehaviour.ZoneCount > m_usedEmitters.Count)
                {
                    for (int i = m_usedEmitters.Count; i < m_currentBehaviour.ZoneCount; i++)
                        m_usedEmitters.Add(ProjectilePool.Instance.GetProjectileEmitter().GetComponent<ParticleSystem>());
                }
                for (int i = 0; i < m_currentBehaviour.ZoneCount; i++)
                {
                    m_usedEmitters[i].transform.rotation = Quaternion.Euler(0, 0, 360f- 360f /(i+1));
                    //Shape Module
                    var ShapeModule = m_usedEmitters[i].shape;
                    ShapeModule.enabled = true;
                    ShapeModule.shapeType = ParticleSystemShapeType.Cone;
                    ShapeModule.radius = 0.01f;
                    ShapeModule.arc = Mathf.Abs(m_currentBehaviour.EndAngle - m_currentBehaviour.StartAngle);
                    ShapeModule.arcMode = ParticleSystemShapeMultiModeValue.BurstSpread;
                    m_usedEmitters[i].transform.position = transform.position;

                    //Burst Emission
                    ParticleSystem.Burst burst = new ParticleSystem.Burst();
                    burst.count = m_currentBehaviour.ProjectileCount;
                    burst.repeatInterval = m_currentBehaviour.SpawnFrequency;
                    burst.cycleCount = 0;
                    m_usedEmitters[i].emission.SetBurst(0, burst);

                    //Emission Module
                    var EmissionModule = m_usedEmitters[i].emission;
                    EmissionModule.rateOverDistance = 0;
                    EmissionModule.rateOverTime = 0;

                    SetupParticleSystemParameters(m_usedEmitters[i]);
                }
            }
            else if(m_currentBehaviour.patternType == EnemyProjectileSpawner.ShootZone.PatternType.Polygon)
            {
                ParticleSystem.Particle[] Particles = new ParticleSystem.Particle[m_currentBehaviour.Vertices + m_currentBehaviour.Vertices * m_currentBehaviour.ProjectileCount];
                float angle = Mathf.Abs(m_currentBehaviour.EndAngle - m_currentBehaviour.StartAngle) / m_currentBehaviour.Vertices;
                for (int i = 0; i < m_currentBehaviour.Vertices; i++)
                {
                    //edge
                    Vector3 EdgePos = new Vector3(Mathf.Cos((angle * i + m_currentBehaviour.StartAngle) * Mathf.Deg2Rad), Mathf.Sin((angle * i + m_currentBehaviour.StartAngle) * Mathf.Deg2Rad));
                    Vector3 NextEdgePos = new Vector3(Mathf.Cos((angle * (i + 1) + m_currentBehaviour.StartAngle) * Mathf.Deg2Rad), Mathf.Sin((angle * (i + 1) + m_currentBehaviour.StartAngle) * Mathf.Deg2Rad));
                    Vector3 Vertice = NextEdgePos - EdgePos;
                    Particles[i].position = EdgePos;
                    //line
                    for (int u = m_currentBehaviour.Vertices; u < m_currentBehaviour.ProjectileCount; u++)
                    {

                    }
                }
                

                SetupParticleSystemParameters(m_usedEmitters[0]);
            }
            if (m_currentBehaviour.AimAtClosestPlayer)
            {
                GameObject _closestPlayer = PlayerManager.Instance.GetClosestPlayer(transform.position);
                float angle = Mathf.Atan2(_closestPlayer.transform.position.y - transform.position.y, _closestPlayer.transform.position.x - transform.position.x) * Mathf.Rad2Deg;
                for (int i = 0; i < m_currentBehaviour.ZoneCount; i++)
                {
                    m_usedEmitters[i].transform.rotation = Quaternion.Euler(0, 0, 360f - 360f / (i + 1));
                }
            }
            else
            {
                for (int i = 0; i < m_currentBehaviour.ZoneCount; i++)
                {
                    m_usedEmitters[i].transform.rotation = Quaternion.Euler(0, 0, 360f - 360f / (i + 1));
                }
            }
            yield return new WaitForSeconds(m_currentBehaviour.SpawnFrequency);
        }
    }
    private void StopEmission()
    {
        if(m_usedEmitters.Count>0)
        for(int i = 0; i < m_usedEmitters.Count; i++)
        {
            m_usedEmitters[i].Stop();
        }
    }
    private void SetupParticleSystemParameters(ParticleSystem system)
    {
        //Render Module
        ParticleSystemRenderer particleSystemRenderer = system.GetComponent<ParticleSystemRenderer>();
        particleSystemRenderer.material = m_currentBehaviour.ProjectileParameters.Mat;
        particleSystemRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        particleSystemRenderer.alignment = ParticleSystemRenderSpace.Velocity;

        //Main Module
        var mainModule = system.main;
        mainModule.startColor = m_currentBehaviour.ProjectileParameters.InitialColor;
        mainModule.startSize3D = true;
        mainModule.startSizeX = m_currentBehaviour.ProjectileParameters.InitialScale.x;
        mainModule.startSizeY = m_currentBehaviour.ProjectileParameters.InitialScale.y;
        mainModule.startLifetime = m_currentBehaviour.ProjectileParameters.LifeTime;
        mainModule.startSpeed = m_currentBehaviour.ProjectileParameters.InitialVelocityStrength;
        mainModule.startColor = m_currentBehaviour.ProjectileParameters.InitialColor;
        mainModule.startRotation3D = true;
        var RotX = mainModule.startRotationX;
        RotX.constant = 90;

        var RotZ = mainModule.startRotationZ;
        RotZ.constant = m_currentBehaviour.ProjectileParameters.InitialRotation;
        mainModule.maxParticles = 1000;
        mainModule.simulationSpace = ParticleSystemSimulationSpace.World;


        //Emitter shape module
        var EmitterModule = system.shape;
        EmitterModule.enabled = true;

        //Sprite Module
        var SpriteModule = system.textureSheetAnimation;
        SpriteModule.enabled = true;
        SpriteModule.mode = ParticleSystemAnimationMode.Sprites;
        SpriteModule.SetSprite(0,m_currentBehaviour.ProjectileParameters.Texture);

        //Collision module
        var CollisionModule = system.collision;
        CollisionModule.enabled = true;
        CollisionModule.type = ParticleSystemCollisionType.World;
        CollisionModule.mode = ParticleSystemCollisionMode.Collision2D;
        CollisionModule.lifetimeLoss = 1;
        CollisionModule.collidesWith = LayerMask.GetMask("Player");
        CollisionModule.sendCollisionMessages = true;

        //ScaleOverLifetime
        var ScaleModule = system.sizeOverLifetime;
        var ScaleModuleCurves = ScaleModule.size;
        if (m_currentBehaviour.ProjectileParameters.VariableScale)
        {
            ScaleModule.enabled = true;
            ScaleModuleCurves.mode = ParticleSystemCurveMode.Curve;
            ScaleModuleCurves.curveMultiplier = m_currentBehaviour.ProjectileParameters.ScaleOverTime.keys.Max(x => x.value);
            ScaleModuleCurves.curve = m_currentBehaviour.ProjectileParameters.ScaleOverTime;
        }
        else
        {
            ScaleModule.enabled = false;

        }
        ScaleModule.size = ScaleModuleCurves;

        //RotationOverLifetime
        var RotationModule = system.rotationOverLifetime;
        var RotationModuleCurves = RotationModule.z;
        if (m_currentBehaviour.ProjectileParameters.VariableRotation)
        {
            RotationModule.enabled = true;
            RotationModule.separateAxes = true;
            RotationModuleCurves.curveMultiplier = m_currentBehaviour.ProjectileParameters.RotationOverTime.keys.Max(x => x.value);
            RotationModuleCurves.curve = m_currentBehaviour.ProjectileParameters.RotationOverTime;
            RotationModule.z = RotationModuleCurves;
        }
        else
        {
            RotationModule.enabled = false;
        }

        //ColorOverLifetime
        var ColorModule = system.colorOverLifetime;
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
        var VelocityModule = system.velocityOverLifetime;
        var VelocityModuleCurves = VelocityModule.speedModifier;
        if (m_currentBehaviour.ProjectileParameters.VariableVelocity)
        {
            VelocityModule.enabled = true;
            VelocityModuleCurves.mode = ParticleSystemCurveMode.Curve;
            VelocityModuleCurves.curveMultiplier = m_currentBehaviour.ProjectileParameters.VelocityOverTime.keys.Max(x => x.value);
            VelocityModuleCurves.curve = m_currentBehaviour.ProjectileParameters.VelocityOverTime;
        }
        else
        {
            VelocityModule.enabled = false;
        }



    }
}
