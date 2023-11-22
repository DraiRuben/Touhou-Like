using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class EnemyProjectileSpawner : ScriptableObject
{
    public ProjectileSpawnSettingsDictionnary ProjectilePatterns;
    [Serializable]
    public class ShootZone
    {
#if UNITY_EDITOR
        [HideInInspector] public bool IsCircle;
        [HideInInspector] public bool IsPolygon;
        [HideInInspector] public bool IsStar;
        [HideInInspector] public bool ShowExitValue;
        [HideInInspector] public bool ShowInfiniteDuration;
#endif
        [Header("Transition")]
        public BehaviourChangeType BehaviourType;
        [ShowCondition("ShowInfiniteDuration")]
        public bool InfiniteDuration;
        [ShowCondition("ShowExitValue")]
        [Tooltip("ALWAYS remember to order the Shoot Zones by behaviour exit time from the biggest to the smallest for Life behaviour types")]
        public float BehaviourExitValue;

        [Header("Pattern")]
        public PatternType patternType;
        [HideCondition("AimAtClosestPlayer")]
        public bool Spin;
        [ShowCondition("Spin")]
        public float SpinSpeed;

        [Space]
        [ShowCondition("IsCircle")]
        [Range(-180, 180f)]
        public float StartAngle;

        [ShowCondition("IsCircle")]
        [Range(-180, 180f)]
        public float EndAngle;
        [ShowCondition("IsCircle")]
        [Min(1)]
        public int ZoneCount =1;

        [ShowCondition("IsPolygon")]
        [Min(3)]
        public int Vertices = 3;

        [ShowCondition("IsStar")]
        [Min(3)]
        public int Limbs = 3;

        [Space]
        [HideCondition("Spin")]
        public bool AimAtClosestPlayer;
        [HideCondition("AimAtClosestPlayer")]
        [Range(-180, 180f)]
        public float ShootRotation;

        [Tooltip("This is the number of projectiles in an arc for a circle zone,\nOr the number of projectiles per vertice of a polygon or Star")]
        public float ProjectileCount;
        public float SpawnFrequency;

        [Space]
        public Projectile.ProjectileParameters ProjectileParameters;

        public enum PatternType
        {
            Circle,
            Polygon,
            Star
        }

    }
#if UNITY_EDITOR
    private void OnValidate()
    {
        foreach(var behaviour in ProjectilePatterns)
        {
            foreach(ShootZone zone in behaviour.Value.ShootZones)
            {
                zone.IsCircle = zone.patternType == ShootZone.PatternType.Circle;
                zone.IsPolygon = zone.patternType == ShootZone.PatternType.Polygon;
                zone.IsStar = zone.patternType == ShootZone.PatternType.Star;
                zone.BehaviourType = behaviour.Key;
                zone.ShowInfiniteDuration = zone.BehaviourType == BehaviourChangeType.Time;
                zone.ShowExitValue = zone.BehaviourType == BehaviourChangeType.Time && !zone.InfiniteDuration || zone.BehaviourType == BehaviourChangeType.Life;
            }
        }
    }
#endif
    [Serializable]
    public struct ShootBehaviour
    {
        [Header("Projectile Pattern")]
        public BehaviourChooseMethod ChoiceMethod;
        public List<ShootZone> ShootZones;

        public enum BehaviourChooseMethod
        {
            Sequence,
            Random,
            RandomNonRepeating
        }
        [HideInInspector] public List<int> AlreadyChosenIndexes;
        
    }

    public enum BehaviourChangeType
    {
        Time,
        Collision,
        Death,
        Life,
    }
}
