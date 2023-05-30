using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class Startup : MonoBehaviour
{
	[SerializeField] private float loadingScreenStartupMinTimer = 1f;
	[SerializeField] private AssetReferenceGameObject loadingScreenAsset;
	[SerializeField] private AssetReference gameBaseAsset;

	private void Awake()
	{
		LoadingScreenUI loadingScreen = null;

		if (GameBase.Instance)
		{
			GameBase.Instance.LoadingScreen.Setup(0, StartMenuMusic);
			GameBase.Instance.skins.ReleaseAll();
			MenuBase.OpenMenu(MenuBase.STARTUP_MENU, (x) => { GameBase.Instance.LoadingScreen.Hide(); });
			NetworkManager.Singleton.Shutdown(true);
			return;
		}
		else
		{
			loadingScreenAsset.InstantiateAsync().Completed += x =>
			{
				if (x.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
				{
					loadingScreen = x.Result.GetComponent<LoadingScreenUI>();
					loadingScreen.Setup(loadingScreenStartupMinTimer, StartMenuMusic);
				}

				gameBaseAsset.InstantiateAsync().Completed += x =>
				{
					GameBase.Instance.LoadingScreen = loadingScreen;
					MenuBase.OpenMenu(MenuBase.STARTUP_MENU, x =>
					{
						loadingScreen.Hide();
					});
				};
			};
		}
	}

	private void StartMenuMusic()
	{
		SoundManager.Instance.StartBackgroundMusic(SoundManager.MusicType.MainMenuMusic);
	}
}
