using Sirenix.OdinInspector;
using System;
using UnityEngine;

public static class Projectile
{
    [Serializable]
    public struct ProjectileParameters
    {
        public Sprite Texture;
        public Material Mat;
        public float LifeTime;

        [Header("InitialParameters")]
        public float InitialVelocityStrength;
        [Range(-180, 180f)]
        public float InitialRotation;
        [Space]
        public Color InitialColor;
        [Space]
        public Vector3 InitialScale;


        [Header("LifetimeParameters")]

        public bool VariableVelocity;
        [ShowIf(nameof(VariableVelocity))]
        public ParticleSystem.MinMaxCurve VelocityOverTime;
        [Space]
        public bool VariableScale;
        [ShowIf(nameof(VariableScale))]
        public ParticleSystem.MinMaxCurve ScaleOverTime;

        [Space]
        public bool VariableColor;
        [ShowIf(nameof(VariableColor))]
        public Gradient ColorOverTime;

        [Space]
        public bool VariableRotation;
        [ShowIf(nameof (VariableRotation))]
        public ParticleSystem.MinMaxCurve RotationOverTime;

        [Space]
        public bool BulletCurve;
        [ShowIf(nameof (BulletCurve))]
        public ParticleSystem.MinMaxCurve TrajectoryOverTime;
    }
}
