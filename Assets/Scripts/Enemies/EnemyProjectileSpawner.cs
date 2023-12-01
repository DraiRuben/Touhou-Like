using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class EnemyProjectileSpawner : ScriptableObject
{
    public ProjectileSpawnSettingsDictionnary ProjectilePatterns;
    [Serializable]
    public class ShootZone
    {
        [Header("Transition")]
        public BehaviourChangeType BehaviourType;

        [ShowIf(nameof(BehaviourType), BehaviourChangeType.Time)]
        public bool InfiniteDuration;
        [ShowIf(nameof(BehaviourType), BehaviourChangeType.Life)]
        public float BehaviourEnterValue;
        [ShowIf("@this.BehaviourType != BehaviourChangeType.Time || this.BehaviourType == BehaviourChangeType.Time && !this.InfiniteDuration")]
        public float BehaviourExitValue;

        [Header("Pattern")]
        public PatternType patternType;
        public bool CircleCenteredVelocity;
        [MinValue(.001f)]
        public float CenterDistance = 1;
        [Space]
        [MaxValue(nameof(EndAngle))]
        [PropertyRange(-180f, nameof(EndAngle))]
        public float StartAngle;

        [ShowIf(nameof(patternType), PatternType.Circle)]
        [MinValue(nameof(StartAngle))]
        [PropertyRange(nameof(StartAngle),180f)]
        public float EndAngle;
        [ShowIf(nameof(patternType), PatternType.Circle)]
        [MaxValue("@360f/Mathf.Abs(this.EndAngle - this.StartAngle)")]
        [MinValue(1)]
        public int ZoneCount = 1;
        [ShowIf(nameof(patternType), PatternType.Circle)]
        public bool RandomPosition;

        [ShowIf(nameof(patternType), PatternType.Polygon)]
        [MinValue(3)]
        public int Vertices = 3;

        [ShowIf(nameof(patternType), PatternType.Star)]
        [MinValue(3)]
        public int Limbs = 3;

        [ShowIf(nameof(patternType), PatternType.Star)]
        [MinValue(.1f)]
        public float InnerPointsDist = 1.45f;

        [Space]
        [HideIf(nameof(AimAtClosestPlayer))]
        public bool Spin;
        [ShowIf(nameof(Spin))]
        public float SpinSpeed;
        public bool RotationFollowsAim;

        [Space]
        [HideIf(nameof(Spin))]
        public bool AimAtClosestPlayer;
        [HideIf(nameof(AimAtClosestPlayer))]
        [Range(-180, 180f)]
        public float ShootRotation;

        [Tooltip("This is the number of projectiles in an arc for a circle zone,\nOr the number of projectiles per vertice of a polygon or Star")]
        public int ProjectileCount;
        [MinValue(.001f)]
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
        foreach (KeyValuePair<BehaviourChangeType, ShootBehaviour> behaviour in ProjectilePatterns)
        {
            foreach (ShootZone zone in behaviour.Value.ShootZones)
            {
                zone.BehaviourType = behaviour.Key;
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
