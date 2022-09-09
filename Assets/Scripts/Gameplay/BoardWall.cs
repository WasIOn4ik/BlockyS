using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;

public class BoardWall : NetworkBehaviour
{
    NetworkVariable<Point> coords = new();

    public void Awake()
    {
        coords.OnValueChanged += OnPlaced;
    }

    private void OnPlaced(Point previousValue, Point newValue)
    {
        throw new NotImplementedException();
    }
}
