using Sirenix.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SongPlayer : MonoBehaviour
{
    public static SongPlayer Instance;
    [SerializeField] private List<Song> Waves;
    [SerializeField] private List<Song> Bosses;
    private int m_currentBossSongIndex;
    private int m_currentWaveSongIndex;

    private AudioSource m_audioSource;
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        m_audioSource = GetComponent<AudioSource>();
    }
    private void Start()
    {
        if((EnemySpawnManager.Instance.m_currentWave + 1) % 10 != 0)
            StartCoroutine(PlaySongRoutine(Waves[m_currentWaveSongIndex],true));
        else
            StartCoroutine(PlaySongRoutine(Bosses[m_currentBossSongIndex], true));
    }
    public void NextSong()
    {
        if (EnemySpawnManager.Instance.m_currentWave % 10 == 0)
        {
            m_currentBossSongIndex = (m_currentBossSongIndex + 1) % Bosses.Count;
            StartCoroutine(PlaySongRoutine(Bosses[m_currentBossSongIndex]));
        }
        else
        {
            m_currentWaveSongIndex = (m_currentWaveSongIndex + 1) % Waves.Count;
            StartCoroutine(PlaySongRoutine(Waves[m_currentWaveSongIndex]));
        }
    }
    private IEnumerator PlaySongRoutine(Song song, bool OverrideWaitValue = false)
    {
        bool waitForBossWave = EnemySpawnManager.Instance.m_currentWave % 10 != 0;

        if (OverrideWaitValue) waitForBossWave = (EnemySpawnManager.Instance.m_currentWave+1) % 10 != 0;

        m_audioSource.clip = song.Intro;
        m_audioSource.Play();
        yield return new WaitForSecondsRealtime(song.Intro.length-.05f);
        m_audioSource.clip = song.Loop;
        m_audioSource.Play();
        if (waitForBossWave)
            yield return new WaitUntil(() => EnemySpawnManager.Instance.m_currentWave%10==0);
        else
            yield return new WaitUntil(() => EnemySpawnManager.Instance.m_currentWave % 10 != 0);
        
        NextSong();
    }
    [Serializable]
    public struct Song
    {
        public AudioClip Intro;
        public AudioClip Loop;
    }
}
