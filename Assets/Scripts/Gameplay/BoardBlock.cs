using UnityEngine;

public class BoardBlock : MonoBehaviour
{
	#region Variables

	[SerializeField] private MeshRenderer meshRendererComponent;
	[SerializeField] private MeshFilter meshFilterComponent;

	public int skinID;

	public Point coords;
	public BoardBlock xDir;
	public BoardBlock zDir;
	public BoardBlock mxDir;
	public BoardBlock mzDir;

	public bool bEmpty = true;

	public bool bHighlighted = false;

	private Color color = new();

	#endregion

	#region StaticVariables

	private static BoardBlock selectedBlock = null;

	#endregion

	#region StaticFunctions

	public static void ClearCurrentSelection()
	{
		if (selectedBlock)
			selectedBlock.UnHighlightAround();
	}

	#endregion

	#region UnityCallbacks

	public void Awake()
	{
		color = meshRendererComponent.material.color;
	}

	#endregion

	#region Functions

	public bool IsSelectedBlock()
	{
		return this == selectedBlock;
	}

	public void HighlightAround()
	{
		ClearCurrentSelection();

		selectedBlock = this;

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
		var skin = GameBase.Instance.skins.GetBoard(ind);

		if (skin.TryGetBlock(out var blockMesh))
		{
			meshFilterComponent.mesh = blockMesh;
		}
		else
		{
			Debug.Log("can't load block mesh");
		}

		if (skin.TryGetMaterial(out var material))
		{
			meshRendererComponent.material = material;
			color = meshRendererComponent.material.color;
		}
	}

	public void UnHighlightAround()
	{
		if (selectedBlock == this)
			selectedBlock = null;

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

		meshRendererComponent.material.color = Color.green;
	}

	private void UnHighlightSelf()
	{
		bHighlighted = false;

		meshRendererComponent.material.color = color;
	}

	#endregion
}
