using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Upgrade : MonoBehaviour
{
    public List<UpgradeType> ValidTypes;
    public UpgradeType GetRandomUpgrade()
    {
        return ValidTypes[UnityEngine.Random.Range(0, ValidTypes.Count)];
    }
    public enum UpgradeType
    {
        Health,
        FireRate,
        Spread,
        BulletSize,
        BulletSpeed,
        BulletCount,
        Rebound,
        Explosive,
    }
}
