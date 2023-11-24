using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance;
    private PlayerInputManager m_inputManager;
    public List<GameObject> m_players = new();
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        m_inputManager = GetComponent<PlayerInputManager>();
    }
    public void OnPlayerJoined(PlayerInput _input)
    {
        m_players.Add(_input.gameObject);
    }
    public GameObject GetClosestPlayer(Vector3 _position)
    {
        return m_players.OrderBy(player => Vector3.Distance(player.transform.position, _position)).ToList()[0];
    }
}
