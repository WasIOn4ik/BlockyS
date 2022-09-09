using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;

public class Pawn : NetworkBehaviour
{
    public int playerOrder;
    public NetworkVariable<Point> block;

    public void Awake()
    {
        block.OnValueChanged += OnMoved;
    }

    private void OnMoved(Point previousValue, Point newValue)
    {
        var arr = GameplayBase.instance.gameboard.blocks;
        if (newValue.x < arr.GetLength(0) && newValue.y < arr.GetLength(1))
        {
            transform.position = arr[newValue.x, newValue.y].transform.position;
        }
        else
        {
            SpesLogger.Error("¬ыход за пределы карты при обновлении block в Pawn: " + name);
        }
    }
}
