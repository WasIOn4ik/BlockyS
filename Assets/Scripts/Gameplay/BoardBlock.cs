using UnityEngine;

public class BoardBlock : MonoBehaviour
{
    #region Variables

    [SerializeField] private MeshRenderer mesh;
    [SerializeField] private MeshFilter filter;

	public int skinID;

    public Point coords;
    public BoardBlock xDir;
    public BoardBlock zDir;
    public BoardBlock mxDir;
    public BoardBlock mzDir;

    public bool bEmpty = true;

    public bool bSelected = false;

    public bool bHighlighted = false;

	private Color color = new();

	#endregion

	#region StaticVariables

	public static BoardBlock selectedBlock = null;

    #endregion

    #region UnityCallbacks

    public void Awake()
    {
        color = mesh.material.color;
    }

	#endregion

	#region Functions

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

	public void SetSkin(int ind)
	{
		skinID = ind;
		var skin = GameBase.instance.skins.GetUncheckedBoardSkin(ind);
		filter.mesh = skin.blocks[0];
		mesh.material = skin.mat;
		color = mesh.material.color;
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

	private void HighlightSelf()
    {
        bHighlighted = true;

        mesh.material.color = Color.green;
    }

	private void UnHighlightSelf()
    {
        bHighlighted = false;

        mesh.material.color = color;
    }

    #endregion
}
