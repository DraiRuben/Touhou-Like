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
            if (m_currentBehaviour.patternType == EnemyProjectileSpawner.ShootZone.PatternType.Circle)
            {
                if (m_currentBehaviour.ZoneCount > m_usedEmitters.Count)
                {
                    for (int i = m_usedEmitters.Count; i < m_currentBehaviour.ZoneCount; i++)
                        m_usedEmitters.Add(ProjectilePool.Instance.GetProjectileEmitter().GetComponent<ParticleSystem>());
                }
                for (int i = 0; i < m_currentBehaviour.ZoneCount; i++)
                {
                    m_usedEmitters[i].transform.rotation = Quaternion.Euler(0, 0, 360f - 360f / (i + 1));
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
            else if (m_currentBehaviour.patternType == EnemyProjectileSpawner.ShootZone.PatternType.Polygon)
            {
                if (m_usedEmitters.Count <= 0)
                {
                    m_usedEmitters.Add(ProjectilePool.Instance.GetProjectileEmitter().GetComponent<ParticleSystem>());
                }
                float angleSpan = Mathf.Abs(m_currentBehaviour.EndAngle - m_currentBehaviour.StartAngle);
                float angle;
                int verticeCount;
                bool isIncompleteAngle = false;
                if(angleSpan == 360f)
                {
                    verticeCount = m_currentBehaviour.Vertices;
                    angle = angleSpan / m_currentBehaviour.Vertices;
                }
                else
                {
                    verticeCount = m_currentBehaviour.Vertices +1;
                    isIncompleteAngle = true;
                    angle = angleSpan / (m_currentBehaviour.Vertices-1);
                }
                int particleCount = verticeCount * m_currentBehaviour.ProjectileCount - verticeCount;
                ParticleSystem.Particle[] Particles = new ParticleSystem.Particle[particleCount];
                Vector3 EdgePos = new();
                Vector3 NextEdgePos = new();
                Vector3 MedianDir = new Vector3(Mathf.Cos(angleSpan)/2, Mathf.Sin(angleSpan)/2);
                for (int i = 0; i < verticeCount; i++)
                {
                    if (isIncompleteAngle)
                    {
                        if (i == verticeCount - 1)
                        {
                            EdgePos = NextEdgePos;
                            NextEdgePos.x = Particles[0].position.x*10;
                            NextEdgePos.y = Particles[0].position.y*10;
                        }
                        else if(i==verticeCount - 2)
                        {
                            EdgePos = new Vector3(Mathf.Cos((angle * i + m_currentBehaviour.StartAngle) * Mathf.Deg2Rad), Mathf.Sin((angle * i + m_currentBehaviour.StartAngle) * Mathf.Deg2Rad));

                            NextEdgePos.x = Mathf.Lerp(EdgePos.x, Particles[0].position.x * 10, 0.5f);
                            NextEdgePos.y = Mathf.Lerp(EdgePos.y, Particles[0].position.y * 10, 0.5f);
                        }
                        else
                        {
                            NextEdgePos = new Vector3(Mathf.Cos((angle * (i + 1) + m_currentBehaviour.StartAngle) * Mathf.Deg2Rad), Mathf.Sin((angle * (i + 1) + m_currentBehaviour.StartAngle) * Mathf.Deg2Rad));
                            EdgePos = new Vector3(Mathf.Cos((angle * i + m_currentBehaviour.StartAngle) * Mathf.Deg2Rad), Mathf.Sin((angle * i + m_currentBehaviour.StartAngle) * Mathf.Deg2Rad));
                        }

                    }
                    else
                    {
                        NextEdgePos = new Vector3(Mathf.Cos((angle * (i + 1) + m_currentBehaviour.StartAngle) * Mathf.Deg2Rad), Mathf.Sin((angle * (i + 1) + m_currentBehaviour.StartAngle) * Mathf.Deg2Rad));
                        EdgePos = new Vector3(Mathf.Cos((angle * i + m_currentBehaviour.StartAngle) * Mathf.Deg2Rad), Mathf.Sin((angle * i + m_currentBehaviour.StartAngle) * Mathf.Deg2Rad));
                    }


                    Particles[i * (m_currentBehaviour.ProjectileCount - 1)].position = EdgePos/10;
                    Particles[i * (m_currentBehaviour.ProjectileCount - 1)].velocity = (m_currentBehaviour.CircleCenteredVelocity?EdgePos: MedianDir) * m_currentBehaviour.ProjectileParameters.InitialVelocityStrength;
                    Particles[i * (m_currentBehaviour.ProjectileCount - 1)].startSize3D = new Vector3(m_currentBehaviour.ProjectileParameters.InitialScale.x, m_currentBehaviour.ProjectileParameters.InitialScale.y, 1);
                    Particles[i * (m_currentBehaviour.ProjectileCount - 1)].startLifetime = m_currentBehaviour.ProjectileParameters.LifeTime;
                    Particles[i * (m_currentBehaviour.ProjectileCount - 1)].remainingLifetime = m_currentBehaviour.ProjectileParameters.LifeTime;
                    Particles[i * (m_currentBehaviour.ProjectileCount - 1)].startColor = new Color32(255, 255, 255, 255);
                    Particles[i * (m_currentBehaviour.ProjectileCount - 1)].rotation3D = Quaternion.Euler(90, 0, angle * i + m_currentBehaviour.StartAngle).eulerAngles;

                    //line
                    for (int u = 0; u < m_currentBehaviour.ProjectileCount - 2; u++)
                    {
                        float x = Mathf.Lerp(EdgePos.x, NextEdgePos.x, (float)(u+1) / (m_currentBehaviour.ProjectileCount -1));
                        float y = Mathf.Lerp(EdgePos.y, NextEdgePos.y, (float)(u+1) / (m_currentBehaviour.ProjectileCount -1));
                        Particles[u + i * (m_currentBehaviour.ProjectileCount - 1) + 1].position = new Vector3(x, y)/10;
                        Particles[u + i * (m_currentBehaviour.ProjectileCount - 1) + 1].velocity = (m_currentBehaviour.CircleCenteredVelocity ? Particles[u + i * (m_currentBehaviour.ProjectileCount - 1) + 1].position*10:MedianDir) *m_currentBehaviour.ProjectileParameters.InitialVelocityStrength;
                        Particles[u + i * (m_currentBehaviour.ProjectileCount - 1) + 1].startSize3D = new Vector3(m_currentBehaviour.ProjectileParameters.InitialScale.x, m_currentBehaviour.ProjectileParameters.InitialScale.y, 1);
                        Particles[u + i * (m_currentBehaviour.ProjectileCount - 1) + 1].startLifetime = m_currentBehaviour.ProjectileParameters.LifeTime;
                        Particles[u + i * (m_currentBehaviour.ProjectileCount - 1) + 1].remainingLifetime = m_currentBehaviour.ProjectileParameters.LifeTime;
                        Particles[u + i * (m_currentBehaviour.ProjectileCount - 1) + 1].startColor = new Color32(255, 255, 255, 255);
                        Particles[u + i * (m_currentBehaviour.ProjectileCount - 1) + 1].rotation3D = Quaternion.Euler(90, 0, angle * i + angle * (u + 1) / (i+1)).eulerAngles;
                    }                                                                                             
                }
                SetupParticleSystemParameters(m_usedEmitters[0]);
                var ShapeModule = m_usedEmitters[0].shape;
                ShapeModule.enabled = false;
                m_usedEmitters[0].SetParticles(Particles, Particles.Length);
                m_usedEmitters[0].Emit(Particles.Length);
                yield return null;
                m_usedEmitters[0].SetParticles(Particles, Particles.Length);

            }
            if (m_currentBehaviour.AimAtClosestPlayer)
            {
                GameObject _closestPlayer = PlayerManager.Instance.GetClosestPlayer(transform.position);
                if(_closestPlayer != null) 
                {
                    float angle = Mathf.Atan2(_closestPlayer.transform.position.y - transform.position.y, _closestPlayer.transform.position.x - transform.position.x) * Mathf.Rad2Deg;
                    for (int i = 0; i < m_currentBehaviour.ZoneCount; i++)
                    {
                        m_usedEmitters[i].transform.rotation = Quaternion.Euler(0, 0, angle);
                    }
                }
                
            }
            else
            {
                for (int i = 0; i < m_currentBehaviour.ZoneCount; i++)
                {
                    m_usedEmitters[i].transform.rotation = Quaternion.Euler(0, 0, 360f - 360f / (i + 1));
                }
            }
            yield break;
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
