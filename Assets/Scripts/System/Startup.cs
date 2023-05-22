using System.Collections;
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

		if (loadingScreenAsset.IsValid() && gameBaseAsset.IsValid())
		{
			GameBase.Instance.LoadingScreen.Setup(0, StartMenuMusic);
			MenuBase.OpenMenu(MenuBase.STARTUP_MENU);
			return;
		}

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

	private void StartMenuMusic()
	{
		SoundManager.Instance.StartBackgroundMusic(SoundManager.MusicType.MainMenuMusic);
		SoundManager.Instance.StopAllCoroutines();
		StartCoroutine(SoundManager.Instance.HandleMusic());
	}
}
