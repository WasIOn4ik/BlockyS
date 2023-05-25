using System;
using System.Collections.Generic;
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

	private List<GameObject> instantiated = new List<GameObject>();

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

	public void Unload()
	{
		if (prefabAsset.IsValid())
		{
			for (int i = 0; i < instantiated.Count; i++)
			{
				if (instantiated[i] != null)
					prefabAsset.ReleaseInstance(instantiated[i]);
			}
			instantiated.Clear();

			prefabAsset.ReleaseAsset();
		}
	}

	#endregion
}
