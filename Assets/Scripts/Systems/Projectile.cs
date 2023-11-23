using System;
using System.Collections;
using System.Collections.Generic;
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
        [ShowCondition("VariableVelocity")]
        public AnimationCurve VelocityOverTime;

        [Space]

        public bool VariableScale;
        [ShowCondition("VariableScale")]
        public AnimationCurve ScaleOverTime;

        [Space]
        public bool VariableColor;
        [ShowCondition("VariableColor")]
        public Gradient ColorOverTime;

        [Space]
        public bool VariableRotation;
        [ShowCondition("VariableRotation")]
        public AnimationCurve RotationOverTime;
    }    
}
