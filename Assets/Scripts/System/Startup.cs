using UnityEngine;
using UnityEngine.AddressableAssets;

public class Startup : MonoBehaviour
{
	[SerializeField] private AssetReference startupMenuReference;
	[SerializeField] private AssetReference gameBaseReference;
	private void Awake()
	{
		gameBaseReference.InstantiateAsync().Completed += x =>
		{
			MenuBase.OpenMenu(MenuBase.STARTUP_MENU);
		};
	}
}
