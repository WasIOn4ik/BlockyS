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

		//Asset already loaded
		if (m.assetReference.IsValid())
		{
			var go = m.assetReference.Asset as GameObject;
			if (!go)
			{
				SpesLogger.Critical($"Menu with name {title} loaded, but can't convert it to GameObject");
			}
			onLoaded(go.GetComponent<MenuBase>());
			return;
		}

		//Asset loading
		Addressables.LoadAssetAsync<GameObject>(m.assetReference).Completed += x =>
		{
			if (x.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
				onLoaded?.Invoke(x.Result.GetComponent<MenuBase>());

			else if (x.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Failed)
				onError?.Invoke();
		};
	}
}
