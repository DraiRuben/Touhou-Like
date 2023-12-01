using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EndScreen : MonoBehaviour
{
    public static EndScreen Instance;
    [NonSerialized] public List<GameObject> Scores = new();

    private Animator m_animator;
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        gameObject.SetActive(false);
        m_animator = GetComponent<Animator>();
    }

    public void DisplayScores()
    {
        gameObject.SetActive(true);
        Time.timeScale = 0f;
        Transform Grid = transform.GetChild(1);
        Destroy(Scores[0].transform.parent.parent.parent.gameObject);
        for (int i = 0; i < Scores.Count; i++)
        {
            Grid.GetChild(i).gameObject.SetActive(true);
            Scores[i].transform.SetParent(Grid.GetChild(i));
            Scores[i].transform.localPosition = Vector3.zero;
        } 
    }
    public void Restart()
    {
        Time.timeScale = 1f;
        m_animator.SetTrigger("ChangeState");
        StartCoroutine(PauseScreen.LoadSceneDelayed(.4f, SceneManager.GetActiveScene().buildIndex));
    }
    public void MainMenu()
    {
        Time.timeScale = 1f;
        m_animator.SetTrigger("ChangeState");
        StartCoroutine(PauseScreen.LoadSceneDelayed(.4f, 0));
    }

}
