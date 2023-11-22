using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(EntityHealthHandler))]
public class EnemyFiringSystem : MonoBehaviour
{
    [SerializeField] private EnemyProjectileSpawner ShootSettings;
    private EntityHealthHandler m_healthComp;
    private EnemyProjectileSpawner.ShootZone m_currentBehaviour;
    private int m_nextBehaviourIndex;
    private List<int> m_alreadyUsedPatterns = new();
    private EnemyProjectileSpawner.BehaviourChangeType m_currentBehaviourType;
    private void Awake()
    {
        m_healthComp = GetComponent<EntityHealthHandler>();
        m_healthComp.OnHealthChanged.AddListener(() => IsInPriorityShootingBehaviour = true);
    }

    private bool IsInPriorityShootingBehaviour;
    private void NextPattern()
    {
        if (!IsInPriorityShootingBehaviour) //if we aren't in a behaviour for collision or death since those ones interrupt any other pattern
        {
            //firstly check if we can trigger a health pattern, else simply do a timed pattern
            if (ShootSettings.ProjectilePatterns[EnemyProjectileSpawner.BehaviourChangeType.Life].ShootZones.Count > 0)
            {
                for (int i = 0; i < ShootSettings.ProjectilePatterns[EnemyProjectileSpawner.BehaviourChangeType.Life].ShootZones.Count; i++)
                {
                    if (m_currentBehaviourType != EnemyProjectileSpawner.BehaviourChangeType.Life)
                    {
                        m_nextBehaviourIndex = 0;
                        m_alreadyUsedPatterns.Clear();
                    }
                    if (m_healthComp.Health >= ShootSettings.ProjectilePatterns[EnemyProjectileSpawner.BehaviourChangeType.Life].ShootZones[i].BehaviourExitValue)
                    {
                        m_currentBehaviour = ShootSettings.ProjectilePatterns[EnemyProjectileSpawner.BehaviourChangeType.Life].ShootZones[i];
                        m_nextBehaviourIndex = i;
                        m_currentBehaviourType = EnemyProjectileSpawner.BehaviourChangeType.Life;
                        StartCoroutine(ShootRoutine());
                        return;
                    }
                }
            }
            //if we reached there, it means we don't have any usable health patters so we can check for time patterns
            if (ShootSettings.ProjectilePatterns[EnemyProjectileSpawner.BehaviourChangeType.Time].ShootZones.Count > 0)
            {
                ChooseNewBehaviour(EnemyProjectileSpawner.BehaviourChangeType.Time);
                return;
            }
        }
        else
        {
            if (m_healthComp.Health > 0 && ShootSettings.ProjectilePatterns[EnemyProjectileSpawner.BehaviourChangeType.Collision].ShootZones.Count > 0) //damaged
            {
                ChooseNewBehaviour(EnemyProjectileSpawner.BehaviourChangeType.Collision);
            }
            else if (m_healthComp.Health <= 0 && ShootSettings.ProjectilePatterns[EnemyProjectileSpawner.BehaviourChangeType.Death].ShootZones.Count > 0)//death
            {
                ChooseNewBehaviour(EnemyProjectileSpawner.BehaviourChangeType.Death);
            }
        }

    }
    private void ChooseNewBehaviour(EnemyProjectileSpawner.BehaviourChangeType _newBehaviourType)
    {
        if (m_currentBehaviourType != _newBehaviourType)
        {
            m_nextBehaviourIndex = 0;
            m_alreadyUsedPatterns.Clear();
        }
        switch (ShootSettings.ProjectilePatterns[_newBehaviourType].ChoiceMethod)
        {
            case EnemyProjectileSpawner.ShootBehaviour.BehaviourChooseMethod.Sequence:
                m_currentBehaviour = ShootSettings.ProjectilePatterns[_newBehaviourType].ShootZones[m_nextBehaviourIndex];
                //makes it so that when index is beyond max index, it wraps back to 0
                m_nextBehaviourIndex = (m_nextBehaviourIndex + 1) % ShootSettings.ProjectilePatterns[_newBehaviourType].ShootZones.Count;
                break;
            case EnemyProjectileSpawner.ShootBehaviour.BehaviourChooseMethod.Random:
                m_currentBehaviour = ShootSettings.ProjectilePatterns[_newBehaviourType].ShootZones[Random.Range(0, ShootSettings.ProjectilePatterns[_newBehaviourType].ShootZones.Count)];
                break;
            case EnemyProjectileSpawner.ShootBehaviour.BehaviourChooseMethod.RandomNonRepeating:
                //reset list if we already did all possible behaviours
                if (m_alreadyUsedPatterns.Count >= ShootSettings.ProjectilePatterns[_newBehaviourType].ShootZones.Count)
                {
                    m_alreadyUsedPatterns.Clear();
                }
                //generates new random number until we get one that hasn't been already used (if it was used, it's in the list)
                int index = Random.Range(0, ShootSettings.ProjectilePatterns[_newBehaviourType].ShootZones.Count);
                while (m_alreadyUsedPatterns.Contains(index))
                {
                    index = Random.Range(0, ShootSettings.ProjectilePatterns[_newBehaviourType].ShootZones.Count);
                }
                m_alreadyUsedPatterns.Add(index);
                m_currentBehaviour = ShootSettings.ProjectilePatterns[_newBehaviourType].ShootZones[index];
                break;
        }
        StartCoroutine(ShootRoutine());
        m_currentBehaviourType = _newBehaviourType;
    }

    private IEnumerator ShootRoutine()
    {
        EnemyProjectileSpawner.BehaviourChangeType routineType = m_currentBehaviourType;
        if (!m_currentBehaviour.AimAtClosestPlayer)
        {
            transform.rotation = Quaternion.Euler(0, 0, m_currentBehaviour.ShootRotation);
        }
        while (routineType == m_currentBehaviourType)
        {
            if (m_currentBehaviour.AimAtClosestPlayer)
            {
                GameObject _closestPlayer = PlayerManager.Instance.GetClosestPlayer(transform.position);
                float angle = Mathf.Atan2(_closestPlayer.transform.position.y-transform.position.y, _closestPlayer.transform.position.x - transform.position.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, angle);
            }
            yield return new WaitForSeconds(m_currentBehaviour.SpawnFrequency);
        }
    }
}
