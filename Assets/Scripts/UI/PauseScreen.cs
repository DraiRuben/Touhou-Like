using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class PauseScreen : MonoBehaviour
{
    public static PauseScreen Instance;
    private Animator m_animator;
    [SerializeField] private GameObject m_resume;
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        m_animator = GetComponent<Animator>();
    }
    public void Pause()
    {
        if (Time.timeScale == 1)
        {
            Time.timeScale = 0;
            EventSystem.current.SetSelectedGameObject(m_resume);
        }
        else
        {
            EventSystem.current.SetSelectedGameObject(null);
            StartCoroutine(TimeReset(.4f));
        }

        m_animator.SetTrigger("ChangeState");
    }
    public void Restart()
    {
        Time.timeScale = 1f;
        m_animator.SetTrigger("ChangeState");
        StartCoroutine(LoadSceneDelayed(.4f,SceneManager.GetActiveScene().buildIndex));
    }
    public void MainMenu()
    {
        Time.timeScale = 1f;
        m_animator.SetTrigger("ChangeState");
        StartCoroutine(LoadSceneDelayed(.4f, 0));
    }
    public static IEnumerator LoadSceneDelayed(float _waitTime, int _sceneIndex)
    {
        yield return new WaitForSeconds( _waitTime );
        SceneManager.LoadSceneAsync( _sceneIndex );
    }
    private IEnumerator TimeReset(float _resetWait)
    {
        yield return new WaitForSecondsRealtime(_resetWait);
        Time.timeScale = 1f;
    }
}
