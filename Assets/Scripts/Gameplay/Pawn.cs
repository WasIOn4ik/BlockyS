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
        if (previousValue != null)
        {
            var prevBlock = arr[previousValue.x, previousValue.y];
            if (prevBlock)
            {
                prevBlock.bEmpty = true;
            }
        }
        if (newValue.x < arr.GetLength(0) && newValue.y < arr.GetLength(1))
        {
            var newBlock = arr[newValue.x, newValue.y];
            newBlock.bEmpty = false;
            transform.position = newBlock.transform.position;
        }
        else
        {
            SpesLogger.Error("¬ыход за пределы карты при обновлении block в Pawn: " + name);
        }
    }
}
