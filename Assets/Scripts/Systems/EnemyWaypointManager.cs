using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnemyWaypointManager : MonoBehaviour
{
    [SerializeField] private List<WayPointsHolder> Waypoints;
    public static EnemyWaypointManager Instance;
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    [Serializable]
    public struct WayPointsHolder
    {
        public string Label;
        public List<Transform> points;
    }
    public List<Transform> GetPath(int index)
    {
        if (index < Waypoints.Count)
            return Waypoints[index].points;
        else return null;
    }
    public List<Transform> GetPath(int pathIndex, int pathLength, PathLengthConditionType conditionType = PathLengthConditionType.Equal)
    {
        List<WayPointsHolder> validPaths = GetValidPaths(pathLength, conditionType);
        if (validPaths.Count > 0 && pathIndex < validPaths.Count)
            return validPaths[pathIndex].points;
        else return null;
    }
    public Transform GetWaypoint(int pathIndex, int waypointIndex)
    {
        if (pathIndex < Waypoints.Count && waypointIndex < Waypoints[pathIndex].points.Count)
            return Waypoints[pathIndex].points[waypointIndex];
        else return null;
    }
    public List<Transform> GetRandomPath()
    {
        return Waypoints[UnityEngine.Random.Range(0, Waypoints.Count)].points;
    }
    private List<WayPointsHolder> GetValidPaths(int pathLength, PathLengthConditionType conditionType = PathLengthConditionType.Equal)
    {

        switch (conditionType)
        {
            case PathLengthConditionType.Equal:
                return Waypoints.Where(x => x.points.Count == pathLength).ToList();
            case PathLengthConditionType.SuperiorStrict:
                return Waypoints.Where(x => x.points.Count > pathLength).ToList();
            case PathLengthConditionType.InferiorStrict:
                return Waypoints.Where(x => x.points.Count < pathLength).ToList();
            default:
                break;
        }
        return null;
    }
    public List<Transform> GetRandomPath(int pathLength, PathLengthConditionType conditionType = PathLengthConditionType.Equal)
    {
        List<WayPointsHolder> validPaths = GetValidPaths(pathLength, conditionType);

        if (validPaths.Count > 0)
            return validPaths[UnityEngine.Random.Range(0, validPaths.Count)].points;
        else
            return null;
    }
    public enum PathLengthConditionType
    {
        Equal,
        SuperiorStrict,
        InferiorStrict
    }
}
