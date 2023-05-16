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

	public bool bNetMode;

	private MessageScript currentMessage;

	#endregion

	#region StaticVariables

	public static GameBase Instance { get; private set; }
	public static ServerBase Server { get; private set; }
	public static ClientBase Client { get; private set; }
	public static GameStorage Storage { get; private set; }

	#endregion

	#region UnityCallbacks

	private void Awake()
	{
		if (Instance)
		{
			Destroy(gameObject);
			return;
		}

		Instance = this;
		DontDestroyOnLoad(this);
		MenuBase.SetLibrary(menusLibrary);

		Server = GetComponent<ServerBase>();
		Client = GetComponent<ClientBase>();
		Storage = GetComponent<GameStorage>();

		var net = GetComponent<NetworkManager>();

		Server.networkManager = net;
		Client.networkManager = net;

		Application.targetFrameRate = 60;
		Application.quitting += Application_OnQuit;

		Storage.LoadPrefs();
		Storage.LoadProgress();

		if (Storage.CurrentBoardSkin != 0 && !Storage.CheckBoard(Storage.CurrentBoardSkin))
		{
			Storage.CurrentBoardSkin = 0;
		}

		if (Storage.CurrentPawnSkin != 0 && !Storage.CheckPawn(Storage.CurrentPawnSkin))
		{
			Storage.CurrentPawnSkin = 0;
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
		if (!currentMessage)
		{
			Addressables.InstantiateAsync(messageMenuAsset).Completed += (x) =>
			{
				if (x.Status != UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
					SpesLogger.Critical("Error while loading MessageAssetReference");

				currentMessage = x.Result.GetComponent<MessageScript>();

				currentMessage.ShowMessage(entry, action, bLocalized, param);
			};
			return;
		}

		currentMessage.ShowMessage(entry, action, bLocalized, param);
	}

	#endregion
}
