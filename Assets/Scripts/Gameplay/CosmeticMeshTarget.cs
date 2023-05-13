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
        var boardSkin = GameBase.instance.skins.GetUncheckedBoardSkin(id);
        Mesh m = null;

        switch (num)
        {
            case 0:
                m = boardSkin.variation1Mesh;
                break;
            case 1:
                m = boardSkin.variation2Mesh;
                break;
            case 2:
                m = boardSkin.variation3Mesh;
                break;
            case 3:
                m = boardSkin.variation1Mesh;
                break;
        }

        filter.mesh = m;
        meshRenderer.material = boardSkin.mat;
    }

    #endregion
}