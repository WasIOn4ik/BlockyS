using UnityEngine;
using Unity.Netcode;
using UnityEngine.Localization.Settings;
using UnityEngine.AddressableAssets;

[RequireComponent(typeof(ServerBase), typeof(ClientBase), typeof(GameStorage))]
public class GameBase : MonoBehaviour
{
	#region Variables

	[SerializeField] private MenusLibrary menusLibrary;
	public SkinsLibrarySO skins;
	public GameRules gameRules;
	public AssetReference messageMenuAsset;

	private MessageUI currentMessageUI;

	#endregion

	#region StaticVariables

	public static GameBase Instance { get; private set; }
	public static ServerBase Server { get; private set; }
	public static ClientBase Client { get; private set; }
	public static GameStorage Storage { get; private set; }

	public static LobbyGameSystem Lobby { get; private set; }

	#endregion

	#region UnityCallbacks

	private void Awake()
	{
		if (Instance)
		{
			Destroy(gameObject);
			return;
		}

		SpesLogger.currentLevel = SpesLogLevel.Debug;
		Instance = this;
		DontDestroyOnLoad(this);
		MenuBase.SetLibrary(menusLibrary);
		CameraAnimator.animationTime = gameRules.cameraAnimationTime;

		Server = GetComponent<ServerBase>();
		Client = GetComponent<ClientBase>();
		Storage = GetComponent<GameStorage>();
		Lobby = GetComponent<LobbyGameSystem>();

		Application.targetFrameRate = 60;
		Application.quitting += Application_OnQuit;

		Storage.LoadPrefs();
		Storage.LoadProgress();

		if (Storage.CurrentBoardSkinID != 0 && !Storage.CheckBoard(Storage.CurrentBoardSkinID))
		{
			Storage.CurrentBoardSkinID = 0;
		}

		if (Storage.CurrentPawnSkinID != 0 && !Storage.CheckPawn(Storage.CurrentPawnSkinID))
		{
			Storage.CurrentPawnSkinID = 0;
		}

		skins.Initialize();
	}

	#endregion

	#region Callbacks

	private void Application_OnQuit()
	{
		Storage.SaveProgress();
		Storage.SavePrefs();
	}

	#endregion

	#region Functions

	public void ApplyLanguage(string code)
	{
		var settings = LocalizationSettings.Instance;
		var locale = settings.GetAvailableLocales().GetLocale(code);
		settings.SetSelectedLocale(locale);
		SpesLogger.Detail("Current language set to " + code);
	}

	public void ShowMessage(string entry, MessageAction action, bool bLocalized, string param = "")
	{
		if (!currentMessageUI)
		{
			Addressables.InstantiateAsync(messageMenuAsset).Completed += (x) =>
			{
				if (x.Status != UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
					SpesLogger.Critical("Error while loading MessageAssetReference");

				currentMessageUI = x.Result.GetComponent<MessageUI>();

				currentMessageUI.ShowMessage(entry, action, bLocalized, param);
			};
			return;
		}

		currentMessageUI.ShowMessage(entry, action, bLocalized, param);
	}

	#endregion
}
