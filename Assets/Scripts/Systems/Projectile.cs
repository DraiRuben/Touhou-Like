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
        public float InitialRotation;
        public Color InitialColor;
        public Vector3 InitialOffset;
        public Vector3 InitialScale;


        [Header("LifetimeParameters")]
        public AnimationCurve VelocityOverTime;
        public float VelocityLifetime;

        public AnimationCurve ScaleOverTime;
        public float ScaleLifetime;

        public Gradient ColorOverTime;
        public float ColorLifetime;

        public AnimationCurve RotationOverTime;
        public float RotationLifetime;

        public AnimationCurve PositionXOffsetOverTime;
        public float PositionXOffsetLifetime;

        public AnimationCurve PositionYOffsetOverTime;
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
