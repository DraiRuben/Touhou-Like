using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovementHandler : MonoBehaviour
{
    [SerializeField] private float m_movementSpeed;
    [SerializeField] private AnimationCurve m_velocityOverTime;
    [NonSerialized] public Vector2 MovementInput;
    private float m_timeSinceInput;
    private Rigidbody2D m_rb;
    private void Awake()
    {
        m_rb = GetComponent<Rigidbody2D>();
    }
    private void FixedUpdate()
    {
        if (MovementInput.magnitude != 0)
        {
            m_rb.velocity = MovementInput.normalized * m_movementSpeed * m_velocityOverTime.Evaluate(m_timeSinceInput);
            m_timeSinceInput += Time.deltaTime;
        }
        else
        {
            m_timeSinceInput = 0;
        }
    }
}
