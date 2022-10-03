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

    public BoardBlock finish;

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
        finish = blocks[halfExtent, halfExtent];
        GenConnections(halfExtent);
    }

    public void UpdateSkins(PlayerCosmetic[] skins)
    {
        int count = skins.Length;

        switch (count)
        {
            case 2:
                foreach (var bl in blocks)
                {
                    if (!bl)
                        continue;

                    if (bl.coords.y <= halfExtention)
                    {
                        bl.SetSkin(skins[0].boardSkinID);
                    }
                    else
                    {
                        bl.SetSkin(skins[1].boardSkinID);
                    }
                }
                break;
            case 3:
                foreach (var bl in blocks)
                {
                    if (!bl)
                        continue;

                    if (bl.coords.x > bl.coords.y && bl.coords.y < halfExtention)
                    {
                        bl.SetSkin(skins[0].boardSkinID);
                    }
                    else if (bl.coords.x > (blocks.GetLength(1) - bl.coords.y) && bl.coords.y >= halfExtention)
                    {
                        bl.SetSkin(skins[1].boardSkinID);
                    }
                    else
                    {
                        bl.SetSkin(skins[2].boardSkinID);
                    }
                }
                break;
            case 4:
                break;

            default:
                break;
        }
    }

    public bool HasPath(Point pawnPos, Point includeWallPredict, ETurnType type)
    {
        BoardBlock start = blocks[pawnPos.x, pawnPos.y];
        bool xForward = type == ETurnType.PlaceXForward;

        List<BoardBlock> visited = new();
        Stack<BoardBlock> frontier = new Stack<BoardBlock>();
        frontier.Push(start);


        while (frontier.Count > 0)
        {
            var current = frontier.Pop();
            visited.Add(current);

            if (current == finish)
                return true;

            var neighbours = GetNeighbours(current, includeWallPredict, xForward);
            foreach (var neighbour in neighbours)
                if (!visited.Contains(neighbour))
                    frontier.Push(neighbour);
        }

        return false;/*

        int xLen = blocks.GetLength(0);
        int yLen = blocks.GetLength(1);

        Point[,] arr = new Point[xLen, yLen];

        for (int x = 0; x < xLen; x++)
        {
            for (int y = 0; y < yLen; y++)
            {
                arr[x, y] = blocks[x, y].coords;
            }
        }*/

    }

    protected List<BoardBlock> GetNeighbours(BoardBlock block, Point wall, bool xForward)
    {
        List<BoardBlock> neighbours = new();

        if (block.xDir
            && (xForward || block.coords.x != wall.x || (block.coords.y != wall.y && block.coords.y != wall.y + 1)))
            neighbours.Add(block.xDir);

        if (block.mxDir
            && (xForward || block.coords.x != wall.x + 1 || (block.coords.y != wall.y && block.coords.y != wall.y + 1)))
            neighbours.Add(block.mxDir);

        if (block.zDir
            && (!xForward || block.coords.y != wall.y || (block.coords.x != wall.x && block.coords.x != wall.x + 1)))
            neighbours.Add(block.zDir);

        if (block.mzDir
            && (!xForward || block.coords.y != wall.y + 1 || (block.coords.x != wall.x && block.coords.x != wall.x + 1)))
            neighbours.Add(block.mzDir);

        return neighbours;

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
    /*
        protected Node DFS(Node first, Node target)
        {
            List<Node> visited = new List<Node>();
            Stack<Node> frontier = new Stack<Node>();
            frontier.Push(first);

            while (frontier.Count > 0)
            {
                var current = frontier.Pop();
                visited.Add(current);

                if (current == target)
                    return Node;

                var neighbours = current.GenerateNeighbours();
                foreach (var neighbour in neighbours)
                    if (!visited.Contains(neighbour))
                        frontier.Push(neighbour);
            }

            return default;
        }*/

    #endregion
}
