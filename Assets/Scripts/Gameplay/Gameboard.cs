using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

[Serializable]
public class Gameboard
{
    #region Variables
    [SerializeField] protected BoardBlock boardBlockPrefab;

    [SerializeField] protected WallPlaceholder wallPlaceholderPrefab;

    [SerializeField] protected Transform blocksHolder;

    [Tooltip("AnglesTowersSize –азмер вырезов по кра€м доски")]
    [SerializeField] protected int ats = 2;

    public BoardBlock[,] blocks;

    public WallPlaceholder[,] wallsPlaces;

    public int halfExtention;

    #endregion

    #region Functions

    /// <summary>
    /// —оздает блоки, генерирует соединени€ и точки создани€ стен
    /// </summary>
    /// <param name="halfExtent"> –азме пол€ от спавна игрока до центра </param>
    public void Initialize(int halfExtent)
    {
        halfExtention = halfExtent;
        int size = halfExtent * 2 + 1;
        blocks = new BoardBlock[size, size];
        wallsPlaces = new WallPlaceholder[size - 1, size - 1];

        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                var position = new Vector3(i - halfExtent, 0, j - halfExtent);

                if (!((i < ats && j < ats) || (i > size - (ats + 1) && j < ats) || (i < ats && j > size - (ats + 1)) || (i > size - (ats + 1) && j > size - (ats + 1))))
                {
                    var block = GameObject.Instantiate(boardBlockPrefab, position, Quaternion.identity, blocksHolder);
                    blocks[i, j] = block;
                    block.coords = new Point(i, j);
                    block.name = "BLock_" + i + "x" + j;
                }
                if (!((i < ats && j < ats) || (i > size - (ats + 2) && j < ats) || (i < ats && j > size - (ats + 2)) || (i > size - (ats + 2) && j > size - (ats + 2))))
                {
                    if (i < size - 1 && j < size - 1)
                    {
                        var wallPlaceholder = GameObject.Instantiate(wallPlaceholderPrefab, position + new Vector3(0.5f, 0f, 0.5f), Quaternion.identity, blocksHolder);
                        wallsPlaces[i, j] = wallPlaceholder;
                        wallPlaceholder.coords = new Point(i, j);
                    }
                }
            }
        }
        GenConnections(halfExtent);
    }

    /// <summary>
    /// —оздает соединени€ между блоками
    /// </summary>
    /// <param name="halfExtent"></param>
    protected void GenConnections(int halfExtent)
    {
        int size = halfExtent * 2 + 1;

        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                var block = blocks[i, j];
                if (!block)
                    continue;

                if (i > 0)
                {
                    block.mxDir = blocks[i - 1, j];
                }
                if (i + 1 < size)
                {
                    block.xDir = blocks[i + 1, j];
                }
                if (j > 0)
                {
                    block.mzDir = blocks[i, j - 1];
                }
                if (j + 1 < size)
                {
                    block.zDir = blocks[i, j + 1];
                }
            }
        }
    }

    #endregion
}
