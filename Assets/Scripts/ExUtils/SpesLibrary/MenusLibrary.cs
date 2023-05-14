using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

[Serializable]
public struct MenuDescriptor
{
	public string title;
	public AssetReferenceGameObject assetReference;
}

[CreateAssetMenu(menuName = "Spes/MenusLibrary")]
public class MenusLibrary : ScriptableObject
{
	public List<MenuDescriptor> menuAssets = new List<MenuDescriptor>();

	public void LoadMenuPrefab(string title, Action<MenuBase> onLoaded, Action onError = null)
	{
		var m = menuAssets.Find(x => x.title == title);

		if (m.assetReference == null)
			SpesLogger.Error($"Menu {title} was not found in MenusLibrary");

		Addressables.LoadAssetAsync<GameObject>(m.assetReference).Completed += x =>
		{
			if (x.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
				onLoaded(x.Result.GetComponent<MenuBase>());

			else if (x.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Failed)
				onError?.Invoke();
		};
	}
}
