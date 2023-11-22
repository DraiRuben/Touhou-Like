using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class EnemyProjectileSpawner : ScriptableObject
{
    [SerializeField] private ProjectileSpawnSettingsDictionnary ProjectilePatterns;
    
    [Serializable]
    public class ShootZone
    {
#if UNITY_EDITOR
        public bool IsCircle;
        public bool IsPolygon;
        public bool IsStar;
#endif
        [Header("Transition")]
        public BehaviourChangeType BehaviourType;
        public float BehaviourExitValue;

        [Header("Pattern")]
        public PatternType patternType;
        public float SpawnFrequency;
        public float ShootRotation;

        [Tooltip("This is the number of projectiles in an arc for a circle zone,\nOr the number of projectiles per vertice of a polygon or Star")]
        public float ProjectileCount;

        [Header("Spawn Parameters for a Circle Pattern")]
        //[HideCondition("!IsCircle")]
        public float StartAngle;
        //[HideCondition("!IsCircle")]
        public float EndAngle;

        
        [Header("Spawn Parameters for a Polygon Pattern")]
        [Min(3)]
        //[HideCondition("!IsPolygon")]
        public int Vertices;

        [Header("Spawn Parameters for a Star Pattern")]
        [Min(3)]
        //[HideCondition("!IsStar")]
        public int Limbs;

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
        foreach(ShootBehaviour behaviour in ProjectilePatterns.Values)
        {
            foreach(ShootZone zone in behaviour.ShootZones)
            {
                zone.IsCircle = zone.patternType == ShootZone.PatternType.Circle;
                zone.IsPolygon = zone.patternType == ShootZone.PatternType.Polygon;
                zone.IsStar = zone.patternType == ShootZone.PatternType.Star;
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
