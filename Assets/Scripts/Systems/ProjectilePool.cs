using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectilePool : MonoBehaviour
{
    public static ProjectilePool Instance;
    [SerializeField] private GameObject m_projectile;
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public GameObject GetProjectile()
    {
        if (transform.childCount > 0)
        {
            GameObject _toGive = transform.GetChild(0).gameObject;
            _toGive.SetActive(true);
            return _toGive;
        }
        else
        {
            GameObject _toGive = Instantiate(m_projectile);
            return _toGive;
        }
    }
    public void ReturnToPool(GameObject _toReturn)
    {
        _toReturn.transform.parent = transform;
        _toReturn.transform.localPosition = Vector3.zero;
        _toReturn.transform.rotation = Quaternion.identity;
        _toReturn.gameObject.SetActive(false);
    }
}