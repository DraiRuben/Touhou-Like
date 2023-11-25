using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapBorder : MonoBehaviour
{
    public static PolygonCollider2D BorderCollider;

    private void Awake()
    {
        BorderCollider = GetComponent<PolygonCollider2D>();
    }
}
