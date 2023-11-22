using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [NonSerialized] public ProjectileParameters Parameters;

    [NonSerialized] public int Damage = 1;

    [Serializable]
    public struct ProjectileParameters
    {
        [Header("InitialParameters")]
        public float InitialVelocityStrength;
        [Range(-180, 180f)]
        public float InitialRotation;
        [Space]
        public Color InitialColor;
        [Space]
        public Vector3 InitialOffset;
        public Vector3 InitialScale;


        [Header("LifetimeParameters")]

        public bool VariableVelocity;
        [ShowCondition("VariableVelocity")]
        public AnimationCurve VelocityOverTime;
        [ShowCondition("VariableVelocity")]
        public float VelocityLifetime;

        [Space]

        public bool VariableScale;
        [ShowCondition("VariableScale")]
        public AnimationCurve ScaleOverTime;
        [ShowCondition("VariableScale")]
        public float ScaleLifetime;

        [Space]
        public bool VariableColor;
        [ShowCondition("VariableColor")]
        public Gradient ColorOverTime;
        [ShowCondition("VariableColor")]
        public float ColorLifetime;

        [Space]
        public bool VariableRotation;
        [ShowCondition("VariableRotation")]
        public AnimationCurve RotationOverTime;
        [ShowCondition("VariableRotation")]
        public float RotationLifetime;

        [Space]
        public bool VariablePosX;
        [ShowCondition("VariablePosX")]
        public AnimationCurve PositionXOffsetOverTime;
        [ShowCondition("VariablePosX")]
        public float PositionXOffsetLifetime;

        [Space]
        public bool VariablePosY;
        [ShowCondition("VariablePosY")]
        public AnimationCurve PositionYOffsetOverTime;
        [ShowCondition("VariablePosY")]
        public float PositionYOffsetLifetime;
    }
    private void OnTriggerEnter2D(Collider2D _collider)
    {
        if(_collider.CompareTag("Player") || _collider.CompareTag("Enemy"))
        {
            _collider.GetComponent<EntityHealthHandler>().Health -= Damage;
        }
    }
    
}
