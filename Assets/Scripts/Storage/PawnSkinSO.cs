using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Localization;
using UnityEngine.ResourceManagement.AsyncOperations;

[CreateAssetMenu(menuName = "BlockyS/PawnSkin")]
public class PawnSkinSO : ScriptableObject
{
	#region Variables

	public int id;

	public string title;

	public LocalizedString localizedTitle;

	[SerializeField] private AssetReference prefabAsset;

	public Vector3 rotation;
	public Vector3 position;
	public float scale;

	public int cost;

	#endregion

	#region Functions

	public void InstantiateTo(Transform parent, Action<GameObject> onInstantiated)
	{
		prefabAsset.InstantiateAsync(parent).Completed += x =>
		{
			if (x.Status == AsyncOperationStatus.Succeeded)
			{
				onInstantiated?.Invoke(x.Result);
			}
		};
	}

	#endregion
}
