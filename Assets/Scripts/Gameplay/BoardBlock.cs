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
		color = meshRendererComponent.material.color;
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
		var skin = GameBase.Instance.skins.GetBoard(ind);

		if (skin.TryGetBlock(out var blockMesh))
		{
			meshFilterComponent.mesh = blockMesh;
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

		meshRendererComponent.material.color = Color.green;
	}

	private void UnHighlightSelf()
	{
		bHighlighted = false;

		meshRendererComponent.material.color = color;
	}

	#endregion
}
