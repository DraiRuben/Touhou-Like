using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private List<SpawnInfo> SpawnInfos;
    [SerializeField] private bool RandomX;
    [SerializeField] private bool RandomY;
    [SerializeField] private bool RandomSpawnedEnemy;
    [SerializeField] private bool KeepChoosenEnemyForEntireWave;
    private int CachedIndex;
    private List<GameObject> Spawnable = new();

    [Serializable]
    public struct SpawnInfo
    {
        public GameObject Spawnable;
        public int WaveUnlocked;
    }
    private void TryUnlockEnemies()
    {
        int currentWave = EnemySpawnManager.Instance.m_currentWave;
        var copy = SpawnInfos.ToList();
        foreach(var enemy in copy)
        {
            if (currentWave >= enemy.WaveUnlocked)
            {
                SpawnInfos.Remove(enemy);
                Spawnable.Add(enemy.Spawnable);
            }
        }
    }
    public void SpawnEnemy(int Amount, float Duration)
    {
        TryUnlockEnemies();
        StartCoroutine(SpawnEnemyPeriodically(Amount, Duration));
    }
    private IEnumerator SpawnEnemyPeriodically(int Amount, float Duration)
    {
        int Spawned = 0;
        while (Spawned<Amount)
        {
            float x = RandomX ? UnityEngine.Random.Range(-6f, 6f):transform.position.x;
            float y = RandomY ? UnityEngine.Random.Range(0, 6f):transform.position.y;
            if (RandomSpawnedEnemy)
            {
                if(!KeepChoosenEnemyForEntireWave)
                {
                    CachedIndex = UnityEngine.Random.Range(0, Spawnable.Count);
                }
                EnemySpawnManager.Instance.m_enemies.Add(Instantiate(Spawnable[CachedIndex], new Vector3(x, y), Quaternion.identity));
            }
            else
            {
                if (!KeepChoosenEnemyForEntireWave)
                {
                    CachedIndex++;
                    if(CachedIndex>=Spawnable.Count)
                        CachedIndex = 0;
                }
                EnemySpawnManager.Instance.m_enemies.Add(Instantiate(Spawnable[CachedIndex], new Vector3(x, y), Quaternion.identity));
                
            }
            Spawned++;
            yield return new WaitForSeconds(Duration / Amount);
        }
    }
}
