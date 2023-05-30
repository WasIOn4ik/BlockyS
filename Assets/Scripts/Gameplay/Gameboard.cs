using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Gameboard
{
	#region Variables

	public BoardBlock[,] blocks;

	public WallPlaceholder[,] wallsPlaces;

	public int halfExtention;

	private BoardBlock finish;

	private List<CosmeticMeshTarget> decors = new();

	[SerializeField] private BoardBlock boardBlockPrefab;

	[SerializeField] private WallPlaceholder wallPlaceholderPrefab;

	[SerializeField] private CosmeticMeshTarget cosmeticMeshPrefab;

	[SerializeField] private Transform blocksHolder;

	[Tooltip("Size in blocks of corners decorations")]
	[SerializeField] private int AnglesTowersSize = 2;

	#endregion

	#region Functions

	/// <summary>
	/// Creates blocks, connections and wall-place points
	/// </summary>
	/// <param name="halfExtent"> Board size from center to player spawn </param>
	public void Initialize(int halfExtent)
	{
		SpesLogger.Deb($"Gameboard initialization: {halfExtent}");

		halfExtention = halfExtent;
		int size = halfExtent * 2 + 1;
		blocks = new BoardBlock[size, size];
		wallsPlaces = new WallPlaceholder[size - 1, size - 1];

		List<GameObject> sba = new();

		for (int i = 0; i < size; i++)
		{
			for (int j = 0; j < size; j++)
			{
				var position = new Vector3(i - halfExtent, 0, j - halfExtent);

				if (!((i < AnglesTowersSize && j < AnglesTowersSize) || (i > size - (AnglesTowersSize + 1) && j < AnglesTowersSize) || (i < AnglesTowersSize && j > size - (AnglesTowersSize + 1)) || (i > size - (AnglesTowersSize + 1) && j > size - (AnglesTowersSize + 1))))
				{
					var block = GameObject.Instantiate(boardBlockPrefab, position, Quaternion.identity, blocksHolder);
					sba.Add(block.gameObject);
					blocks[i, j] = block;
					block.coords = new Point(i, j);
					block.name = "BLock_" + i + "x" + j;
				}
				if (!((i < AnglesTowersSize && j < AnglesTowersSize) || (i > size - (AnglesTowersSize + 2) && j < AnglesTowersSize) || (i < AnglesTowersSize && j > size - (AnglesTowersSize + 2)) || (i > size - (AnglesTowersSize + 2) && j > size - (AnglesTowersSize + 2))))
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

		StaticBatchingUtility.Combine(sba.ToArray(), sba[0]);
		finish = blocks[halfExtent, halfExtent];
		GenConnections(halfExtent);
	}

	/// <summary>
	/// Updates game field with selected skins.
	/// </summary>
	/// <param name="skins">2-4</param>
	public void UpdateSkins(List<int> skins)
	{
		Debug.Log("SKINS:");
		foreach (var s in skins)
		{
			Debug.Log(GameBase.Instance.skins.GetBoard(s).title);
		}

		int count = skins.Count;

		switch (count)
		{
			case 1:
				foreach (var bl in blocks)
				{
					if (!bl)
						continue;

					bl.SetSkin(skins[0]);
				}
				decors[0].SetBoardSkin(skins[0], 0);
				decors[1].SetBoardSkin(skins[0], 1);
				decors[2].SetBoardSkin(skins[0], 2);
				decors[3].SetBoardSkin(skins[0], 0);
				break;
			case 2:
				foreach (var bl in blocks)
				{
					if (!bl)
						continue;

					if (bl.coords.y >= halfExtention)
					{
						bl.SetSkin(skins[1]);
					}
					else
					{
						bl.SetSkin(skins[0]);
					}
				}
				decors[0].SetBoardSkin(skins[1], 0);
				decors[1].SetBoardSkin(skins[1], 1);
				decors[2].SetBoardSkin(skins[0], 2);
				decors[3].SetBoardSkin(skins[0], 0);
				break;
			case 3:
				foreach (var bl in blocks)
				{
					if (!bl)
						continue;

					if (bl.coords.x < bl.coords.y && bl.coords.x > (blocks.GetLength(1) - bl.coords.y - 1)) // Zero player
					{
						bl.SetSkin(skins[2]);
					}
					else if (bl.coords.x > bl.coords.y && bl.coords.x < (blocks.GetLength(1) - bl.coords.y - 1)) // The first player
					{
						bl.SetSkin(skins[1]);
					}
					else if (bl.coords.x >= bl.coords.y && bl.coords.x >= (blocks.GetLength(1) - bl.coords.y - 1)) // The second player
					{
						bl.SetSkin(skins[0]);
					}
					else // The third player
					{
						bl.SetSkin(skins[0]);
					}
				}
				decors[0].SetBoardSkin(skins[0], 0);
				decors[1].SetBoardSkin(skins[2], 1);
				decors[2].SetBoardSkin(skins[1], 2);
				decors[3].SetBoardSkin(skins[0], 0);
				break;
			case 4:
				foreach (var bl in blocks)
				{
					if (!bl)
						continue;

					if (bl.coords.x < bl.coords.y && bl.coords.x > (blocks.GetLength(1) - bl.coords.y - 1)) // Zero player
					{
						bl.SetSkin(skins[2]);
					}
					else if (bl.coords.x > bl.coords.y && bl.coords.x < (blocks.GetLength(1) - bl.coords.y - 1)) // The first player
					{
						bl.SetSkin(skins[0]);
					}
					else if (bl.coords.x >= bl.coords.y && bl.coords.x >= (blocks.GetLength(1) - bl.coords.y - 1)) // The second player
					{
						bl.SetSkin(skins[3]);//
					}
					else // The third player
					{
						bl.SetSkin(skins[1]);
					}
				}
				decors[0].SetBoardSkin(skins[1], 0);
				decors[1].SetBoardSkin(skins[3], 1);
				decors[2].SetBoardSkin(skins[2], 2);
				decors[3].SetBoardSkin(skins[0], 0);
				break;

			default:
				SpesLogger.Warning("Default Value while updating skins: length " + skins.Count);
				break;
		}
	}

	/// <summary>
	/// Checks availabilty of final point to all players
	/// </summary>
	/// <param name="pawnPos"> Player pawn point </param>
	/// <param name="includeWallPredict"> Ghost wall </param>
	/// <param name="type"> Type of turn (Ghost wall rotation) </param>
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

		return false;
	}

	private List<BoardBlock> GetNeighbours(BoardBlock block, Point wall, bool xForward)
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
	/// Creates connections of blocks
	/// </summary>
	/// <param name="halfExtent"></param>
	private void GenConnections(int halfExtent)
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
