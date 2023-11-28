using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Upgrade : MonoBehaviour
{
    public UpgradeType UpgradeInfo;

    public int IntValue;
    public float FloatValue;
    public enum UpgradeType
    {
        Health,
        FireRate,
        Spread,
        BulletSize,
        BulletSpeed,
        BulletCount,
        Explosive,
        Rebound
    }
}
