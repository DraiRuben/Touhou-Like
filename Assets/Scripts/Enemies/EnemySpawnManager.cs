using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class EnemySpawnManager : MonoBehaviour
{
    public static EnemySpawnManager Instance;
    [SerializeField] private EnemySpawner m_leftSpawner;
    [SerializeField] private EnemySpawner m_topSpawner;
    [SerializeField] private EnemySpawner m_rightSpawner;
    [SerializeField] private EnemySpawner m_bossSpawner;

    [SerializeField] private int m_currentWave=0;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
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
