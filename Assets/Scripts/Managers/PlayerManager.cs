using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance;
    private PlayerInputManager m_inputManager;
    public List<GameObject> m_players = new();
    public List<Sprite> m_playerSprites;
    [SerializeField] private Transform UICanvas;
    [SerializeField] private GameObject UIPrefab;
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        m_inputManager = GetComponent<PlayerInputManager>();
    }
    public void OnPlayerJoined(PlayerInput _input)
    {
        m_players.Add(_input.gameObject);
        _input.transform.GetChild(0).GetComponent<SpriteRenderer>().sprite = m_playerSprites[m_players.Count - 1];
        Instantiate(UIPrefab, UICanvas);
        EntityHealthHandler.AlivePlayers++;
    }
    public GameObject GetClosestPlayer(Vector3 _position)
    {
        if (m_players.Count > 0)
            return m_players.OrderBy(player => Vector3.Distance(player.transform.position, _position)).ToList()[0];
        else return null;
    }
}
