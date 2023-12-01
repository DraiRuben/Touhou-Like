using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

public class EnemySpawnManager : MonoBehaviour
{
    public static EnemySpawnManager Instance;
    [SerializeField] private EnemySpawner m_leftSpawner;
    [SerializeField] private EnemySpawner m_topSpawner;
    [SerializeField] private EnemySpawner m_rightSpawner;
    [SerializeField] private EnemySpawner m_bossSpawner;

    public int m_currentWave = 0;

    [SerializeField] private GameObject m_healthUpgrade;
    [SerializeField] private GameObject m_statsUpgrade;

    [NonSerialized] public List<GameObject> m_enemies = new();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    public void TrySpawnUpgrade(Transform _transform)
    {
        int result = UnityEngine.Random.Range(0, 2);
        if(result == 1)
        {
            result = UnityEngine.Random.Range(0, 2);
            if(result == 0)
            {
                Instantiate(m_healthUpgrade, _transform.position, Quaternion.identity);
            }
            else
            {
                Instantiate(m_statsUpgrade, _transform.position, Quaternion.identity);
            }
        }
    }
    public GameObject GetClosestEnemy(Vector2 _position)
    {
        if (m_enemies.Count > 0)
        {
            m_enemies = m_enemies.Where(x => x != null).ToList();
            if (m_enemies.Count > 0)
            {
                var AliveEnemies = m_enemies.Where(x => x.GetComponent<EntityHealthHandler>().Health > 0).ToList();
                return AliveEnemies.OrderBy(player => Vector3.Distance(player.transform.position, _position)).ToList()[0];
            }
        }
        return null;
    }
    private void Update()
    {
        if (EnemyMovement.EnemyCount <= 0)
        {
            m_currentWave++;
            if (m_currentWave%10 != 0)
            {
                int SpawnAmount = 1+ (int)(10 * Mathf.Log10(m_currentWave));
                m_leftSpawner.SpawnEnemy((int)(SpawnAmount/3f), 3f);
                m_rightSpawner.SpawnEnemy((int)(SpawnAmount / 3f), 3f);
                m_topSpawner.SpawnEnemy((int)(SpawnAmount / 3f), 3f);

                if ((int)((float)SpawnAmount % 3) != 0)
                    m_topSpawner.SpawnEnemy((int)(SpawnAmount % 3), 3f);
            }
            else
            {
                m_bossSpawner.SpawnEnemy(1, 1);
            }
        }
    }
}
