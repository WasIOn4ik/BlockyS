using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;

public class BoardWall : NetworkBehaviour
{
    public NetworkVariable<Turn> coords = new();

    public void Awake()
    {
        coords.OnValueChanged += OnPlaced;
    }

    private void OnPlaced(Turn previousValue, Turn newValue)
    {
        SpesLogger.Detail(newValue.type.ToString() + " " + newValue.pos.x + " x " + newValue.pos.y);
        var blocks = GameplayBase.instance.gameboard.blocks;

        if (newValue.type == ETurnType.PlaceXForward)
        {
            try
            {
                blocks[newValue.pos.x, newValue.pos.y].zDir = null;
                blocks[newValue.pos.x + 1, newValue.pos.y].zDir = null;
                blocks[newValue.pos.x, newValue.pos.y + 1].mzDir = null;
                blocks[newValue.pos.x + 1, newValue.pos.y + 1].mzDir = null;
            }
            catch (Exception ex)
            {
                SpesLogger.Exception(ex, "Ошибка при разрушении связей между блоками внутри OnPlaced в BoardWall");
            }
        }
        else if (newValue.type == ETurnType.PlaceZForward)
        {
            try
            {
                blocks[newValue.pos.x, newValue.pos.y].xDir = null;
                blocks[newValue.pos.x + 1, newValue.pos.y].mxDir = null;
                blocks[newValue.pos.x, newValue.pos.y + 1].xDir = null;
                blocks[newValue.pos.x + 1, newValue.pos.y + 1].mxDir = null;
            }
            catch (Exception ex)
            {
                SpesLogger.Exception(ex, "Ошибка при разрушении связей между блоками внутри OnPlaced в BoardWall");
            }
        }
        GameplayBase.instance.gameboard.wallsPlaces[newValue.pos.x, newValue.pos.y].bEmpty = false;
    }
}
