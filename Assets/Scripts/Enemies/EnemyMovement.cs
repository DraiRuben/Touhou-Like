using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    private EntityHealthHandler m_healthHandler;
    private EnemyFiringSystem m_weapon;
    private Rigidbody2D m_rb;

    public bool TriggerNextMovementBehaviour;

    private int m_currentPathChoiceIndex;
    [SerializeField] private PathTransitionType m_transitionType;
    [Space]
    public bool IsStationnaryAfterNPaths;
    [ShowCondition("IsStationnaryAfterNPaths")]
    [Min(1)]
    public int NPathsBeforeStationnary;


    [ShowCondition("ShowLoopPathChoices")][SerializeField] private bool LoopPathChoices;
    [SerializeField] private List<PathChoice> m_pathChoices;
    private void Awake()
    {
        m_healthHandler = GetComponent<EntityHealthHandler>();
        m_weapon = GetComponent<EnemyFiringSystem>();
        m_rb = GetComponent<Rigidbody2D>();
    }

    private IEnumerator Movement()
    {
        List<Transform> Waypoints = GetWaypoints();
        if (Waypoints == null || Waypoints.Count<=0) yield break;
        Vector2 currentWaypoint = Waypoints[0].position;
        float timer = 0;
        int visitedPathsCount = 0;
        int visitedWaypointsCount = 0;
        int currentWaypointIndex = 0;
        while (true)
        {

            timer += Time.fixedDeltaTime;
            if (Vector2.Distance(transform.position, currentWaypoint) < 0.1f || TriggerNextMovementBehaviour)
            {
                currentWaypointIndex++;
                visitedWaypointsCount++;
                if (currentWaypointIndex >= Waypoints.Count || TriggerNextMovementBehaviour) //finished path
                {
                    //if we didn't reach the N waypoints count limit or need to trigger the next behaviour at all costs
                    if (((!m_pathChoices[m_currentPathChoiceIndex].IsStationnaryAfterNWaypoints || visitedWaypointsCount <m_pathChoices[m_currentPathChoiceIndex].NWaypointsBeforeStationnary)&& m_pathChoices[m_currentPathChoiceIndex].LoopThroughPath)
                        || TriggerNextMovementBehaviour)
                    {
                        currentWaypointIndex = 0;
                        visitedPathsCount++;
                        //if we didn't reach the N path count limit or need to trigger the next behaviour at all costs
                        if((!IsStationnaryAfterNPaths || visitedPathsCount < NPathsBeforeStationnary )|| TriggerNextMovementBehaviour)
                        {
                            if(m_currentPathChoiceIndex+1 >= Waypoints.Count && LoopPathChoices)
                            {
                                m_currentPathChoiceIndex = 0;
                            }
                            else
                            {
                                //we finished reading all paths and can't loop, so we don't need to run the coroutine anymore
                                yield break;
                            }
                        }
                        if (m_pathChoices[m_currentPathChoiceIndex].RegenPathWithLoop)
                        {
                            m_currentPathChoiceIndex++;
                            Waypoints = GetWaypoints();
                        }
                    }
                }
                currentWaypoint = Waypoints[currentWaypointIndex].position;
            }
            yield return new WaitForFixedUpdate();
        }
    }
    private List<Transform> GetWaypoints()
    {
        PathChoice currentPath = m_pathChoices[m_currentPathChoiceIndex];
        if (currentPath.RandomMovementPath)
        {
            if(currentPath.RandomPathLength)
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
        [ShowCondition("ShowLoopPath")] public bool LoopThroughPath;
        [ShowCondition("LoopThroughPath")] public bool RegenPathWithLoop;

        public bool RandomMovementPath;
        [HideCondition("RandomMovementPath")]public int PathIndex;

        [Space]
        public bool RandomPathLength;
        [HideCondition("RandomPathLength")] public EnemyWaypointManager.PathLengthConditionType PathLengthConditionType;
        [HideCondition("RandomPathLength")] public int PathLength;

        [Space]
        public bool IsStationnaryAfterNWaypoints;
        [ShowCondition("IsStationnaryAfterNWaypoints")]
        [Min(1)]
        public int NWaypointsBeforeStationnary;

        [Space]
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
        if(m_transitionType == PathTransitionType.Automatic)
        {
            IsStationnaryAfterNPaths = false;
        }
        for(int i = 0; i < m_pathChoices.Count; i++)
        {
            m_pathChoices[i].ShowLoopPath = m_transitionType == PathTransitionType.Triggered;
            if(m_transitionType == PathTransitionType.Automatic)
            {
                m_pathChoices[i].LoopThroughPath = false;
            }
        }
    }
#endif
}
