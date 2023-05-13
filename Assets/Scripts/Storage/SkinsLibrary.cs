using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public struct BoardSkinDescription
{
	public string name;

	public Mesh variation1Mesh;
	public Mesh variation2Mesh;
	public Mesh variation3Mesh;

	public Mesh wallMesh;

	public Material mat;

	public List<Mesh> blocks;

	public int cost;
}

[Serializable]
public struct PawnSkinDescription
{
	public string name;
	public Mesh mesh;
	public Material mat;
	public Vector3 rotation;
	public Vector3 position;
	public float scale;

	public int cost;
}

[CreateAssetMenu(menuName = "Spes/Skins")]
public class SkinsLibrary : ScriptableObject
{
	[SerializeField] private List<BoardSkinDescription> boardSkins = new();
	[SerializeField] private List<PawnSkinDescription> pawnSkins = new();

	public bool TryGetSkin(string skinName, out BoardSkinDescription skin)
	{
		foreach (var sk in boardSkins)
		{
			if (sk.name == skinName)
			{
				skin = sk;
				return true;
			}
		}
		skin = default;
		return false;
	}

	public int GetBoardSkinsCount()
	{
		return boardSkins.Count;
	}

	public int GetPawnSkinsCount()
	{
		return pawnSkins.Count;
	}

	public BoardSkinDescription GetUncheckedBoardSkin(int id)
	{
		return boardSkins[id];
	}

	public PawnSkinDescription GetUncheckedPawnSkin(int id)
	{
		return pawnSkins[id];
	}
}
