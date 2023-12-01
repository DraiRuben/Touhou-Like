using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    public static int EnemyCount;

    public bool IsBoss;
    public bool TriggerNextMovementBehaviour;
    [SerializeField] private PathTransitionType m_transitionType;
    [Space]
    public bool IsStationnaryAfterNPaths;
    [ShowIf(nameof(IsStationnaryAfterNPaths))]
    [Min(1)]
    public int NPathsBeforeStationnary;

    [ShowIf(nameof(ShowLoopPathChoices))][SerializeField] private bool LoopPathChoices;
    [SerializeField] private List<PathChoice> m_pathChoices;

    private int m_currentPathChoiceIndex;
    private Rigidbody2D m_rb;
    private EntityHealthHandler m_healthHandler;
    private EnemyFiringSystem m_firingSystem;
    private void Awake()
    {
        m_rb = GetComponent<Rigidbody2D>();
        m_healthHandler = GetComponent<EntityHealthHandler>();
        m_firingSystem = GetComponent<EnemyFiringSystem>();
    }
    private void OnEnable()
    {
        EnemyCount++;
    }
    private void OnDisable()
    {
        EnemyCount--;
    }
    private void Start()
    {
        StartCoroutine(Movement());
    }
    private void OnParticleCollision(GameObject other)
    {
        m_healthHandler.Health--;
        if (IsBoss)
        {
            GetShootOrigin(other).Score += 10;
        }
        if (m_healthHandler.Health <= 0)
        {
            GetShootOrigin(other).Score += m_healthHandler.MaxHealth*113;
        }
        GFXManager.Instance.DisplayEffect(transform.position, GFXManager.EffectType.Hit);
    }
    private PlayerInputReceiver GetShootOrigin(GameObject other)
    {
        if(other.transform.parent != null)
        {
            return other.transform.parent.GetComponent<PlayerInputReceiver>();
        }
        else
        {
            return PlayerManager.Instance.m_players[(int)other.transform.localScale.z-1].GetComponent<PlayerInputReceiver>();
        }
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            m_firingSystem.HasCollided = true;
            m_healthHandler.Health--;
            GFXManager.Instance.DisplayEffect(transform.position, GFXManager.EffectType.Explosion);
        }
    }
    private IEnumerator Movement()
    {
        List<Transform> Waypoints = GetWaypoints();
        if (Waypoints == null || Waypoints.Count <= 0) yield break;
        Vector2 currentWaypoint = Waypoints[0].position;
        float timer = 0;
        int visitedPathsCount = 0;
        int visitedWaypointsCount = 0;
        int currentWaypointIndex = 0;
        bool waitForNext = false;
        bool reachedWaypointsLimit = false;
        Vector2 MovementVector = currentWaypoint - (Vector2)transform.position;
        while (true)
        {
            if (!waitForNext || TriggerNextMovementBehaviour)
            {
                m_rb.velocity = MovementVector.normalized
                * (m_pathChoices[m_currentPathChoiceIndex].MovemementMultiplier.mode == ParticleSystemCurveMode.Constant ? m_pathChoices[m_currentPathChoiceIndex].MovemementMultiplier.constant : m_pathChoices[m_currentPathChoiceIndex].MovemementMultiplier.curve.Evaluate(timer) * m_pathChoices[m_currentPathChoiceIndex].MovemementMultiplier.curveMultiplier);
                timer += Time.fixedDeltaTime;
                if (Vector2.Distance(transform.position, currentWaypoint) < 0.1f || TriggerNextMovementBehaviour)
                {
                    timer = 0;
                    if (!TriggerNextMovementBehaviour)
                        visitedWaypointsCount++;
                    m_rb.velocity = Vector2.zero;
                    reachedWaypointsLimit = !(!m_pathChoices[m_currentPathChoiceIndex].IsStationnaryAfterNWaypoints || visitedWaypointsCount < m_pathChoices[m_currentPathChoiceIndex].NWaypointsBeforeStationnary);
                    if (!reachedWaypointsLimit && !TriggerNextMovementBehaviour && Waypoints.Count > 1 && currentWaypointIndex < Waypoints.Count - 1)
                    {
                        currentWaypointIndex++;
                        timer = 0;
                        currentWaypoint = Waypoints[currentWaypointIndex].position;
                        MovementVector = currentWaypoint - (Vector2)transform.position;
                        continue;
                    }
                    waitForNext = true;

                    if (Waypoints.Count == 1 || currentWaypointIndex >= Waypoints.Count - 1 || TriggerNextMovementBehaviour) //finished path
                    {

                        //if we didn't reach the N waypoints count limit or need to trigger the next behaviour at all costs
                        if ((!reachedWaypointsLimit && m_pathChoices[m_currentPathChoiceIndex].LoopThroughPath)
                            || (TriggerNextMovementBehaviour && m_pathChoices[m_currentPathChoiceIndex].RegenPathWithLoop))
                        {
                            TriggerNextMovementBehaviour = false;
                            visitedPathsCount++;
                            waitForNext = false;
                            currentWaypointIndex = 0;
                            if (m_pathChoices[m_currentPathChoiceIndex].RegenPathWithLoop)
                            {
                                Waypoints = GetWaypoints();
                                continue;
                            }
                        }
                        //if we didn't reach the N path count limit or need to trigger the next behaviour at all costs
                        else if ((!IsStationnaryAfterNPaths || ++visitedPathsCount < NPathsBeforeStationnary))
                        {
                            visitedPathsCount++;
                            visitedWaypointsCount = 0;
                            waitForNext = false;
                            currentWaypointIndex = 0;
                            if (m_currentPathChoiceIndex + 1 >= m_pathChoices.Count)
                            {
                                if (!TriggerNextMovementBehaviour && LoopPathChoices)
                                {
                                    m_currentPathChoiceIndex = 0;
                                }
                                else if (TriggerNextMovementBehaviour)
                                {
                                    m_currentPathChoiceIndex++;
                                    m_currentPathChoiceIndex %= m_pathChoices.Count;
                                    Waypoints = GetWaypoints();
                                }
                                else
                                {
                                    //we finished reading all paths and can't loop, so we don't need to run the coroutine anymore
                                    yield break;
                                }
                            }
                            else
                            {
                                TriggerNextMovementBehaviour = false;
                                m_currentPathChoiceIndex++;
                                Waypoints = GetWaypoints();
                            }
                        }
                    }
                    currentWaypoint = Waypoints[currentWaypointIndex].position;
                    MovementVector = currentWaypoint - (Vector2)transform.position;
                }
            }
            yield return new WaitForFixedUpdate();
        }
    }
    private List<Transform> GetWaypoints()
    {
        PathChoice currentPath = m_pathChoices[m_currentPathChoiceIndex];
        if (currentPath.RandomMovementPath)
        {
            if (currentPath.RandomPathLength)
                return EnemyWaypointManager.Instance.GetRandomPath();
            else
                return EnemyWaypointManager.Instance.GetRandomPath(currentPath.PathLength, currentPath.PathLengthConditionType);
        }
        else
        {
            return EnemyWaypointManager.Instance.GetPath(currentPath.PathIndex);
        }
    }
    [Serializable]
    public class PathChoice
    {
        [Header("Path Choice Parameters")]
        [ShowIf(nameof(ShowLoopPath))] public bool LoopThroughPath;
        [ShowIf(nameof(LoopThroughPath))] public bool RegenPathWithLoop;

        public bool RandomMovementPath;
        [HideIf(nameof(RandomMovementPath))] public int PathIndex;

        [Space]
        public bool RandomPathLength;
        [HideIf(nameof(RandomPathLength))] public EnemyWaypointManager.PathLengthConditionType PathLengthConditionType;
        [HideIf(nameof(RandomPathLength))] public int PathLength;

        [Space]
        public bool IsStationnaryAfterNWaypoints;
        [ShowIf(nameof(IsStationnaryAfterNWaypoints))]
        [Min(1)]
        public int NWaypointsBeforeStationnary;


        public ParticleSystem.MinMaxCurve MovemementMultiplier;

#if UNITY_EDITOR
        [HideInInspector] public bool ShowLoopPath;
#endif
    }

    public enum PathTransitionType
    {
        Automatic,
        Triggered
    }

#if UNITY_EDITOR
    [HideInInspector] public bool ShowLoopPathChoices;
    private void OnValidate()
    {
        ShowLoopPathChoices = m_transitionType == PathTransitionType.Automatic;
        if (m_transitionType == PathTransitionType.Automatic)
        {
            IsStationnaryAfterNPaths = false;
        }
        for (int i = 0; i < m_pathChoices.Count; i++)
        {
            m_pathChoices[i].ShowLoopPath = m_transitionType == PathTransitionType.Triggered;
            if (m_transitionType == PathTransitionType.Automatic)
            {
                m_pathChoices[i].LoopThroughPath = false;
            }
        }
    }
#endif
}
