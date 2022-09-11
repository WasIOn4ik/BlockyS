using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public struct BlockVariation
{
    public Mesh mesh;
    public Material mat;
}

[Serializable]
public struct SkinDescription
{
    public string name;

    public Mesh variation1Prefab;
    public Mesh variation2Prefab;
    public Mesh variation3Prefab;

    public List<BlockVariation> blocks;
}

[Serializable]
public struct PawnDescription
{
    public string name;
    public Mesh mesh;
    public Material mat;
}

[CreateAssetMenu(menuName = "Spes/Skins")]
public class SkinsLibrary : ScriptableObject
{
    public List<SkinDescription> boardSkins = new();
    public List<PawnDescription> pawnSkins = new();

    public bool TryGetSkin(string skinName, out SkinDescription skin)
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
}
