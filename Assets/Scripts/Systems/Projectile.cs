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
        public ParticleSystem.MinMaxCurve VelocityOverTime;

        [Space]

        public bool VariableScale;
        public ParticleSystem.MinMaxCurve ScaleOverTime;

        [Space]
        public bool VariableColor;
        [ShowCondition("VariableColor")]
        public Gradient ColorOverTime;

        [Space]
        public bool VariableRotation;
        public ParticleSystem.MinMaxCurve RotationOverTime;
    }
}
