using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CosmeticMeshTarget : MonoBehaviour
{
    public MeshRenderer meshRenderer;
    public MeshFilter filter;

    public void SetSkin(int id, int num)
    {
        var skin = GameBase.instance.skins.boardSkins[id];
        Mesh m = null;

        switch (num)
        {
            case 0:
                m = skin.variation1Mesh;
                break;
            case 1:
                m = skin.variation2Mesh;
                break;
            case 2:
                m = skin.variation3Mesh;
                break;
            case 3:
                m = skin.variation1Mesh;
                break;
        }

        filter.mesh = m;
        meshRenderer.material = skin.mat;
    }
}