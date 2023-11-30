using System.Collections;
using UnityEngine;

public class ProjectilePool : MonoBehaviour
{
    public static ProjectilePool Instance;
    [SerializeField] private ParticleSystem m_emitterPrefab;
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public GameObject GetEmitter()
    {
        if (transform.childCount > 0)
        {
            GameObject _toGive = transform.GetChild(0).gameObject;
            _toGive.transform.parent = null;
            _toGive.SetActive(true);
            return _toGive;
        }
        else
        {
            GameObject _toGive = Instantiate(m_emitterPrefab.gameObject);
            return _toGive;
        }
    }
    public void ReturnToPool(ParticleSystem _toReturn)
    {
        _toReturn.transform.parent = transform;
        _toReturn.transform.localPosition = Vector3.zero;
        _toReturn.transform.rotation = Quaternion.identity;
        _toReturn.gameObject.SetActive(false);
    }
    public void ReturnToPoolLater(ParticleSystem _toReturn)
    {
        StartCoroutine(ReturnLater(_toReturn));
    }
    private IEnumerator ReturnLater(ParticleSystem _toReturn)
    {
        yield return null;
        yield return null;
        ParticleSystem system = _toReturn.GetComponent<ParticleSystem>();
        yield return new WaitUntil(()=>system.particleCount<=0);
        ReturnToPool(_toReturn);
    }
}
