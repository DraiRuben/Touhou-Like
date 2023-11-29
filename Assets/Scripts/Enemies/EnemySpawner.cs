using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private List<GameObject> Spawnable;
    [SerializeField] private bool RandomX;
    [SerializeField] private bool RandomY;
    [SerializeField] private bool RandomSpawnedEnemy;
    [SerializeField] private bool KeepChoosenEnemyForEntireWave;
    private int CachedIndex;
    public void SpawnEnemy(int Amount, float Duration)
    {
        StartCoroutine(SpawnEnemyPeriodically(Amount, Duration));
    }
    private IEnumerator SpawnEnemyPeriodically(int Amount, float Duration)
    {
        int Spawned = 0;
        while (Spawned<Amount)
        {
            float x = RandomX ? Random.Range(-6f, 6f):transform.position.x;
            float y = RandomY ? Random.Range(0, 6f):transform.position.y;
            if (RandomSpawnedEnemy)
            {
                if(!KeepChoosenEnemyForEntireWave)
                {
                    CachedIndex = Random.Range(0, Spawnable.Count);
                }
                Instantiate(Spawnable[CachedIndex], new Vector3(x, y), Quaternion.identity);
            }
            else
            {
                Instantiate(Spawnable[CachedIndex], new Vector3(x, y), Quaternion.identity);
                if (!KeepChoosenEnemyForEntireWave)
                {
                    CachedIndex = (CachedIndex + 1) % (Spawnable.Count-1);
                }
            }
            Spawned++;
            yield return new WaitForSeconds(Duration / Amount);
        }
    }
}
