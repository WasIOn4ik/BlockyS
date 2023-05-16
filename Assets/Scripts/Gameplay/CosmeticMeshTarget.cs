using UnityEngine;

/// <summary>
/// Support wrap on decorations of map corners
/// </summary>
public class CosmeticMeshTarget : MonoBehaviour
{
	#region Variables

	[SerializeField] private MeshRenderer meshRenderer;
	[SerializeField] private MeshFilter filter;

	#endregion

	#region Functions

	public void SetBoardSkin(int id, int num)
	{
		var boardSkin = GameBase.Instance.skins.GetBoard(id);
		if (boardSkin.TryGetDecorMesh(num, out var decorMesh))
		{
			filter.mesh = decorMesh;
		}

		if (boardSkin.TryGetMaterial(out var material))
		{
			meshRenderer.material = material;
		}
	}

	#endregion
}