using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public struct SkinDescription
{
    public string name;

    public Mesh variation1Mesh;
    public Mesh variation2Mesh;
    public Mesh variation3Mesh;

    public Mesh wallMesh;

    public Material mat;

    public List<Mesh> blocks;
}

[Serializable]
public struct PawnDescription
{
    public string name;
    public Mesh mesh;
    public Material mat;
    public Vector3 rotation;
    public Vector3 position;
    public float scale;
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
