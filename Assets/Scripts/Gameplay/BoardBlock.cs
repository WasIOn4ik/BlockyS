using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardBlock : MonoBehaviour
{
    [SerializeField] protected MeshRenderer mesh;
    [SerializeField] protected MeshFilter filter;

    public Point coords;
    public BoardBlock xDir;
    public BoardBlock zDir;
    public BoardBlock mxDir;
    public BoardBlock mzDir;

    public bool bEmpty = true;

    public bool bSelected = false;

    public bool bHighlighted = false;

    protected Color color = new();

    public static BoardBlock selectedBlock = null;

    public void Awake()
    {
        color = mesh.material.color;
    }

    public void HighlightAround()
    {
        if (selectedBlock)
            selectedBlock.UnHighlightAround();

        selectedBlock = this;
        bSelected = true;

        if (xDir)
            xDir.HighlightSelf();
        if (zDir)
            zDir.HighlightSelf();
        if (mxDir)
            mxDir.HighlightSelf();
        if (mzDir)
            mzDir.HighlightSelf();
    }

    public void UnHighlightAround()
    {
        if (selectedBlock == this)
            selectedBlock = null;

        bSelected = false;

        if (xDir)
            xDir.UnHighlightSelf();
        if (zDir)
            zDir.UnHighlightSelf();
        if (mxDir)
            mxDir.UnHighlightSelf();
        if (mzDir)
            mzDir.UnHighlightSelf();
    }

    public void HighlightSelf()
    {
        bHighlighted = true;

        mesh.material.color = Color.green;
    }

    public void UnHighlightSelf()
    {
        bHighlighted = false;

        mesh.material.color = color;
    }

    public void SetSkin(int ind)
    {
        var skin = GameBase.instance.skins.boardSkins[ind];
        filter.mesh = skin.blocks[0];
        mesh.material = skin.mat;
        color = mesh.material.color;
    }
}
