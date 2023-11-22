using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance;
    private PlayerInputManager m_inputManager;
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        m_inputManager = GetComponent<PlayerInputManager>();
    }
    public void OnPlayerJoined(PlayerInput _input)
    {

    }
}
