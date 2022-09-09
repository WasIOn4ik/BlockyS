using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardBlock : MonoBehaviour
{
    [SerializeField] protected List<MeshRenderer> meshes;

    public Point coords;
    public BoardBlock xDir;
    public BoardBlock zDir;
    public BoardBlock mxDir;
    public BoardBlock mzDir;

    public bool bSelected = false;

    public bool bHighlighted = false;

    protected List<Color> colors = new();

    public static BoardBlock selectedBlock = null;

    public void Awake()
    {
        foreach (var m in meshes)
        {
            foreach (var mat in m.materials)
            {
                colors.Add(mat.color);
            }
        }
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

        foreach (var m in meshes)
        {
            m.material.color = Color.green;
        }
    }

    public void UnHighlightSelf()
    {
        bHighlighted = false;

        int ind = 0;
        for (int i = 0; i < meshes.Count; i++)
        {
            for (int j = 0; j < meshes[i].materials.Length; j++)
            {
                meshes[i].materials[j].color = colors[ind];
                ind++;
            }
        }
    }
}
