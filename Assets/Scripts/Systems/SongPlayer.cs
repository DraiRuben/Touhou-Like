using Sirenix.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SongPlayer : MonoBehaviour
{
    public static SongPlayer Instance;
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    [SerializeField]private List<Song> Waves;
    [SerializeField]private List<Song> Bosses;

    [Serializable]
    public struct Song
    {
        public AudioClip Intro;
        public AudioClip Loop;
    }
}
