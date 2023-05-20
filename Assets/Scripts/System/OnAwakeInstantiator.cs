using UnityEngine;
using UnityEngine.AddressableAssets;

public class OnAwakeInstantiator : MonoBehaviour
{
	[SerializeField] private AssetReferenceGameObject assetToInstantiate;

	private void Awake()
	{
		assetToInstantiate.InstantiateAsync();
	}
}
