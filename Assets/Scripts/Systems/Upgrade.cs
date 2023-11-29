using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Upgrade : MonoBehaviour
{
    public bool IsRandom;
    public UpgradeType DefaultValue;
    public UpgradeType GetRandomUpgrade()
    {
        return (UpgradeType)UnityEngine.Random.Range(0, 7);
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
