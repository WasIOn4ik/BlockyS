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

    [SerializeField] protected CosmeticMeshTarget cosmeticMeshPrefab;

    [SerializeField] protected Transform blocksHolder;

    [Tooltip("AnglesTowersSize Размер вырезов по краям доски")]
    [SerializeField] protected int ats = 2;

    public BoardBlock[,] blocks;

    public BoardBlock finish;

    public WallPlaceholder[,] wallsPlaces;

    public int halfExtention;

    public List<CosmeticMeshTarget> decors = new();

    #endregion

    #region Functions

    /// <summary>
    /// Создает блоки, генерирует соединения и точки создания стен
    /// </summary>
    /// <param name="halfExtent"> Разме поля от спавна игрока до центра </param>
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

        foreach (var d in decors)
        {
            if (d)
                GameObject.Destroy(d.gameObject);
        }

        decors.Clear();

        decors.Add(GameObject.Instantiate(cosmeticMeshPrefab, new Vector3(0.5f - halfExtent, 0, halfExtent - 0.5f), Quaternion.identity, blocksHolder)); // 0
        decors.Add(GameObject.Instantiate(cosmeticMeshPrefab, new Vector3(halfExtent - 0.5f, 0, halfExtent - 0.5f), Quaternion.identity, blocksHolder)); // 0

        decors.Add(GameObject.Instantiate(cosmeticMeshPrefab, new Vector3(0.5f - halfExtent, 0, 0.5f - halfExtent), Quaternion.identity, blocksHolder)); // 1
        decors.Add(GameObject.Instantiate(cosmeticMeshPrefab, new Vector3(halfExtent - 0.5f, 0, 0.5f - halfExtent), Quaternion.identity, blocksHolder)); // 1

        finish = blocks[halfExtent, halfExtent];
        GenConnections(halfExtent);
    }

    /// <summary>
    /// Обновляет игровое поле, количество PlayerCosmetic: 2-4
    /// </summary>
    /// <param name="skins"></param>
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

                    if (bl.coords.y >= halfExtention)
                    {
                        bl.SetSkin(skins[0].boardSkinID);
                    }
                    else
                    {
                        bl.SetSkin(skins[1].boardSkinID);
                    }
                }
                decors[0].SetSkin(skins[0].boardSkinID, 0);
                decors[1].SetSkin(skins[0].boardSkinID, 1);
                decors[2].SetSkin(skins[1].boardSkinID, 0);
                decors[3].SetSkin(skins[1].boardSkinID, 1);
                break;
            case 3:
                foreach (var bl in blocks)
                {
                    if (!bl)
                        continue;

                    if (bl.coords.x < bl.coords.y && bl.coords.y >= halfExtention) // Нулевой игрок
                    {
                        bl.SetSkin(skins[0].boardSkinID);
                    }
                    else if (bl.coords.x < (blocks.GetLength(1) - bl.coords.y - 1) && bl.coords.y < halfExtention) // Первый игрок
                    {
                        bl.SetSkin(skins[1].boardSkinID);
                    }
                    else // Второй игрок
                    {
                        bl.SetSkin(skins[2].boardSkinID);
                    }
                }
                decors[0].SetSkin(skins[0].boardSkinID, 0);
                decors[1].SetSkin(skins[0].boardSkinID, 1);
                decors[2].SetSkin(skins[1].boardSkinID, 0);
                decors[3].SetSkin(skins[2].boardSkinID, 0);
                break;
            case 4:
                foreach (var bl in blocks)
                {
                    if (!bl)
                        continue;

                    if (bl.coords.x < bl.coords.y && bl.coords.x > (blocks.GetLength(1) - bl.coords.y - 1)) // Нулевой игрок
                    {
                        bl.SetSkin(skins[0].boardSkinID);
                    }
                    else if (bl.coords.x > bl.coords.y && bl.coords.x < (blocks.GetLength(1) - bl.coords.y - 1)) // Первый игрок
                    {
                        bl.SetSkin(skins[1].boardSkinID);
                    }
                    else if (bl.coords.x >= bl.coords.y && bl.coords.x >= (blocks.GetLength(1) - bl.coords.y - 1)) // Второй игрок
                    {
                        bl.SetSkin(skins[2].boardSkinID);
                    }
                    else // Третий игрок
                    {
                        bl.SetSkin(skins[3].boardSkinID);
                    }
                }
                decors[0].SetSkin(skins[0].boardSkinID, 0);
                decors[1].SetSkin(skins[1].boardSkinID, 0);
                decors[2].SetSkin(skins[2].boardSkinID, 0);
                decors[3].SetSkin(skins[3].boardSkinID, 0);
                break;

            default:
                SpesLogger.Warning("Default Value while updating skins: length " + skins.Length);
                break;
        }
    }

    /// <summary>
    /// Проверяет доступность пути от пешки до финиша
    /// </summary>
    /// <param name="pawnPos"> Положение пешки игрока </param>
    /// <param name="includeWallPredict"> Еще не поставленная стенка</param>
    /// <param name="type"> Положение непоставленной стенки </param>
    /// <returns></returns>
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
    /// Создает соединения между блоками
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
